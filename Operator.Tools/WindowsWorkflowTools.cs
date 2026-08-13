using System;
using System.IO;
using System.Threading;

namespace Operator.Tools;

public static class WindowsWorkflowTools
{
    // =========================================================
    // SAVE ACTIVE DOCUMENT AS DESKTOP FILE
    // =========================================================

    public static string SaveActiveDocumentAsDesktopFile(
        string relativePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return "ERROR: File path is empty.";
            }

            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory
                );

            string fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        desktop,
                        relativePath
                    )
                );

            string desktopFullPath =
                Path.GetFullPath(desktop);

            // Security boundary:
            // keep saves inside Desktop.
            if (!fullPath.StartsWith(
                    desktopFullPath +
                    Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "BLOCKED: Save path must remain inside Desktop.";
            }

            string? directory =
                Path.GetDirectoryName(
                    fullPath
                );

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(
                    directory
                );
            }

            // Remove existing test target so verification
            // proves this workflow created the file.
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    return
                        $"ERROR: Existing target file could not be removed: {ex.Message}";
                }
            }

            // -------------------------------------------------
            // STEP 1
            // Force Save As
            // -------------------------------------------------

            string shortcutResult =
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                );

            if (!shortcutResult.StartsWith(
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    $"ERROR: Could not open Save As. {shortcutResult}";
            }

            // -------------------------------------------------
            // STEP 2
            // Wait for Save As dialog
            // -------------------------------------------------

            Thread.Sleep(1200);

            bool dialogReady =
                WaitForForegroundChange(
                    maximumAttempts: 5,
                    delayMilliseconds: 400
                );

            if (!dialogReady)
            {
                // We still continue because some dialogs
                // may not expose themselves perfectly to UIA.
                Thread.Sleep(500);
            }

            // -------------------------------------------------
            // STEP 3
            // Select existing filename
            // -------------------------------------------------

            string selectResult =
                WindowsInputTools.PressKey(
                    "CTRL+A"
                );

            if (!selectResult.StartsWith(
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    $"ERROR: Could not select current filename. {selectResult}";
            }

            Thread.Sleep(250);

            // -------------------------------------------------
            // STEP 4
            // Put desired path on clipboard
            // -------------------------------------------------

            System.Windows.Clipboard.SetText(
                fullPath
            );

            Thread.Sleep(200);

            // -------------------------------------------------
            // STEP 5
            // Paste path
            // -------------------------------------------------

            string pasteResult =
                WindowsInputTools.PressKey(
                    "CTRL+V"
                );

            if (!pasteResult.StartsWith(
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    $"ERROR: Could not paste filename. {pasteResult}";
            }

            Thread.Sleep(350);

            // -------------------------------------------------
            // STEP 6
            // Press Enter to save
            // -------------------------------------------------

            string saveResult =
                WindowsInputTools.PressKey(
                    "ENTER"
                );

            if (!saveResult.StartsWith(
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    $"ERROR: Could not submit Save As dialog. {saveResult}";
            }

            // -------------------------------------------------
            // STEP 7
            // Wait for file to appear
            // -------------------------------------------------

            if (WaitForFile(
                    fullPath,
                    attempts: 8,
                    delayMilliseconds: 350))
            {
                return
                    $"SUCCESS: File saved and verified at {fullPath}";
            }

            // -------------------------------------------------
            // STEP 8
            // Possible overwrite/confirmation dialog
            // -------------------------------------------------

            WindowsInputTools.PressKey(
                "ENTER"
            );

            if (WaitForFile(
                    fullPath,
                    attempts: 6,
                    delayMilliseconds: 350))
            {
                return
                    $"SUCCESS: File saved and verified at {fullPath}";
            }

            // -------------------------------------------------
            // STEP 9
            // One recovery attempt
            // -------------------------------------------------

            Thread.Sleep(500);

            WindowsInputTools.PressKey(
                "CTRL+A"
            );

            Thread.Sleep(200);

            System.Windows.Clipboard.SetText(
                fullPath
            );

            Thread.Sleep(200);

            WindowsInputTools.PressKey(
                "CTRL+V"
            );

            Thread.Sleep(300);

            WindowsInputTools.PressKey(
                "ENTER"
            );

            if (WaitForFile(
                    fullPath,
                    attempts: 6,
                    delayMilliseconds: 400))
            {
                return
                    $"SUCCESS: File saved after recovery and verified at {fullPath}";
            }

            return
                $"ERROR: Save workflow completed but file was not found at {fullPath}.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Save workflow failed: {ex.Message}";
        }
    }

    // =========================================================
    // VERIFY FILE
    // =========================================================

    public static string VerifyDesktopFile(
        string relativePath)
    {
        try
        {
            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory
                );

            string fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        desktop,
                        relativePath
                    )
                );

            if (!File.Exists(fullPath))
            {
                return
                    $"NOT_FOUND: {fullPath}";
            }

            long size =
                new FileInfo(fullPath).Length;

            return
                $"SUCCESS: Verified file at {fullPath}. Size: {size} bytes.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT FOR FILE
    // =========================================================

    private static bool WaitForFile(
        string fullPath,
        int attempts,
        int delayMilliseconds)
    {
        for (int attempt = 1;
             attempt <= attempts;
             attempt++)
        {
            if (File.Exists(fullPath))
            {
                return true;
            }

            Thread.Sleep(
                delayMilliseconds
            );
        }

        return false;
    }

    // =========================================================
    // SMALL UI WAIT
    // =========================================================

    private static bool WaitForForegroundChange(
        int maximumAttempts,
        int delayMilliseconds)
    {
        for (int attempt = 1;
             attempt <= maximumAttempts;
             attempt++)
        {
            string inspection =
                WindowsUiTools.InspectWindow(
                    "__FOREGROUND__"
                );

            if (!inspection.StartsWith(
                    "NOT_FOUND",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !inspection.StartsWith(
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Thread.Sleep(
                delayMilliseconds
            );
        }

        return false;
    }
}