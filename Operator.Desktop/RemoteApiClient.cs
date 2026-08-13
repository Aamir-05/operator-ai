using Operator.AI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.Desktop;

public sealed class RemoteApiClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly RemoteSettings _settings;

    public RemoteApiClient(RemoteSettings settings) => _settings = settings;

    public Task<PairStartResponse> StartPairingAsync(CancellationToken token) =>
        PostAsync<PairStartResponse>(
            "operator-pair",
            new { action = "start", device_name = _settings.DeviceName },
            null,
            token);

    public Task<PairPollResponse> PollPairingAsync(string sessionId, string pollToken, CancellationToken token) =>
        PostAsync<PairPollResponse>(
            "operator-pair",
            new { action = "poll", session_id = sessionId, poll_token = pollToken },
            null,
            token);

    public Task<RemotePollResponse> PollDeviceAsync(CancellationToken token) =>
        DevicePostAsync<RemotePollResponse>(new { action = "poll" }, token);

    public Task<RemoteControlResponse> GetControlAsync(string commandId, CancellationToken token) =>
        DevicePostAsync<RemoteControlResponse>(new { action = "control", command_id = commandId }, token);

    public async Task ReportAsync(
        string commandId,
        string status,
        string? result,
        string? logLine,
        int? progress,
        CancellationToken token)
    {
        await DevicePostAsync<JsonElement>(
            new
            {
                action = "report",
                command_id = commandId,
                status,
                result,
                log_line = logLine,
                progress
            },
            token);
    }

    public async Task UploadArtifactAsync(
        string commandId,
        string fileName,
        string kind,
        string mimeType,
        byte[] bytes,
        CancellationToken token)
    {
        await DevicePostAsync<JsonElement>(
            new
            {
                action = "artifact",
                command_id = commandId,
                file_name = fileName,
                kind,
                mime_type = mimeType,
                content_base64 = Convert.ToBase64String(bytes)
            },
            token);
    }

    private Task<T> DevicePostAsync<T>(object body, CancellationToken token)
    {
        string deviceId = _settings.DeviceId;
        string? secret = OperatorSecrets.GetDeviceSecret(deviceId);

        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("This PC is not paired with Operator AI Mobile.");

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["x-operator-device-id"] = deviceId,
            ["x-operator-device-secret"] = secret
        };

        return PostAsync<T>("operator-device", body, headers, token);
    }

    private async Task<T> PostAsync<T>(
        string functionName,
        object body,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_settings.ProjectUrl))
            throw new InvalidOperationException("Operator Cloud URL is not configured.");

        string url = _settings.ProjectUrl.Trim().TrimEnd('/') + "/functions/v1/" + functionName;
        string json = JsonSerializer.Serialize(body);

        using HttpRequestMessage request = new(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using HttpResponseMessage response = await Http.SendAsync(request, token);
        string responseText = await response.Content.ReadAsStringAsync(token);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Operator Cloud {functionName} returned {(int)response.StatusCode}: {responseText}");

        T? result = JsonSerializer.Deserialize<T>(
            responseText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result ?? throw new InvalidOperationException($"Operator Cloud {functionName} returned an empty response.");
    }
}

public sealed class PairStartResponse
{
    public string SessionId { get; set; } = "";
    public string Code { get; set; } = "";
    public string PollToken { get; set; } = "";
    public string PairUri { get; set; } = "";
    public string ExpiresAt { get; set; } = "";
}

public sealed class PairPollResponse
{
    public string Status { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DeviceSecret { get; set; } = "";
    public string OwnerDisplay { get; set; } = "";
}

public sealed class RemotePollResponse
{
    public RemoteCommandDto? Command { get; set; }
    public string DeviceStatus { get; set; } = "";
}

public sealed class RemoteControlResponse
{
    public string Status { get; set; } = "";
    public string ControlState { get; set; } = "";
    public string ApprovalState { get; set; } = "";
}

public sealed class RemoteCommandDto
{
    public string Id { get; set; } = "";
    public string CommandText { get; set; } = "";
    public string Status { get; set; } = "";
    public string ControlState { get; set; } = "";
    public string ApprovalState { get; set; } = "";
    public bool CaptureScreenshot { get; set; }
    public bool CollectResultFiles { get; set; }
}
