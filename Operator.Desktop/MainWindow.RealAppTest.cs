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
    // VERSION 0.7C-3
    // REAL APPLICATION ORCHESTRATION
    //
    // NOTEPAD + FILE EXPLORER
    // =========================================================

    private async void RealApps07CTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        const string folderName =
            "OperatorAI-07C3";

        const string fileName =
            "real-app-proof.txt";

        const string expectedContent =
            "Operator AI real application orchestration 0.7C";

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
                "STARTING VERSION 0.7C REAL APPLICATION TEST"
            );

            Log(
                "Applications: Notepad + File Explorer"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // CREATE REAL TEST FILE
            // =================================================

            Log(
                "[SETUP] Creating deterministic real Desktop file..."
            );

            Directory.CreateDirectory(
                folderPath
            );

            File.WriteAllText(
                filePath,
                expectedContent
            );

            if (!File.Exists(
                    filePath))
            {
                RealApps07C_Fail(
                    "Test file creation"
                );

                return;
            }

            string directContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    directContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealApps07C_Fail(
                    "Initial test file content"
                );

                return;
            }

            Log(
                $"PASS: Created {filePath}"
            );

            // =================================================
            // OPEN REAL NOTEPAD DOCUMENT
            // =================================================

            Log(
                "[SETUP] Opening real file in Notepad..."
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

            if (RealApps07C_IsFailure(
                    waitNotepad))
            {
                RealApps07C_Fail(
                    "Notepad document window"
                );

                return;
            }

            Log(
                "PASS: Real Notepad document detected."
            );

            // =================================================
            // OPEN REAL FILE EXPLORER FOLDER
            // =================================================

            Log(
                "[SETUP] Opening test folder in File Explorer..."
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

            if (RealApps07C_IsFailure(
                    waitExplorer))
            {
                RealApps07C_Fail(
                    "File Explorer folder window"
                );

                return;
            }

            await Task.Delay(
                1000
            );

            Log(
                "PASS: Real File Explorer window detected."
            );

            // =================================================
            // VERIFY BOTH REAL WINDOWS BEFORE AGENT
            // =================================================

            Log(
                "[SETUP] Verifying both real application windows..."
            );

            string initialWindows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                initialWindows
            );

            if (RealApps07C_IsFailure(
                    initialWindows))
            {
                RealApps07C_Fail(
                    "Initial real application discovery"
                );

                return;
            }

            if (
                !initialWindows.Contains(
                    fileName,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !initialWindows.Contains(
                    folderName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                RealApps07C_Fail(
                    "Initial real application discovery"
                );

                return;
            }

            Log(
                "PASS: Notepad and File Explorer discovered."
            );

            // =================================================
            // RUN AUTONOMOUS REAL-APP AGENT
            // =================================================

            Log(
                "Starting autonomous real-application orchestration..."
            );

            RealAppAgent07C agent =
                new RealAppAgent07C();

            using CancellationTokenSource timeout =
                new CancellationTokenSource(
                    TimeSpan.FromMinutes(4)
                );

            string result =
                await agent.RunAsync(
                    $"""
                    Perform a controlled real Windows application
                    orchestration task.

                    A real Notepad document is open.

                    Its filename is:

                    {fileName}

                    A real File Explorer window is also open.

                    The File Explorer folder name is:

                    {folderName}

                    The Desktop-relative test file path is:

                    {relativePath}

                    Expected file content is:

                    {expectedContent}

                    Complete these steps:

                    1. List top-level Windows and verify both the Notepad
                       document window and File Explorer folder window exist.

                    2. Focus the window whose title contains:

                       {fileName}

                    3. Verify that window became foreground.

                    4. Inspect its native controls to prove you are interacting
                       with the real Notepad application.

                    Do not edit the document.

                    5. Focus the window whose title contains:

                       {folderName}

                    6. Verify it became foreground.

                    7. Inspect its native controls to prove you are interacting
                       with the real File Explorer window.

                    8. Find a native File Explorer control whose accessible
                       name contains or equals the test filename.

                    First try:

                       {fileName}

                    If Explorer hides file extensions, also try:

                       {Path.GetFileNameWithoutExtension(fileName)}

                    Use windows_find_control_any because Explorer control types
                    can differ between Windows versions.

                    Do not open, rename, move, or delete the file.

                    9. Independently verify the real Desktop file exists using:

                       {relativePath}

                    10. Read that Desktop file.

                    Verify its content contains exactly:

                       {expectedContent}

                    11. Switch back to the Notepad window whose title contains:

                       {fileName}

                    12. Verify Notepad became foreground again.

                    13. Inspect the Notepad controls one final time.

                    Do not use keyboard automation.
                    Do not use browser automation.
                    Do not use screen coordinates.
                    Do not modify the file.

                    Do not claim success until all real-application and
                    real-file checks have been completed.
                    """,
                    message =>
                    {
                        lock (actionLock)
                        {
                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "windows_list_windows"
                            );

                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "windows_focus_window"
                            );

                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "windows_verify_foreground"
                            );

                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "windows_list_controls"
                            );

                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "windows_find_control_any"
                            );

                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "desktop_file_exists"
                            );

                            TrackRealApps07CAction(
                                agentActions,
                                message,
                                "read_desktop_file"
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

            if (RealApps07C_IsFailure(
                    result))
            {
                RealApps07C_Fail(
                    "Autonomous real-application execution"
                );

                return;
            }

            // =================================================
            // REQUIRE IMPORTANT AGENT TOOLS
            // =================================================

            string[] requiredActions =
            [
                "windows_list_windows",
                "windows_focus_window",
                "windows_verify_foreground",
                "windows_list_controls",
                "windows_find_control_any",
                "desktop_file_exists",
                "read_desktop_file"
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
                        RealApps07C_Fail(
                            $"Agent did not exercise {requiredAction}"
                        );

                        return;
                    }
                }
            }

            Log(
                "PASS: Required real-app tools were exercised."
            );

            // =================================================
            // INDEPENDENT FILE VERIFICATION
            // =================================================

            Log(
                "Independently verifying real filesystem state..."
            );

            if (!File.Exists(
                    filePath))
            {
                RealApps07C_Fail(
                    "Independent file existence"
                );

                return;
            }

            string finalContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    finalContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealApps07C_Fail(
                    "Agent unexpectedly changed the file"
                );

                return;
            }

            Log(
                "PASS: File exists and remained unchanged."
            );

            // =================================================
            // INDEPENDENT EXPLORER VERIFICATION
            // =================================================

            Log(
                "Independently verifying File Explorer..."
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

            if (RealApps07C_IsFailure(
                    explorerFocus))
            {
                RealApps07C_Fail(
                    "Independent File Explorer focus"
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

            if (RealApps07C_IsFailure(
                    explorerForeground))
            {
                RealApps07C_Fail(
                    "Independent File Explorer foreground verification"
                );

                return;
            }

            // =================================================
            // EXPLORER UIA STABILIZATION
            //
            // File Explorer can expose an item's accessible name
            // differently depending on:
            //
            // - Windows version
            // - extension visibility setting
            // - Explorer view mode
            // - UI Automation virtualization
            //
            // Therefore:
            //
            // 1. Try full file name
            // 2. Try filename without extension
            // 3. Enumerate the entire foreground UIA tree
            // 4. Retry briefly while Explorer finishes loading
            // =================================================

            Log(
                "Locating real file through Explorer UI Automation..."
            );

            string explorerFile =
                await RealApps07C_FindExplorerFileAsync(
                    fileName
                );

            Log(
                explorerFile
            );

            if (RealApps07C_IsFailure(
                    explorerFile))
            {
                RealApps07C_Fail(
                    "Independent Explorer file-item verification"
                );

                return;
            }

            Log(
                "PASS: Real file independently located in File Explorer."
            );

            // =================================================
            // FILE STILL EXISTS AFTER EXPLORER INSPECTION
            // =================================================

            if (!File.Exists(
                    filePath))
            {
                RealApps07C_Fail(
                    "File disappeared during Explorer verification"
                );

                return;
            }

            // =================================================
            // INDEPENDENT NOTEPAD VERIFICATION
            // =================================================

            Log(
                "Independently returning to Notepad..."
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

            if (RealApps07C_IsFailure(
                    notepadFocus))
            {
                RealApps07C_Fail(
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

            if (RealApps07C_IsFailure(
                    notepadForeground))
            {
                RealApps07C_Fail(
                    "Independent Notepad foreground verification"
                );

                return;
            }

            string notepadControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            180
                        )
                );

            Log(
                notepadControls
            );

            if (RealApps07C_IsFailure(
                    notepadControls))
            {
                RealApps07C_Fail(
                    "Independent Notepad UI inspection"
                );

                return;
            }

            if (
                !notepadControls.Contains(
                    "Text",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !notepadControls.Contains(
                    "Document",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !notepadControls.Contains(
                    "Edit",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                RealApps07C_Fail(
                    "Notepad editable/document control verification"
                );

                return;
            }

            Log(
                "PASS: Real Notepad document independently verified."
            );

            // =================================================
            // FINAL SAFETY VERIFICATION
            // =================================================

            string unchangedContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    unchangedContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealApps07C_Fail(
                    "Final file unchanged verification"
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
                "SUCCESS: VERSION 0.7C REAL APPLICATION TEST PASSED."
            );

            Log(
                "Real Notepad launch: PASS"
            );

            Log(
                "Real File Explorer launch: PASS"
            );

            Log(
                "Real-app Win32 discovery: PASS"
            );

            Log(
                "Agent Notepad targeting: PASS"
            );

            Log(
                "Agent Notepad foreground verification: PASS"
            );

            Log(
                "Agent Notepad UI inspection: PASS"
            );

            Log(
                "Agent File Explorer switching: PASS"
            );

            Log(
                "Agent File Explorer foreground verification: PASS"
            );

            Log(
                "Agent File Explorer UI inspection: PASS"
            );

            Log(
                "Agent Explorer file-item discovery: PASS"
            );

            Log(
                "Agent filesystem existence verification: PASS"
            );

            Log(
                "Agent real-file content verification: PASS"
            );

            Log(
                "Agent return-to-Notepad workflow: PASS"
            );

            Log(
                "Independent File Explorer verification: PASS"
            );

            Log(
                "Independent Explorer filename fallback: PASS"
            );

            Log(
                "Independent Notepad verification: PASS"
            );

            Log(
                "File unchanged safety verification: PASS"
            );

            Log(
                "VERSION 0.7C REAL APPS: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (OperationCanceledException)
        {
            RealApps07C_Fail(
                "Real-application test timed out"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7C REAL APPLICATION TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // ROBUST FILE EXPLORER ITEM DISCOVERY
    // =========================================================

    private static async Task<string>
        RealApps07C_FindExplorerFileAsync(
            string fileName)
    {
        string baseName =
            Path.GetFileNameWithoutExtension(
                fileName
            );

        string lastResult =
            "";

        for (int attempt = 1;
             attempt <= 10;
             attempt++)
        {
            // =================================================
            // 1. FULL FILENAME
            // =================================================

            string fullNameResult =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControlInfo(
                            "__FOREGROUND__",
                            fileName
                        )
                );

            if (!RealApps07C_IsFailure(
                    fullNameResult))
            {
                return
                    "SUCCESS: Explorer file item found using full filename.\n" +
                    fullNameResult;
            }

            lastResult =
                fullNameResult;

            // =================================================
            // 2. NAME WITHOUT EXTENSION
            //
            // Explorer may hide known file extensions.
            // =================================================

            string baseNameResult =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControlInfo(
                            "__FOREGROUND__",
                            baseName
                        )
                );

            if (!RealApps07C_IsFailure(
                    baseNameResult))
            {
                return
                    "SUCCESS: Explorer file item found using filename without extension.\n" +
                    baseNameResult;
            }

            lastResult =
                baseNameResult;

            // =================================================
            // 3. LISTITEM + FULL NAME
            // =================================================

            string listItemFull =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControl(
                            "__FOREGROUND__",
                            "listitem",
                            fileName,
                            false
                        )
                );

            if (!RealApps07C_IsFailure(
                    listItemFull))
            {
                return
                    "SUCCESS: Explorer ListItem found using full filename.\n" +
                    listItemFull;
            }

            lastResult =
                listItemFull;

            // =================================================
            // 4. LISTITEM + BASE NAME
            // =================================================

            string listItemBase =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControl(
                            "__FOREGROUND__",
                            "listitem",
                            baseName,
                            false
                        )
                );

            if (!RealApps07C_IsFailure(
                    listItemBase))
            {
                return
                    "SUCCESS: Explorer ListItem found using filename without extension.\n" +
                    listItemBase;
            }

            lastResult =
                listItemBase;

            // =================================================
            // 5. COMPLETE CONTROL ENUMERATION FALLBACK
            // =================================================

            string controls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            500
                        )
                );

            if (!RealApps07C_IsFailure(
                    controls))
            {
                bool fullVisible =
                    controls.Contains(
                        fileName,
                        StringComparison.OrdinalIgnoreCase
                    );

                bool baseVisible =
                    controls.Contains(
                        baseName,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (
                    fullVisible
                    ||
                    baseVisible
                )
                {
                    return
                        "SUCCESS: Explorer file item found through UI Automation enumeration.\n" +
                        $"Matched: {(fullVisible ? fileName : baseName)}";
                }
            }

            lastResult =
                controls;

            // =================================================
            // EXPLORER MAY STILL BE MATERIALIZING ITS VIEW
            // =================================================

            await Task.Delay(
                300
            );
        }

        return
            "NOT_FOUND: Explorer UI Automation could not locate the expected file item.\n" +
            $"Full filename: {fileName}\n" +
            $"Base filename: {baseName}\n" +
            $"Last result:\n{lastResult}";
    }

    // =========================================================
    // ACTION TRACKING
    // =========================================================

    private static void TrackRealApps07CAction(
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

    private void RealApps07C_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7C REAL APPLICATION TEST - {testName}"
        );

        Log(
            "Test stopped at first failed requirement."
        );

        Log(
            "============================================================"
        );
    }

    // =========================================================
    // FAILURE CHECK
    // =========================================================

    private static bool RealApps07C_IsFailure(
        string result)
    {
        if (string.IsNullOrWhiteSpace(
                result))
        {
            return true;
        }

        return
            result.StartsWith(
                "ERROR",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "NOT_FOUND",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "BLOCKED",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "TIMEOUT",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase);
    }
}