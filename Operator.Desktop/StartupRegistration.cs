using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace Operator.Desktop;

public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "OperatorAI";

    public static void Apply(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        string executable = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Could not determine Operator AI executable path.");

        key.SetValue(ValueName, $"\"{executable}\" --background");
    }
}
