using System;
using System.IO;
using System.Text.Json;

namespace Operator.Desktop;

public sealed class RemoteSettings
{
    public bool Enabled { get; set; }
    public string ProjectUrl { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = Environment.MachineName;
    public bool StartWithWindows { get; set; }
    public int PollIntervalSeconds { get; set; } = 2;
    public bool CaptureScreenshotAfterRemoteTask { get; set; } = true;

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OperatorAI",
        "remote.settings.json");

    public static RemoteSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                RemoteSettings defaults = new();
                defaults.Save();
                return defaults;
            }

            RemoteSettings settings = JsonSerializer.Deserialize<RemoteSettings>(File.ReadAllText(SettingsPath)) ?? new();
            settings.PollIntervalSeconds = Math.Clamp(settings.PollIntervalSeconds, 1, 30);
            if (string.IsNullOrWhiteSpace(settings.DeviceName)) settings.DeviceName = Environment.MachineName;
            return settings;
        }
        catch
        {
            return new RemoteSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
