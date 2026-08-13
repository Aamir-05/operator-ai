using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7D-1
    // REAL NOTEPAD WRITE + SAVE + EXPLORER VERIFICATION
    // =========================================================

    private async void RealWrite07DTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        const string folderName =
            "OperatorAI-07D1";

        const string fileName =
            "notepad-write-proof.txt";

        const string initialContent =
            "INITIAL-07D1";

        const string expectedContent =
            "Operator AI 0.7D real Notepad write and save verification.";

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

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7D REAL NOTEPAD WRITE/SAVE TEST"
            );

            Log(
                "Applications: Notepad + File Explorer"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // 1. CREATE SAFE TEST FILE
            // =================================================

            Log(
                "[1/12] Creating deterministic test file..."
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
                RealWrite07D_Fail(
                    "Initial test file creation"
                );

                return;
            }

            string initialRead =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    initialRead,
                    initialContent,
                    StringComparison.Ordinal))
            {
                RealWrite07D_Fail(
                    "Initial file verification"
                );

                return;
            }

            Log(
                $"PASS: Created {filePath}"
            );

            // =================================================
            // 2. OPEN REAL NOTEPAD
            // =================================================

            Log(
                "[2/12] Opening real file in Notepad..."
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
                RealWrite07D_Fail(
                    "Notepad window detection"
                );

                return;
            }

            string focusNotepad =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            fileName
                        )
                );

            Log(
                focusNotepad
            );

            if (RealWrite07D_IsFailure(
                    focusNotepad))
            {
                RealWrite07D_Fail(
                    "Notepad focus"
                );

                return;
            }

            string verifyNotepad =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            fileName
                        )
                );

            Log(
                verifyNotepad
            );

            if (RealWrite07D_IsFailure(
                    verifyNotepad))
            {
                RealWrite07D_Fail(
                    "Notepad foreground verification"
                );

                return;
            }

            Log(
                "PASS: Real Notepad document targeted"
            );

            // =================================================
            // 3. INSPECT REAL NOTEPAD
            // =================================================

            Log(
                "[3/12] Inspecting Notepad native controls..."
            );

            string notepadControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            220
                        )
                );

            Log(
                notepadControls
            );

            if (RealWrite07D_IsFailure(
                    notepadControls))
            {
                RealWrite07D_Fail(
                    "Notepad control inspection"
                );

                return;
            }

            bool hasEditorLikeControl =
                notepadControls.Contains(
                    "Type=Edit",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                notepadControls.Contains(
                    "Type=Document",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                notepadControls.Contains(
                    "Text editor",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!hasEditorLikeControl)
            {
                RealWrite07D_Fail(
                    "Notepad editor discovery"
                );

                return;
            }

            Log(
                "PASS: Real Notepad editor structure discovered"
            );

            // =================================================
            // 4. WRITE CONTENT
            //
            // Native ValuePattern first.
            // Verified keyboard fallback only if necessary.
            // =================================================

            Log(
                "[4/12] Writing exact content into Notepad..."
            );

            string writeResult =
                await RealWrite07D_WriteNotepadTextAsync(
                    fileName,
                    expectedContent
                );

            Log(
                writeResult
            );

            if (RealWrite07D_IsFailure(
                    writeResult))
            {
                RealWrite07D_Fail(
                    "Notepad content write"
                );

                return;
            }

            Log(
                "PASS: Notepad content written"
            );

            // =================================================
            // 5. VERIFY EDITOR CONTENT BEFORE SAVE
            // =================================================

            Log(
                "[5/12] Reading Notepad content before save..."
            );

            string editorRead =
                await RealWrite07D_ReadNotepadTextAsync(
                    expectedContent
                );

            Log(
                editorRead
            );

            if (RealWrite07D_IsFailure(
                    editorRead))
            {
                RealWrite07D_Fail(
                    "Notepad pre-save content verification"
                );

                return;
            }

            if (!editorRead.Contains(
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealWrite07D_Fail(
                    "Notepad pre-save text mismatch"
                );

                return;
            }

            Log(
                "PASS: Notepad editor content verified before save"
            );

            // =================================================
            // 6. SAVE REAL DOCUMENT
            //
            // CTRL+S was already independently proven earlier.
            // Since this file already exists, no Save As dialog
            // should be needed.
            // =================================================

            Log(
                "[6/12] Saving real Notepad document..."
            );

            string saveResult =
                WindowsInputTools.PressKey(
                    "CTRL+S"
                );

            Log(
                saveResult
            );

            if (RealWrite07D_IsFailure(
                    saveResult))
            {
                RealWrite07D_Fail(
                    "Notepad save command"
                );

                return;
            }

            bool saved =
                await RealWrite07D_WaitForFileContentAsync(
                    filePath,
                    expectedContent,
                    10
                );

            if (!saved)
            {
                RealWrite07D_Fail(
                    "Saved filesystem content did not update"
                );

                return;
            }

            Log(
                "PASS: Real Notepad document saved to disk"
            );

            // =================================================
            // 7. VERIFY EXACT FILE CONTENT
            // =================================================

            Log(
                "[7/12] Verifying exact filesystem content..."
            );

            string savedContent =
                File.ReadAllText(
                    filePath
                );

            Log(
                $"Saved content: {savedContent}"
            );

            if (!string.Equals(
                    savedContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealWrite07D_Fail(
                    "Exact filesystem content verification"
                );

                return;
            }

            Log(
                "PASS: Exact saved content verified"
            );

            // =================================================
            // 8. OPEN REAL FILE EXPLORER
            // =================================================

            Log(
                "[8/12] Opening saved file location in File Explorer..."
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
                RealWrite07D_Fail(
                    "File Explorer detection"
                );

                return;
            }

            string focusExplorer =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            folderName
                        )
                );

            Log(
                focusExplorer
            );

            if (RealWrite07D_IsFailure(
                    focusExplorer))
            {
                RealWrite07D_Fail(
                    "File Explorer focus"
                );

                return;
            }

            string verifyExplorer =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            folderName
                        )
                );

            Log(
                verifyExplorer
            );

            if (RealWrite07D_IsFailure(
                    verifyExplorer))
            {
                RealWrite07D_Fail(
                    "File Explorer foreground verification"
                );

                return;
            }

            Log(
                "PASS: Real File Explorer targeted"
            );

            // =================================================
            // 9. LOCATE SAVED FILE IN EXPLORER
            // =================================================

            Log(
                "[9/12] Locating saved file through Explorer UI Automation..."
            );

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
                RealWrite07D_Fail(
                    "Explorer saved-file discovery"
                );

                return;
            }

            Log(
                "PASS: Saved file located in real File Explorer"
            );

            // =================================================
            // 10. REVERIFY FILESYSTEM AFTER EXPLORER
            // =================================================

            Log(
                "[10/12] Re-verifying saved file after Explorer inspection..."
            );

            if (!File.Exists(
                    filePath))
            {
                RealWrite07D_Fail(
                    "Saved file disappeared"
                );

                return;
            }

            string explorerVerifiedContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    explorerVerifiedContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealWrite07D_Fail(
                    "Saved file content changed unexpectedly"
                );

                return;
            }

            Log(
                "PASS: Saved file remained correct"
            );

            // =================================================
            // 11. RETURN TO NOTEPAD
            // =================================================

            Log(
                "[11/12] Returning to real Notepad..."
            );

            string returnNotepad =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            fileName
                        )
                );

            Log(
                returnNotepad
            );

            if (RealWrite07D_IsFailure(
                    returnNotepad))
            {
                RealWrite07D_Fail(
                    "Return-to-Notepad focus"
                );

                return;
            }

            string returnForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            fileName
                        )
                );

            Log(
                returnForeground
            );

            if (RealWrite07D_IsFailure(
                    returnForeground))
            {
                RealWrite07D_Fail(
                    "Return-to-Notepad foreground verification"
                );

                return;
            }

            string finalEditorRead =
                await RealWrite07D_ReadNotepadTextAsync(
                    expectedContent
                );

            Log(
                finalEditorRead
            );

            if (
                RealWrite07D_IsFailure(
                    finalEditorRead)
                ||
                !finalEditorRead.Contains(
                    expectedContent,
                    StringComparison.Ordinal)
            )
            {
                RealWrite07D_Fail(
                    "Final Notepad content verification"
                );

                return;
            }

            Log(
                "PASS: Notepad still contains saved content"
            );

            // =================================================
            // 12. FINAL INDEPENDENT VERIFICATION
            // =================================================

            Log(
                "[12/12] Final independent verification..."
            );

            string finalFileContent =
                File.ReadAllText(
                    filePath
                );

            if (!string.Equals(
                    finalFileContent,
                    expectedContent,
                    StringComparison.Ordinal))
            {
                RealWrite07D_Fail(
                    "Final filesystem verification"
                );

                return;
            }

            string finalWindows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                finalWindows
            );

            if (
                RealWrite07D_IsFailure(
                    finalWindows)
                ||
                !finalWindows.Contains(
                    fileName,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !finalWindows.Contains(
                    folderName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                RealWrite07D_Fail(
                    "Final real-application verification"
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
                "SUCCESS: VERSION 0.7D REAL NOTEPAD WRITE/SAVE TEST PASSED."
            );

            Log(
                "Safe test-file creation: PASS"
            );

            Log(
                "Real Notepad targeting: PASS"
            );

            Log(
                "Real Notepad UI inspection: PASS"
            );

            Log(
                "Real Notepad content write: PASS"
            );

            Log(
                "Notepad pre-save content verification: PASS"
            );

            Log(
                "Real Notepad save: PASS"
            );

            Log(
                "Exact filesystem content verification: PASS"
            );

            Log(
                "Real File Explorer targeting: PASS"
            );

            Log(
                "Explorer saved-file discovery: PASS"
            );

            Log(
                "Cross-application file verification: PASS"
            );

            Log(
                "Return-to-Notepad workflow: PASS"
            );

            Log(
                "Final Notepad content verification: PASS"
            );

            Log(
                "Final filesystem verification: PASS"
            );

            Log(
                "VERSION 0.7D-1: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7D REAL WRITE TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // WRITE NOTEPAD TEXT
    //
    // Structured ValuePattern first.
    // Existing deterministic keyboard fallback second.
    // =========================================================

    private static async Task<string>
        RealWrite07D_WriteNotepadTextAsync(
            string windowTitle,
            string expectedContent)
    {
        (string Type, string Name, bool Exact)[] candidates =
        [
            (
                "edit",
                "Text editor",
                false
            ),
            (
                "document",
                "Text editor",
                false
            ),
            (
                "edit",
                "Text Editor",
                false
            ),
            (
                "document",
                "Text Editor",
                false
            ),
            (
                "edit",
                "",
                false
            ),
            (
                "document",
                "",
                false
            )
        ];

        string lastNativeResult =
            "";

        foreach (
            (string Type, string Name, bool Exact) candidate
            in candidates)
        {
            string setResult =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetControlValue(
                            "__FOREGROUND__",
                            candidate.Type,
                            candidate.Name,
                            candidate.Exact,
                            expectedContent
                        )
                );

            lastNativeResult =
                setResult;

            if (!RealWrite07D_IsFailure(
                    setResult))
            {
                string verify =
                    await RealWrite07D_ReadNotepadTextAsync(
                        expectedContent
                    );

                if (
                    !RealWrite07D_IsFailure(
                        verify)
                    &&
                    verify.Contains(
                        expectedContent,
                        StringComparison.Ordinal)
                )
                {
                    return
                        "SUCCESS: Notepad text written using native ValuePattern.\n" +
                        setResult;
                }
            }
        }

        // =====================================================
        // FALLBACK
        //
        // CTRL+A + existing TypeText pathway.
        // This fallback is allowed because it is independently
        // verified afterward through UI Automation and disk.
        // =====================================================

        string selectAll =
            WindowsInputTools.PressKey(
                "CTRL+A"
            );

        if (RealWrite07D_IsFailure(
                selectAll))
        {
            return
                "ERROR: Native ValuePattern failed and CTRL+A fallback failed.\n" +
                $"Last native result:\n{lastNativeResult}\n" +
                $"CTRL+A result:\n{selectAll}";
        }

        await Task.Delay(
            150
        );

        string typeResult =
            WindowsUiTools.TypeText(
                windowTitle,
                expectedContent
            );

        if (RealWrite07D_IsFailure(
                typeResult))
        {
            return
                "ERROR: Native ValuePattern and keyboard text fallback both failed.\n" +
                $"Last native result:\n{lastNativeResult}\n" +
                $"Type result:\n{typeResult}";
        }

        await Task.Delay(
            250
        );

        string verifyFallback =
            await RealWrite07D_ReadNotepadTextAsync(
                expectedContent
            );

        if (
            RealWrite07D_IsFailure(
                verifyFallback)
            ||
            !verifyFallback.Contains(
                expectedContent,
                StringComparison.Ordinal)
        )
        {
            return
                "ERROR: Keyboard fallback wrote text but UI verification failed.\n" +
                verifyFallback;
        }

        return
            "SUCCESS: Notepad text written using verified keyboard fallback.\n" +
            typeResult;
    }

    // =========================================================
    // READ NOTEPAD DOCUMENT TEXT
    // =========================================================

    private static async Task<string>
        RealWrite07D_ReadNotepadTextAsync(
            string expectedContent)
    {
        (string Type, string Name, bool Exact)[] candidates =
        [
            (
                "edit",
                "Text editor",
                false
            ),
            (
                "document",
                "Text editor",
                false
            ),
            (
                "edit",
                "Text Editor",
                false
            ),
            (
                "document",
                "Text Editor",
                false
            ),
            (
                "document",
                "",
                false
            ),
            (
                "edit",
                "",
                false
            )
        ];

        string lastResult =
            "";

        foreach (
            (string Type, string Name, bool Exact) candidate
            in candidates)
        {
            string result =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            candidate.Type,
                            candidate.Name,
                            candidate.Exact
                        )
                );

            lastResult =
                result;

            if (
                !RealWrite07D_IsFailure(
                    result)
                &&
                result.Contains(
                    expectedContent,
                    StringComparison.Ordinal)
            )
            {
                return
                    "SUCCESS: Notepad document text verified.\n" +
                    result;
            }
        }

        return
            "NOT_FOUND: Expected Notepad document text was not found through UI Automation.\n" +
            $"Expected: {expectedContent}\n" +
            $"Last result:\n{lastResult}";
    }

    // =========================================================
    // WAIT FOR SAVED CONTENT
    // =========================================================

    private static async Task<bool>
        RealWrite07D_WaitForFileContentAsync(
            string filePath,
            string expectedContent,
            int timeoutSeconds)
    {
        int safeTimeout =
            Math.Clamp(
                timeoutSeconds,
                1,
                60
            );

        DateTime deadline =
            DateTime.UtcNow.AddSeconds(
                safeTimeout
            );

        while (DateTime.UtcNow <
               deadline)
        {
            try
            {
                if (File.Exists(
                        filePath))
                {
                    string content =
                        File.ReadAllText(
                            filePath
                        );

                    if (string.Equals(
                            content,
                            expectedContent,
                            StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            await Task.Delay(
                150
            );
        }

        return false;
    }

    // =========================================================
    // ROBUST FILE EXPLORER DISCOVERY
    // =========================================================

    private static async Task<string>
        RealWrite07D_FindExplorerFileAsync(
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
            // FULL NAME
            // =================================================

            string fullName =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControlInfo(
                            "__FOREGROUND__",
                            fileName
                        )
                );

            if (!RealWrite07D_IsFailure(
                    fullName))
            {
                return
                    "SUCCESS: Explorer file found using full filename.\n" +
                    fullName;
            }

            lastResult =
                fullName;

            // =================================================
            // BASE NAME
            // =================================================

            string baseNameResult =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControlInfo(
                            "__FOREGROUND__",
                            baseName
                        )
                );

            if (!RealWrite07D_IsFailure(
                    baseNameResult))
            {
                return
                    "SUCCESS: Explorer file found using filename without extension.\n" +
                    baseNameResult;
            }

            lastResult =
                baseNameResult;

            // =================================================
            // LIST ITEM FULL
            // =================================================

            string listFull =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControl(
                            "__FOREGROUND__",
                            "listitem",
                            fileName,
                            false
                        )
                );

            if (!RealWrite07D_IsFailure(
                    listFull))
            {
                return
                    "SUCCESS: Explorer ListItem found using full filename.\n" +
                    listFull;
            }

            lastResult =
                listFull;

            // =================================================
            // LIST ITEM BASE
            // =================================================

            string listBase =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControl(
                            "__FOREGROUND__",
                            "listitem",
                            baseName,
                            false
                        )
                );

            if (!RealWrite07D_IsFailure(
                    listBase))
            {
                return
                    "SUCCESS: Explorer ListItem found using filename without extension.\n" +
                    listBase;
            }

            lastResult =
                listBase;

            // =================================================
            // FULL ENUMERATION FALLBACK
            // =================================================

            string controls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            500
                        )
                );

            if (!RealWrite07D_IsFailure(
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
                        "SUCCESS: Explorer file found through full UI Automation enumeration.\n" +
                        $"Matched: {(fullVisible ? fileName : baseName)}";
                }
            }

            lastResult =
                controls;

            await Task.Delay(
                300
            );
        }

        return
            "NOT_FOUND: Explorer could not locate the saved test file.\n" +
            $"Full name: {fileName}\n" +
            $"Base name: {baseName}\n" +
            $"Last result:\n{lastResult}";
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void RealWrite07D_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7D REAL NOTEPAD WRITE/SAVE TEST - {testName}"
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

    private static bool RealWrite07D_IsFailure(
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