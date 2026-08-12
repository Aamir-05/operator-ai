using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Operator.Tools;

public static class WindowsTools
{
    private static readonly Dictionary<string, string> AllowedApps =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["notepad"] = "notepad.exe",
            ["calculator"] = "calc.exe",
            ["edge"] = "msedge.exe"
        };

    public static string OpenApplication(string application)
    {
        try
        {
            if (!AllowedApps.TryGetValue(application, out string? executable))
            {
                return $"BLOCKED: Application '{application}' is not allowed.";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true
            });

            return $"SUCCESS: Opened {application}.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    public static string CreateDesktopFolder(string folderName)
    {
        try
        {
            string desktop = GetDesktopPath();

            string safeFolderName =
                Path.GetFileName(folderName);

            if (string.IsNullOrWhiteSpace(safeFolderName))
            {
                return "ERROR: Invalid folder name.";
            }

            string fullPath =
                Path.Combine(desktop, safeFolderName);

            Directory.CreateDirectory(fullPath);

            return Directory.Exists(fullPath)
                ? $"SUCCESS: Folder created at {fullPath}"
                : "ERROR: Folder could not be verified.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    public static string CreateDesktopFile(
        string relativePath,
        string content)
    {
        try
        {
            string fullPath =
                GetSafeDesktopPath(relativePath);

            string? directory =
                Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);

            return File.Exists(fullPath)
                ? $"SUCCESS: File created at {fullPath}"
                : "ERROR: File could not be verified.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    public static string ReadDesktopFile(
        string relativePath)
    {
        try
        {
            string fullPath =
                GetSafeDesktopPath(relativePath);

            if (!File.Exists(fullPath))
            {
                return $"NOT_FOUND: {fullPath}";
            }

            string content =
                File.ReadAllText(fullPath);

            return
                $"SUCCESS: File found.\n" +
                $"Path: {fullPath}\n" +
                $"Content:\n{content}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    public static string DesktopFileExists(
        string relativePath)
    {
        try
        {
            string fullPath =
                GetSafeDesktopPath(relativePath);

            return File.Exists(fullPath)
                ? $"SUCCESS: File exists at {fullPath}"
                : $"NOT_FOUND: {fullPath}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    public static string ListDesktop()
    {
        try
        {
            string desktop = GetDesktopPath();

            string[] directories =
                Directory.GetDirectories(desktop);

            string[] files =
                Directory.GetFiles(desktop);

            List<string> results = new();

            results.Add("FOLDERS:");

            foreach (string directory in directories)
            {
                results.Add(
                    $"[DIR] {Path.GetFileName(directory)}"
                );
            }

            results.Add("");
            results.Add("FILES:");

            foreach (string file in files)
            {
                results.Add(
                    $"[FILE] {Path.GetFileName(file)}"
                );
            }

            return string.Join(
                Environment.NewLine,
                results
            );
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static string GetDesktopPath()
    {
        return Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory
        );
    }

    private static string GetSafeDesktopPath(
        string relativePath)
    {
        string desktop =
            Path.GetFullPath(GetDesktopPath());

        string candidate =
            Path.GetFullPath(
                Path.Combine(desktop, relativePath)
            );

        if (!candidate.StartsWith(
                desktop + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                candidate,
                desktop,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Path is outside the allowed Desktop directory."
            );
        }

        return candidate;
    }
}