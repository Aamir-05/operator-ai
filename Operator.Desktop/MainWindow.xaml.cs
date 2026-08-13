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

        // Prevent more than one agent task running at once.
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
                catch (Exception ex)
                {
                    Log(
                        $"WARNING: Could not remove existing test file: {ex.Message}"
                    );
                }
            }

            string openResult =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(openResult);

            await Task.Delay(1500);

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(focusResult);

            await Task.Delay(300);

            string typeResult =
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI save dialog test"
                );

            Log(typeResult);

            await Task.Delay(600);

            Log(
                "Opening Save As with CTRL+SHIFT+S..."
            );

            string saveAsResult =
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                );

            Log(saveAsResult);

            await Task.Delay(2000);

            Log(
                "Selecting existing filename..."
            );

            string selectResult =
                WindowsInputTools.PressKey(
                    "CTRL+A"
                );

            Log(selectResult);

            await Task.Delay(300);

            System.Windows.Clipboard.SetText(
                targetPath
            );

            await Task.Delay(200);

            Log(
                "Entering target file path..."
            );

            string pasteResult =
                WindowsInputTools.PressKey(
                    "CTRL+V"
                );

            Log(pasteResult);

            await Task.Delay(500);

            Log(
                "Pressing ENTER to save..."
            );

            string enterResult =
                WindowsInputTools.PressKey(
                    "ENTER"
                );

            Log(enterResult);

            await Task.Delay(2000);

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