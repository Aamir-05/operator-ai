using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _agentCancellation;

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

        if (_agentCancellation != null)
        {
            Log("A task is already running.");
            return;
        }

        try
        {
            _agentCancellation =
                new CancellationTokenSource();

            AskAIButton.IsEnabled =
                false;

            StopTaskButton.IsEnabled =
                true;

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
                        ),
                    _agentCancellation.Token
                );

            Log($"AI: {result}");
        }
        catch (OperationCanceledException)
        {
            Log(
                "[CANCELLED] Task stopped."
            );
        }
        catch (Exception ex)
        {
            Log(
                $"ERROR: {ex.Message}"
            );
        }
        finally
        {
            _agentCancellation?.Dispose();

            _agentCancellation =
                null;

            AskAIButton.IsEnabled =
                true;

            StopTaskButton.IsEnabled =
                false;

            Log("Task finished.");
        }
    }

    // =========================================================
    // STOP TASK
    // =========================================================

    private void StopTask_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_agentCancellation == null)
        {
            Log(
                "No AI task is currently running."
            );

            return;
        }

        Log(
            "Stop requested..."
        );

        StopTaskButton.IsEnabled =
            false;

        _agentCancellation.Cancel();
    }

    // =========================================================
    // BASIC WINDOWS TESTS
    // =========================================================

    private void OpenNotepad_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("Opening Notepad...");

            string result =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(result);
        }
        catch (Exception ex)
        {
            Log(
                $"OPEN NOTEPAD ERROR: {ex.Message}"
            );
        }
    }

    private void CreateFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("Creating test.txt...");

            string result =
                WindowsTools.CreateDesktopFile(
                    "test.txt",
                    "Hello Aamir"
                );

            Log(result);
        }
        catch (Exception ex)
        {
            Log(
                $"CREATE FILE ERROR: {ex.Message}"
            );
        }
    }

    private void VerifyFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("Verifying test.txt...");

            string result =
                WindowsTools.DesktopFileExists(
                    "test.txt"
                );

            Log(result);
        }
        catch (Exception ex)
        {
            Log(
                $"VERIFY FILE ERROR: {ex.Message}"
            );
        }
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

            Log(
                "Windows UI test finished."
            );
        }
        catch (Exception ex)
        {
            Log(
                $"UI TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // KEYBOARD TEST
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

            Log(
                "Keyboard test finished."
            );
        }
        catch (Exception ex)
        {
            Log(
                $"KEYBOARD TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // SAVE DIALOG TEST
    // =========================================================

    private async void SaveDialogTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log(
                "Starting Save As automation test..."
            );

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

            if (System.IO.File.Exists(
                    targetPath))
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
                        $"WARNING: Could not remove existing test file: {deleteEx.Message}"
                    );
                }
            }

            // -------------------------------------------------
            // Open Notepad
            // -------------------------------------------------

            string openResult =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(openResult);

            await Task.Delay(1500);

            // -------------------------------------------------
            // Focus Notepad
            // -------------------------------------------------

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(focusResult);

            await Task.Delay(300);

            // -------------------------------------------------
            // Type content
            // -------------------------------------------------

            string typeResult =
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI save dialog test"
                );

            Log(typeResult);

            await Task.Delay(600);

            // -------------------------------------------------
            // Open Save As
            // -------------------------------------------------

            Log(
                "Opening Save As with CTRL+SHIFT+S..."
            );

            string saveAsResult =
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                );

            Log(saveAsResult);

            await Task.Delay(2000);

            // -------------------------------------------------
            // Select current filename
            // -------------------------------------------------

            Log(
                "Selecting existing filename..."
            );

            string selectResult =
                WindowsInputTools.PressKey(
                    "CTRL+A"
                );

            Log(selectResult);

            await Task.Delay(300);

            // -------------------------------------------------
            // Copy target path
            // -------------------------------------------------

            System.Windows.Clipboard.SetText(
                targetPath
            );

            await Task.Delay(200);

            // -------------------------------------------------
            // Paste target path
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
            // Save
            // -------------------------------------------------

            Log(
                "Pressing ENTER to save..."
            );

            string enterResult =
                WindowsInputTools.PressKey(
                    "ENTER"
                );

            Log(enterResult);

            await Task.Delay(2000);

            // -------------------------------------------------
            // Verify
            // -------------------------------------------------

            Log(
                "Verifying saved file..."
            );

            string verify =
                WindowsTools.DesktopFileExists(
                    "agent-test.txt"
                );

            Log(verify);

            if (System.IO.File.Exists(
                    targetPath))
            {
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
    // VERSION 0.6A
    // BROWSER TEST
    // =========================================================

    private async void BrowserTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log("Starting browser test...");

            // -------------------------------------------------
            // STEP 1
            // Start Chromium
            // -------------------------------------------------

            Log("Starting Chromium...");

            string startResult =
                await BrowserTools.StartBrowserAsync();

            Log(startResult);

            if (startResult.StartsWith(
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "Browser test stopped because Chromium could not start."
                );

                return;
            }

            // -------------------------------------------------
            // STEP 2
            // Navigate to example.com
            // -------------------------------------------------

            Log(
                "Navigating to https://example.com..."
            );

            string navigateResult =
                await BrowserTools.NavigateAsync(
                    "https://example.com"
                );

            Log(navigateResult);

            if (navigateResult.StartsWith(
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "Browser test stopped because navigation failed."
                );

                return;
            }

            // -------------------------------------------------
            // STEP 3
            // Read page title and URL
            // -------------------------------------------------

            Log(
                "Reading page information..."
            );

            string pageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(pageInfo);

            // -------------------------------------------------
            // STEP 4
            // Read visible page text
            // -------------------------------------------------

            Log(
                "Reading visible page text..."
            );

            string pageText =
                await BrowserTools.ReadPageTextAsync();

            Log(pageText);

            // -------------------------------------------------
            // STEP 5
            // List links
            // -------------------------------------------------

            Log(
                "Listing page links..."
            );

            string links =
                await BrowserTools.ListLinksAsync();

            Log(links);

            // -------------------------------------------------
            // COMPLETE
            // -------------------------------------------------

            Log(
                "SUCCESS: Browser test completed."
            );
        }
        catch (Exception ex)
        {
            Log(
                $"BROWSER TEST ERROR: {ex.Message}"
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