using System;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TaskBox.Text =
            "Open Notepad, type exactly: Monthly operations report, save it to my Desktop as operations-report.txt, verify that the file exists, and read it back.";

        Log("Operator AI started.");
        Log("Status: Ready.");
    }

    // =========================================================
    // AI AGENT
    // =========================================================

    private async void AskAI_Click(
        object sender,
        RoutedEventArgs e)
    {
        string task =
            TaskBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(task))
        {
            Log("Please enter a task.");
            return;
        }

        try
        {
            AskAIButton.IsEnabled = false;

            Log("--------------------------------");
            Log($"TASK: {task}");
            Log("Starting autonomous agent...");

            OperatorAgent agent =
                new OperatorAgent();

            string result =
                await agent.RunAsync(
                    task,
                    message =>
                        Dispatcher.Invoke(
                            () => Log(message)
                        )
                );

            Log($"AI: {result}");
            Log("Task finished.");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
        finally
        {
            AskAIButton.IsEnabled = true;
        }
    }

    // =========================================================
    // BASIC WINDOWS TESTS
    // =========================================================

    private void OpenNotepad_Click(
        object sender,
        RoutedEventArgs e)
    {
        Log("Opening Notepad...");

        string result =
            WindowsTools.OpenApplication(
                "notepad"
            );

        Log(result);
    }

    private void CreateFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        Log("Creating test.txt...");

        string result =
            WindowsTools.CreateDesktopFile(
                "test.txt",
                "Hello Aamir"
            );

        Log(result);
    }

    private void VerifyFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        Log("Verifying test.txt...");

        string result =
            WindowsTools.DesktopFileExists(
                "test.txt"
            );

        Log(result);
    }

    // =========================================================
    // WINDOWS UI TYPE TEST
    // =========================================================

    private async void UiTypeTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log("Starting Windows UI test...");

            string openResult =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(openResult);

            await Task.Delay(1200);

            string windows =
                WindowsUiTools.ListWindows();

            Log(windows);

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(focusResult);

            string typeResult =
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI can control Windows UI."
                );

            Log(typeResult);

            Log("Windows UI test finished.");
        }
        catch (Exception ex)
        {
            Log(
                $"UI TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // CTRL+S TEST
    // =========================================================

    private async void SaveKeyTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log("Testing keyboard control...");

            string openResult =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(openResult);

            await Task.Delay(1200);

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(focusResult);

            string typeResult =
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Keyboard automation test"
                );

            Log(typeResult);

            await Task.Delay(500);

            string keyResult =
                WindowsInputTools.PressKey(
                    "CTRL+S"
                );

            Log(keyResult);

            Log("Keyboard test finished.");
        }
        catch (Exception ex)
        {
            Log(
                $"KEYBOARD TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // VERSION 0.5D
    // SAVE AS AUTOMATION TEST
    // =========================================================

    private async void SaveDialogTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log("Starting Save As automation test...");

            // -------------------------------------------------
            // STEP 1
            // Build target path
            // -------------------------------------------------

            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory
                );

            string targetPath =
                System.IO.Path.Combine(
                    desktop,
                    "agent-test.txt"
                );

            Log(
                $"Target file: {targetPath}"
            );

            // Remove previous test file
            // so verification is genuine.
            if (System.IO.File.Exists(targetPath))
            {
                try
                {
                    System.IO.File.Delete(
                        targetPath
                    );

                    Log(
                        "Removed previous agent-test.txt."
                    );
                }
                catch (Exception deleteEx)
                {
                    Log(
                        $"WARNING: Could not delete previous test file: {deleteEx.Message}"
                    );
                }
            }

            // -------------------------------------------------
            // STEP 2
            // Open Notepad
            // -------------------------------------------------

            Log("Opening Notepad...");

            string openResult =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(openResult);

            await Task.Delay(1500);

            // -------------------------------------------------
            // STEP 3
            // Focus Notepad
            // -------------------------------------------------

            Log("Focusing Notepad...");

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(focusResult);

            await Task.Delay(300);

            // -------------------------------------------------
            // STEP 4
            // Type content
            // -------------------------------------------------

            Log("Typing document content...");

            string typeResult =
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI save dialog test"
                );

            Log(typeResult);

            await Task.Delay(600);

            // -------------------------------------------------
            // STEP 5
            // Open Save As directly
            // -------------------------------------------------

            Log(
                "Opening Save As directly with CTRL+SHIFT+S..."
            );

            string saveAsResult =
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                );

            Log(saveAsResult);

            await Task.Delay(2000);

            // -------------------------------------------------
            // STEP 6
            // Inspect the foreground window
            // -------------------------------------------------

            Log(
                "Inspecting foreground after Save As..."
            );

            string foreground =
                WindowsUiTools.InspectWindow(
                    "__FOREGROUND__"
                );

            Log(foreground);

            // -------------------------------------------------
            // STEP 7
            // Select existing filename
            // -------------------------------------------------

            Log(
                "Selecting existing filename..."
            );

            string selectAllResult =
                WindowsInputTools.PressKey(
                    "CTRL+A"
                );

            Log(selectAllResult);

            await Task.Delay(300);

            // -------------------------------------------------
            // STEP 8
            // Copy full target path to clipboard
            // -------------------------------------------------

            Log(
                $"Putting target path on clipboard: {targetPath}"
            );

            System.Windows.Clipboard.SetText(
                targetPath
            );

            await Task.Delay(200);

            // -------------------------------------------------
            // STEP 9
            // Paste target filename/path
            // -------------------------------------------------

            Log(
                "Entering target file path..."
            );

            string pasteResult =
                WindowsInputTools.PressKey(
                    "CTRL+V"
                );

            Log(pasteResult);

            await Task.Delay(500);

            // -------------------------------------------------
            // STEP 10
            // Save
            // -------------------------------------------------

            Log("Pressing ENTER to save...");

            string enterResult =
                WindowsInputTools.PressKey(
                    "ENTER"
                );

            Log(enterResult);

            await Task.Delay(2000);

            // -------------------------------------------------
            // STEP 11
            // Check whether file exists
            // -------------------------------------------------

            bool fileExists =
                System.IO.File.Exists(
                    targetPath
                );

            if (!fileExists)
            {
                Log(
                    "File not found yet. Checking foreground for a confirmation dialog..."
                );

                string possibleDialog =
                    WindowsUiTools.InspectWindow(
                        "__FOREGROUND__"
                    );

                Log(possibleDialog);

                // Possible overwrite/confirmation dialog.
                string confirmResult =
                    WindowsInputTools.PressKey(
                        "ENTER"
                    );

                Log(confirmResult);

                await Task.Delay(1500);

                fileExists =
                    System.IO.File.Exists(
                        targetPath
                    );
            }

            // -------------------------------------------------
            // STEP 12
            // Verify using our own Windows tool
            // -------------------------------------------------

            Log("Verifying saved file...");

            string verify =
                WindowsTools.DesktopFileExists(
                    "agent-test.txt"
                );

            Log(verify);

            // -------------------------------------------------
            // STEP 13
            // Read file back
            // -------------------------------------------------

            if (fileExists)
            {
                Log("Reading saved file...");

                string readResult =
                    WindowsTools.ReadDesktopFile(
                        "agent-test.txt"
                    );

                Log(readResult);

                Log(
                    "SUCCESS: Save As automation completed."
                );
            }
            else
            {
                Log(
                    "ERROR: agent-test.txt was not created."
                );

                Log(
                    "Final foreground window:"
                );

                string finalForeground =
                    WindowsUiTools.InspectWindow(
                        "__FOREGROUND__"
                    );

                Log(finalForeground);
            }

            Log(
                "Save As automation test finished."
            );
        }
        catch (Exception ex)
        {
            Log(
                $"SAVE DIALOG TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // LOGGING
    // =========================================================

    private void Log(
        string message)
    {
        LogBox.AppendText(
            $"[{DateTime.Now:HH:mm:ss}] {message}\n"
        );

        LogBox.ScrollToEnd();
    }
}