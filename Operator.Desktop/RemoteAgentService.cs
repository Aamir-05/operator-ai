using Operator.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.Desktop;

public sealed class RemoteAgentService : IAsyncDisposable
{
    private readonly Func<string, Task> _localLog;
    private CancellationTokenSource? _serviceCancellation;
    private Task? _serviceTask;

    public event Action<string>? StatusChanged;

    public RemoteAgentService(Func<string, Task> localLog) => _localLog = localLog;

    public bool IsRunning => _serviceTask != null && !_serviceTask.IsCompleted;

    public void Start()
    {
        if (IsRunning) return;

        _serviceCancellation = new CancellationTokenSource();
        _serviceTask = Task.Run(() => ServiceLoopAsync(_serviceCancellation.Token));
    }

    public async Task StopAsync()
    {
        if (_serviceCancellation == null) return;

        _serviceCancellation.Cancel();

        if (_serviceTask != null)
        {
            try { await _serviceTask; } catch { }
        }

        _serviceCancellation.Dispose();
        _serviceCancellation = null;
        _serviceTask = null;
        RaiseStatus("Remote stopped");
    }

    private async Task ServiceLoopAsync(CancellationToken token)
    {
        RaiseStatus("Remote starting");

        while (!token.IsCancellationRequested)
        {
            RemoteSettings settings = RemoteSettings.Load();

            if (!settings.Enabled
                || string.IsNullOrWhiteSpace(settings.ProjectUrl)
                || string.IsNullOrWhiteSpace(settings.DeviceId)
                || string.IsNullOrWhiteSpace(OperatorSecrets.GetDeviceSecret(settings.DeviceId)))
            {
                RaiseStatus("Remote not paired/configured");
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                continue;
            }

            RemoteApiClient api = new(settings);

            try
            {
                RaiseStatus("Remote online");
                RemotePollResponse poll = await api.PollDeviceAsync(token);

                if (poll.Command != null)
                    await ExecuteRemoteCommandAsync(api, settings, poll.Command, token);
                else
                    await Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds), token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                RaiseStatus("Remote reconnecting");
                await _localLog("[REMOTE ERROR] " + ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
    }

    private async Task ExecuteRemoteCommandAsync(
        RemoteApiClient api,
        RemoteSettings settings,
        RemoteCommandDto command,
        CancellationToken serviceToken)
    {
        if (string.IsNullOrWhiteSpace(command.Id) || string.IsNullOrWhiteSpace(command.CommandText))
            return;

        using CancellationTokenSource taskCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(serviceToken);

        RemoteCommandControl control = new(api, command.Id, taskCancellation);
        Task controlTask = control.RunAsync(serviceToken);

        try
        {
            RaiseStatus("Remote task running");
            await api.ReportAsync(command.Id, "running", null, null, 1, serviceToken);
            await _localLog("[REMOTE TASK] " + command.CommandText);

            OperatorExecutionHooks hooks = new()
            {
                BeforePlanningStepAsync = async (_, token) =>
                    await control.WaitUntilRunnableAsync(token),

                BeforeToolAsync = async (_, _, token) =>
                {
                    await control.WaitUntilRunnableAsync(token);
                    return OperatorToolGateDecision.Continue();
                }
            };

            OperatorAgent agent = new();
            string remoteTask = BuildRemoteTaskPrompt(command);
            int logCount = 0;

            string result = await agent.RunAsync(
                remoteTask,
                message =>
                {
                    logCount++;
                    _ = _localLog("[REMOTE] " + message);
                    _ = ReportLogBestEffortAsync(api, command.Id, message, Math.Min(95, 5 + logCount));
                },
                taskCancellation.Token,
                hooks);

            string finalStatus = AgentRunGuard.IsFailure(result) ? "failed" : "completed";
            if (result.StartsWith("CANCELLED", StringComparison.OrdinalIgnoreCase))
                finalStatus = "cancelled";

            await api.ReportAsync(command.Id, finalStatus, result, null, 100, serviceToken);

            if (finalStatus == "completed")
            {
                if (command.CaptureScreenshot || settings.CaptureScreenshotAfterRemoteTask)
                    await UploadScreenshotBestEffortAsync(api, command.Id, serviceToken);

                if (command.CollectResultFiles)
                    await UploadDeclaredResultFilesAsync(api, command.Id, result, serviceToken);
            }

            RaiseStatus("Remote online");
        }
        catch (OperationCanceledException)
        {
            try
            {
                await api.ReportAsync(
                    command.Id,
                    "cancelled",
                    "CANCELLED: Remote task was cancelled.",
                    null,
                    100,
                    serviceToken);
            }
            catch { }

            RaiseStatus("Remote online");
        }
        catch (Exception ex)
        {
            try
            {
                await api.ReportAsync(command.Id, "failed", "ERROR: " + ex.Message, null, 100, serviceToken);
            }
            catch { }

            await _localLog("[REMOTE TASK ERROR] " + ex.Message);
            RaiseStatus("Remote online");
        }
        finally
        {
            control.Stop();
            try { await controlTask; } catch { }
        }
    }

    private static string BuildRemoteTaskPrompt(RemoteCommandDto command) =>
        """
        This task was sent remotely by the authenticated owner of this paired Operator AI computer.

        Perform only the requested task on this computer and continue to follow all Operator AI safe-mode rules.

        When the task creates or updates a file that the user is likely to want on mobile, and that file is on the Windows Desktop, include this exact line in the final response for every such file:

        RESULT_FILE: <Desktop-relative-path>

        Do not use RESULT_FILE for files outside Desktop.

        REMOTE USER COMMAND:
        """
        + Environment.NewLine
        + command.CommandText;

    private static async Task ReportLogBestEffortAsync(
        RemoteApiClient api,
        string commandId,
        string message,
        int progress)
    {
        try
        {
            await api.ReportAsync(commandId, "running", null, message, progress, CancellationToken.None);
        }
        catch { }
    }

    private static async Task UploadScreenshotBestEffortAsync(
        RemoteApiClient api,
        string commandId,
        CancellationToken token)
    {
        try
        {
            byte[] screenshot = WindowsScreenCapture.CapturePrimaryScreenJpeg();
            await api.UploadArtifactAsync(commandId, "screen.jpg", "screenshot", "image/jpeg", screenshot, token);
        }
        catch { }
    }

    private static async Task UploadDeclaredResultFilesAsync(
        RemoteApiClient api,
        string commandId,
        string result,
        CancellationToken token)
    {
        string desktop = Path.GetFullPath(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

        MatchCollection matches = Regex.Matches(
            result,
            @"(?im)^\s*RESULT_FILE:\s*(.+?)\s*$");

        HashSet<string> uploaded = new(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            string relative = match.Groups[1].Value.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(relative)) continue;

            string candidate = Path.GetFullPath(Path.Combine(desktop, relative));
            bool insideDesktop = candidate.StartsWith(
                desktop + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);

            if (!insideDesktop || !File.Exists(candidate) || !uploaded.Add(candidate))
                continue;

            FileInfo info = new(candidate);
            const long MaximumBytes = 12L * 1024L * 1024L;
            if (info.Length > MaximumBytes) continue;

            byte[] bytes = await File.ReadAllBytesAsync(candidate, token);
            await api.UploadArtifactAsync(
                commandId,
                Path.GetFileName(candidate),
                "result_file",
                GuessMimeType(candidate),
                bytes,
                token);
        }
    }

    private static string GuessMimeType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

    private void RaiseStatus(string status) => StatusChanged?.Invoke(status);

    public async ValueTask DisposeAsync() => await StopAsync();

    private sealed class RemoteCommandControl
    {
        private readonly RemoteApiClient _api;
        private readonly string _commandId;
        private readonly CancellationTokenSource _taskCancellation;
        private readonly SemaphoreSlim _stateChanged = new(0, int.MaxValue);
        private volatile string _controlState = "run";
        private volatile bool _stopped;

        public RemoteCommandControl(
            RemoteApiClient api,
            string commandId,
            CancellationTokenSource taskCancellation)
        {
            _api = api;
            _commandId = commandId;
            _taskCancellation = taskCancellation;
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !_stopped)
            {
                try
                {
                    RemoteControlResponse state = await _api.GetControlAsync(_commandId, token);
                    string newState = string.IsNullOrWhiteSpace(state.ControlState)
                        ? "run"
                        : state.ControlState.Trim().ToLowerInvariant();

                    _controlState = newState;

                    if (newState is "cancel" or "cancel_requested")
                    {
                        _taskCancellation.Cancel();
                        return;
                    }

                    _stateChanged.Release();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch { }

                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
        }

        public async Task WaitUntilRunnableAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                string state = _controlState;

                if (state is "cancel" or "cancel_requested")
                {
                    _taskCancellation.Cancel();
                    token.ThrowIfCancellationRequested();
                }

                if (state is not "pause" and not "paused")
                    return;

                await _stateChanged.WaitAsync(TimeSpan.FromSeconds(3), token);
            }
        }

        public void Stop()
        {
            _stopped = true;
            try { _stateChanged.Release(); } catch { }
        }
    }
}
