using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Operator.AI;

public sealed class OperatorSettings
{
    public const string ProductVersion = "1.0.0";

    public string Model { get; set; } = "gpt-5.6";

    public int TaskTimeoutMinutes { get; set; } = 10;

    public int MaximumPlanningSteps { get; set; } = 80;

    public int MaximumRepeatedToolCalls { get; set; } = 3;

    public int MaximumConsecutiveErrors { get; set; } = 5;

    public int MaximumTotalToolCalls { get; set; } = 140;

    public int MaximumToolResultCharacters { get; set; } = 40000;

    public bool SafeMode { get; set; } = true;

    public bool AllowBrowserCoordinateFallback { get; set; } = true;

    public bool AllowKeyboardFallback { get; set; } = true;

    public bool WriteTaskJournal { get; set; } = true;

    [JsonIgnore]
    public string SettingsPath => GetSettingsPath();

    [JsonIgnore]
    public string HistoryDirectory => GetHistoryDirectory();

    public static OperatorSettings Load()
    {
        string settingsPath = GetSettingsPath();
        OperatorSettings settings = new();

        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(settingsPath)!
            );

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);

                settings =
                    JsonSerializer.Deserialize<OperatorSettings>(json)
                    ?? new OperatorSettings();
            }
            else
            {
                SaveDefaults(settings, settingsPath);
            }
        }
        catch
        {
            settings = new OperatorSettings();
        }

        settings.Normalize();
        return settings;
    }

    private void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Model))
        {
            Model = "gpt-5.6";
        }

        TaskTimeoutMinutes = Math.Clamp(TaskTimeoutMinutes, 1, 60);
        MaximumPlanningSteps = Math.Clamp(MaximumPlanningSteps, 10, 250);
        MaximumRepeatedToolCalls = Math.Clamp(MaximumRepeatedToolCalls, 1, 10);
        MaximumConsecutiveErrors = Math.Clamp(MaximumConsecutiveErrors, 2, 20);
        MaximumTotalToolCalls = Math.Clamp(MaximumTotalToolCalls, 20, 500);
        MaximumToolResultCharacters = Math.Clamp(MaximumToolResultCharacters, 4000, 120000);
    }

    private static void SaveDefaults(
        OperatorSettings settings,
        string settingsPath)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        File.WriteAllText(
            settingsPath,
            JsonSerializer.Serialize(settings, options)
        );
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OperatorAI",
            "operator.settings.json"
        );
    }

    private static string GetHistoryDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OperatorAI",
            "history"
        );
    }
}

public sealed class OperatorTaskJournal : IDisposable
{
    private readonly object _sync = new();
    private readonly bool _enabled;
    private readonly string _taskPreview;
    private bool _finished;

    public string RunId { get; }

    public string FilePath { get; }

    public DateTime StartedUtc { get; }

    private OperatorTaskJournal(
        string task,
        OperatorSettings settings)
    {
        RunId =
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") +
            "-" +
            Guid.NewGuid().ToString("N")[..8];

        StartedUtc = DateTime.UtcNow;
        _enabled = settings.WriteTaskJournal;
        _taskPreview = SanitizeTask(task);

        string dayFolder = Path.Combine(
            settings.HistoryDirectory,
            DateTime.UtcNow.ToString("yyyy-MM-dd")
        );

        FilePath = Path.Combine(dayFolder, $"{RunId}.jsonl");

        if (_enabled)
        {
            try
            {
                Directory.CreateDirectory(dayFolder);

                Record(
                    "run_start",
                    $"Operator AI {OperatorSettings.ProductVersion} task started."
                );
            }
            catch
            {
                // Journaling must never prevent a task from running.
            }
        }
    }

    public static OperatorTaskJournal Start(
        string task,
        OperatorSettings settings)
    {
        return new OperatorTaskJournal(task, settings);
    }

    public void Record(
        string kind,
        string message,
        string? tool = null,
        string? arguments = null,
        string? result = null)
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            var entry = new
            {
                time_utc = DateTime.UtcNow,
                run_id = RunId,
                kind,
                message,
                tool,
                arguments = RedactArguments(arguments),
                result = SummarizeResult(result),
                task = kind == "run_start" ? _taskPreview : null
            };

            string json = JsonSerializer.Serialize(entry);

            lock (_sync)
            {
                File.AppendAllText(
                    FilePath,
                    json + Environment.NewLine
                );
            }
        }
        catch
        {
            // Journaling is best-effort.
        }
    }

    public void Finish(
        string state,
        string result)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;

        Record(
            "run_end",
            $"Task finished with state '{state}'.",
            result: result
        );

        if (!_enabled)
        {
            return;
        }

        try
        {
            string indexPath = Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(FilePath)!)!,
                "index.jsonl"
            );

            var summary = new
            {
                run_id = RunId,
                started_utc = StartedUtc,
                finished_utc = DateTime.UtcNow,
                state,
                task = _taskPreview,
                result = SummarizeResult(result),
                journal = FilePath
            };

            lock (_sync)
            {
                File.AppendAllText(
                    indexPath,
                    JsonSerializer.Serialize(summary) + Environment.NewLine
                );
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        // No unmanaged resources. The type is IDisposable so callers can use
        // a scoped lifetime and reliably finish a run before returning.
    }

    public static string RedactArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return "";
        }

        string lower = arguments.ToLowerInvariant();

        string[] sensitiveHints =
        [
            "password",
            "passcode",
            "secret",
            "api_key",
            "api key",
            "access token",
            "authorization",
            "bearer ",
            "credit card",
            "card number",
            "cvv",
            "cvc",
            "security code",
            "one-time code",
            "otp",
            "2fa code"
        ];

        foreach (string hint in sensitiveHints)
        {
            if (lower.Contains(hint, StringComparison.Ordinal))
            {
                return "[REDACTED: sensitive tool arguments]";
            }
        }

        return arguments.Length <= 4000
            ? arguments
            : arguments[..4000] + "...[truncated]";
    }

    private static string SanitizeTask(string task)
    {
        string redacted = RedactArguments(task);

        return redacted.Length <= 2000
            ? redacted
            : redacted[..2000] + "...[truncated]";
    }

    private static string SummarizeResult(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return "";
        }

        string firstLine =
            result.Replace("\r", "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];

        return firstLine.Length <= 500
            ? firstLine
            : firstLine[..500] + "...[truncated]";
    }
}

public static class OperatorSafetyPolicy
{
    private static readonly HashSet<string> ClickLikeTools =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "windows_click_control",
            "browser_click",
            "browser_role_click",
            "browser_mouse_click",
            "browser_mouse_double_click",
            "browser_page_key",
            "press_key"
        };

    private static readonly HashSet<string> InputTools =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "type_text",
            "windows_set_control_value",
            "windows_replace_document_text",
            "browser_fill",
            "browser_type",
            "browser_role_fill"
        };

    private static readonly string[] HighConsequenceHints =
    [
        "buy now",
        "place order",
        "confirm purchase",
        "pay now",
        "send payment",
        "transfer money",
        "wire transfer",
        "delete account",
        "close account",
        "confirm deletion",
        "permanently delete",
        "empty recycle bin",
        "format drive",
        "change password",
        "reset password",
        "disable two-factor",
        "disable 2fa",
        "remove 2fa",
        "sign contract",
        "e-sign",
        "submit legal"
    ];

    private static readonly string[] CredentialHints =
    [
        "password",
        "passcode",
        "credit card",
        "card number",
        "cvv",
        "cvc",
        "security code",
        "one-time code",
        "otp",
        "2fa code",
        "api key",
        "secret key",
        "access token"
    ];

    public static bool CanExecute(
        string userTask,
        string toolName,
        string arguments,
        OperatorSettings settings,
        out string reason)
    {
        if (!settings.SafeMode)
        {
            reason = "";
            return true;
        }

        if (
            !settings.AllowBrowserCoordinateFallback
            &&
            (toolName.Equals("browser_mouse_click", StringComparison.OrdinalIgnoreCase)
             || toolName.Equals("browser_mouse_double_click", StringComparison.OrdinalIgnoreCase)
             || toolName.Equals("browser_mouse_move", StringComparison.OrdinalIgnoreCase))
        )
        {
            reason =
                "Browser coordinate interaction is disabled by Operator AI safe-mode configuration.";

            return false;
        }

        if (
            !settings.AllowKeyboardFallback
            &&
            (toolName.Equals("press_key", StringComparison.OrdinalIgnoreCase)
             || toolName.Equals("type_text", StringComparison.OrdinalIgnoreCase))
        )
        {
            reason =
                "Keyboard fallback is disabled by Operator AI safe-mode configuration.";

            return false;
        }

        string combined =
            $"{toolName} {arguments}".ToLowerInvariant();

        if (ClickLikeTools.Contains(toolName))
        {
            foreach (string hint in HighConsequenceHints)
            {
                if (combined.Contains(hint, StringComparison.Ordinal))
                {
                    reason =
                        $"Safe mode blocked a high-consequence final action matching '{hint}'.";

                    return false;
                }
            }
        }

        if (InputTools.Contains(toolName))
        {
            foreach (string hint in CredentialHints)
            {
                if (combined.Contains(hint, StringComparison.Ordinal))
                {
                    reason =
                        $"Safe mode blocked entry of a credential or highly sensitive value matching '{hint}'.";

                    return false;
                }
            }
        }

        reason = "";
        return true;
    }
}
