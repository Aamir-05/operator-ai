using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7D-2
    // AUTONOMOUS REAL NOTEPAD WRITE/SAVE WORKFLOW
    // =========================================================

    private async void RealWriteAgent07DTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        const string folderName =
            "OperatorAI-07D2";

        const string fileName =
            "autonomous-write-proof.txt";

        const string initialContent =
            "INITIAL-07D2";

        const string expectedContent =
            "Operator AI 0.7D autonomous real Notepad write/save verification.";

        string relativePath =
            Path.Combine(
                folderName,
                fileName
            );

        string desktop =
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory
            );

        string folderPath =
            Path.Combine(
                desktop,
                folderName
            );

        string filePath =
            Path.Combine(
                folderPath,
                fileName
            );

        object actionLock =
            new object();

        HashSet<string> agentActions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7D AUTONOMOUS REAL WRITE/SAVE TEST"
            );

            Log(
                "Applications: real Notepad + real File Explorer"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // SAFE TEST FILE
            // =================================================

            Log(
                "[SETUP] Creating dedicated 0.7D-2 test file..."
            );

            Directory.CreateDirectory(
                folderPath
            );

            File.WriteAllText(
                filePath,
                initialContent
            );

            if (!File.Exists(
                    filePath))
            {
                RealWriteAgent07D_Fail(
                    "Initial test file creation"
                );

                return;
            }

            string setupContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    setupContent,
                    initialContent,
                    StringComparison.Ordinal))
            {
                RealWriteAgent07D_Fail(
                    "Initial test file verification"
                );

                return;
            }

            Log(
                $"PASS: Created {filePath}"
            );

            // =================================================
            // OPEN REAL NOTEPAD
            // =================================================

            Log(
                "[SETUP] Opening dedicated file in real Notepad..."
            );

            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        "notepad.exe",

                    Arguments =
                        $"\"{filePath}\"",

                    UseShellExecute =
                        true
                }
            );

            string waitNotepad =
                await Task.Run(
                    () =>
                        WindowsWindowTools.WaitForWindow(
                            fileName,
                            15
                        )
                );

            Log(
                waitNotepad
            );

            if (RealWrite07D_IsFailure(
                    waitNotepad))
            {
                RealWriteAgent07D_Fail(
                    "Notepad startup"
                );

                return;
            }

            // =================================================
            // OPEN REAL EXPLORER
            // =================================================

            Log(
                "[SETUP] Opening dedicated folder in real File Explorer..."
            );

            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        "explorer.exe",

                    Arguments =
                        $"\"{folderPath}\"",

                    UseShellExecute =
                        true
                }
            );

            string waitExplorer =
                await Task.Run(
                    () =>
                        WindowsWindowTools.WaitForWindow(
                            folderName,
                            15
                        )
                );

            Log(
                waitExplorer
            );

            if (RealWrite07D_IsFailure(
                    waitExplorer))
            {
                RealWriteAgent07D_Fail(
                    "File Explorer startup"
                );

                return;
            }

            await Task.Delay(
                900
            );

            // =================================================
            // PRE-TEST REAL WINDOWS
            // =================================================

            string initialWindows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                initialWindows
            );

            if (
                RealWrite07D_IsFailure(
                    initialWindows)
                ||
                !initialWindows.Contains(
                    fileName,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !initialWindows.Contains(
                    folderName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                RealWriteAgent07D_Fail(
                    "Initial real-application discovery"
                );

                return;
            }

            Log(
                "PASS: Real Notepad and Explorer are available."
            );

            // =================================================
            // AUTONOMOUS AGENT
            // =================================================

            Log(
                "Starting autonomous 0.7D real write/save workflow..."
            );

            RealWriteAgent07D agent =
                new RealWriteAgent07D();

            using CancellationTokenSource timeout =
                new CancellationTokenSource(
                    TimeSpan.FromMinutes(4)
                );

            string result =
                await agent.RunAsync(
                    $"""
                    Perform the final controlled 0.7D autonomous
                    real-application write/save workflow.

                    A real Notepad document already exists and is open.

                    NOTEPAD FILENAME:

                    {fileName}

                    A real File Explorer window is open at the dedicated
                    folder:

                    {folderName}

                    DESKTOP-RELATIVE FILE PATH:

                    {relativePath}

                    Replace the entire Notepad document with exactly this text:

                    {expectedContent}

                    Perform this exact workflow:

                    1. List the available top-level Windows.

                    2. Focus the Notepad window whose title contains:

                       {fileName}

                    3. Verify that Notepad window is foreground.

                    4. Inspect its native controls.

                    5. Replace the complete Notepad document contents with
                       exactly:

                       {expectedContent}

                    6. Read the Notepad document through UI Automation and
                       verify it contains exactly the requested text.

                    7. Save the existing document using:

                       windows_save_document

                    8. Verify the real Desktop file exists at:

                       {relativePath}

                    9. Read the real saved file.

                    Verify its content is:

                       {expectedContent}

                    10. Focus the File Explorer window whose title contains:

                        {folderName}

                    11. Verify Explorer became foreground.

                    12. Inspect Explorer native controls.

                    13. Locate the saved file using:

                        {fileName}

                    If Explorer hides the extension, try:

                        {Path.GetFileNameWithoutExtension(fileName)}

                    Do not open the file.

                    14. Return to the Notepad window containing:

                        {fileName}

                    15. Verify Notepad became foreground again.

                    16. Read the Notepad document one final time and verify
                        it still contains:

                        {expectedContent}

                    Do not modify any other file.

                    Do not rename, move, or delete anything.

                    Do not use browser automation.

                    Do not use screen coordinates.

                    Do not claim success unless both the actual saved file
                    and the final Notepad document have been verified.
                    """,
                    message =>
                    {
                        lock (actionLock)
                        {
                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_list_windows"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_focus_window"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_verify_foreground"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_list_controls"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_replace_document_text"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_read_document_text"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_save_document"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "desktop_file_exists"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "read_desktop_file"
                            );

                            TrackRealWriteAgent07DAction(
                                agentActions,
                                message,
                                "windows_find_control_any"
                            );
                        }

                        Dispatcher.Invoke(
                            () =>
                                Log(
                                    $"[AGENT] {message}"
                                )
                        );
                    },
                    timeout.Token
                );

            Log(
                $"Agent result: {result}"
            );

            if (RealWrite07D_IsFailure(
                    result))
            {
                RealWriteAgent07D_Fail(
                    "Autonomous agent execution"
                );

                return;
            }

            // =================================================
            // REQUIRED AUTONOMOUS ACTIONS
            // =================================================

            string[] requiredActions =
            [
                "windows_list_windows",
                "windows_focus_window",
                "windows_verify_foreground",
                "windows_list_controls",
                "windows_replace_document_text",
                "windows_read_document_text",
                "windows_save_document",
                "desktop_file_exists",
                "read_desktop_file",
                "windows_find_control_any"
            ];

            lock (actionLock)
            {
                foreach (
                    string requiredAction
                    in requiredActions)
                {
                    if (!agentActions.Contains(
                            requiredAction))
                    {
                        RealWriteAgent07D_Fail(
                            $"Agent did not exercise {requiredAction}"
                        );

                        return;
                    }
                }
            }

            Log(
                "PASS: Required autonomous real-write tools were exercised."
            );

            // =================================================
            // INDEPENDENT FILESYSTEM VERIFICATION
            // =================================================

            Log(
                "Independently verifying exact saved filesystem content..."
            );

            bool correctFile =
                await RealWrite07D_WaitForFileContentAsync(
                    filePath,
                    expectedContent,
                    10
                );

            if (!correctFile)
            {
                RealWriteAgent07D_Fail(
                    "Independent saved filesystem content"
                );

                return;
            }

            string independentFileContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    independentFileContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealWriteAgent07D_Fail(
                    "Independent exact file-content verification"
                );

                return;
            }

            Log(
                "PASS: Exact real filesystem content independently verified."
            );

            // =================================================
            // INDEPENDENT EXPLORER VERIFICATION
            // =================================================

            Log(
                "Independently verifying saved file in real File Explorer..."
            );

            string explorerFocus =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            folderName
                        )
                );

            Log(
                explorerFocus
            );

            if (RealWrite07D_IsFailure(
                    explorerFocus))
            {
                RealWriteAgent07D_Fail(
                    "Independent Explorer focus"
                );

                return;
            }

            string explorerForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            folderName
                        )
                );

            Log(
                explorerForeground
            );

            if (RealWrite07D_IsFailure(
                    explorerForeground))
            {
                RealWriteAgent07D_Fail(
                    "Independent Explorer foreground verification"
                );

                return;
            }

            string explorerFile =
                await RealWrite07D_FindExplorerFileAsync(
                    fileName
                );

            Log(
                explorerFile
            );

            if (RealWrite07D_IsFailure(
                    explorerFile))
            {
                RealWriteAgent07D_Fail(
                    "Independent Explorer saved-file discovery"
                );

                return;
            }

            Log(
                "PASS: Saved file independently located in Explorer."
            );

            // =================================================
            // INDEPENDENT NOTEPAD VERIFICATION
            // =================================================

            Log(
                "Independently returning to real Notepad..."
            );

            string notepadFocus =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            fileName
                        )
                );

            Log(
                notepadFocus
            );

            if (RealWrite07D_IsFailure(
                    notepadFocus))
            {
                RealWriteAgent07D_Fail(
                    "Independent Notepad focus"
                );

                return;
            }

            string notepadForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            fileName
                        )
                );

            Log(
                notepadForeground
            );

            if (RealWrite07D_IsFailure(
                    notepadForeground))
            {
                RealWriteAgent07D_Fail(
                    "Independent Notepad foreground verification"
                );

                return;
            }

            string finalNotepadText =
                await RealWrite07D_ReadNotepadTextAsync(
                    expectedContent
                );

            Log(
                finalNotepadText
            );

            if (
                RealWrite07D_IsFailure(
                    finalNotepadText)
                ||
                !finalNotepadText.Contains(
                    expectedContent,
                    StringComparison.Ordinal)
            )
            {
                RealWriteAgent07D_Fail(
                    "Independent final Notepad text verification"
                );

                return;
            }

            Log(
                "PASS: Final real Notepad document independently verified."
            );

            // =================================================
            // FINAL FILE SAFETY
            // =================================================

            string finalFile =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    finalFile,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealWriteAgent07D_Fail(
                    "Final real-file persistence verification"
                );

                return;
            }

            // =================================================
            // SUCCESS
            // =================================================

            Log(
                "============================================================"
            );

            Log(
                "SUCCESS: VERSION 0.7D AUTONOMOUS REAL WRITE/SAVE TEST PASSED."
            );

            Log(
                "Agent real-window discovery: PASS"
            );

            Log(
                "Agent Notepad targeting: PASS"
            );

            Log(
                "Agent Notepad foreground verification: PASS"
            );

            Log(
                "Agent Notepad native inspection: PASS"
            );

            Log(
                "Agent autonomous document replacement: PASS"
            );

            Log(
                "Agent editor text verification: PASS"
            );

            Log(
                "Agent real document save: PASS"
            );

            Log(
                "Agent filesystem existence verification: PASS"
            );

            Log(
                "Agent saved-file content verification: PASS"
            );

            Log(
                "Agent File Explorer switching: PASS"
            );

            Log(
                "Agent Explorer foreground verification: PASS"
            );

            Log(
                "Agent Explorer UI inspection: PASS"
            );

            Log(
                "Agent Explorer saved-file discovery: PASS"
            );

            Log(
                "Agent return-to-Notepad workflow: PASS"
            );

            Log(
                "Agent final Notepad text verification: PASS"
            );

            Log(
                "Independent exact filesystem verification: PASS"
            );

            Log(
                "Independent File Explorer verification: PASS"
            );

            Log(
                "Independent Notepad verification: PASS"
            );

            Log(
                "Final saved-state persistence: PASS"
            );

            Log(
                "VERSION 0.7D: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (OperationCanceledException)
        {
            RealWriteAgent07D_Fail(
                "Autonomous 0.7D test timed out"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7D AUTONOMOUS REAL WRITE TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // ACTION TRACKING
    // =========================================================

    private static void TrackRealWriteAgent07DAction(
        HashSet<string> actions,
        string message,
        string actionName)
    {
        if (message.Contains(
                $"[ACTION] {actionName}",
                StringComparison.OrdinalIgnoreCase))
        {
            actions.Add(
                actionName
            );
        }
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void RealWriteAgent07D_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7D AUTONOMOUS REAL WRITE/SAVE TEST - {testName}"
        );

        Log(
            "Test stopped at first failed requirement."
        );

        Log(
            "============================================================"
        );
    }
}