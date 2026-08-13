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
    // WINDOWS UI TEST
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
    // VERSION 0.6A
    // BASIC BROWSER TEST
    // =========================================================

    private async void BrowserTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log("Starting browser test...");

            string startResult =
                await BrowserTools.StartBrowserAsync();

            Log(startResult);

            if (IsBrowserFailure(startResult))
            {
                return;
            }

            string navigateResult =
                await BrowserTools.NavigateAsync(
                    "https://example.com"
                );

            Log(navigateResult);

            if (IsBrowserFailure(navigateResult))
            {
                return;
            }

            string pageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(pageInfo);

            string pageText =
                await BrowserTools.ReadPageTextAsync();

            Log(pageText);

            string links =
                await BrowserTools.ListLinksAsync();

            Log(links);

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
    // VERSION 0.6B
    // INTERACTIVE BROWSER TEST
    // =========================================================

    private async void BrowserInteractionTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log("--------------------------------");
            Log(
                "Starting Version 0.6B browser interaction test..."
            );

            // -------------------------------------------------
            // STEP 1
            // Start Chromium
            // -------------------------------------------------

            Log("Starting Chromium...");

            string startResult =
                await BrowserTools.StartBrowserAsync();

            Log(startResult);

            if (IsBrowserFailure(startResult))
            {
                Log(
                    "0.6B test stopped: browser could not start."
                );

                return;
            }

            // -------------------------------------------------
            // STEP 2
            // Navigate to Wikipedia
            // -------------------------------------------------

            Log(
                "Navigating to Wikipedia..."
            );

            string navigateResult =
                await BrowserTools.NavigateAsync(
                    "https://www.wikipedia.org"
                );

            Log(navigateResult);

            if (IsBrowserFailure(navigateResult))
            {
                Log(
                    "0.6B test stopped: navigation failed."
                );

                return;
            }

            await Task.Delay(1000);

            // -------------------------------------------------
            // STEP 3
            // Read page
            // -------------------------------------------------

            Log(
                "Reading Wikipedia page information..."
            );

            string initialPageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(initialPageInfo);

            // -------------------------------------------------
            // STEP 4
            // Inspect interactive elements
            // -------------------------------------------------

            Log(
                "Inspecting interactive browser elements..."
            );

            string interactiveElements =
                await BrowserTools
                    .ListInteractiveElementsAsync();

            Log(interactiveElements);

            // -------------------------------------------------
            // STEP 5
            // Find Wikipedia search field
            // -------------------------------------------------

            Log(
                "Finding Wikipedia search field..."
            );

            string findResult =
                await BrowserTools.FindElementsAsync(
                    "css",
                    "input[name='search']"
                );

            Log(findResult);

            string locatorType =
                "css";

            string locatorQuery =
                "input[name='search']";

            // -------------------------------------------------
            // Fallback locator
            // -------------------------------------------------

            if (IsBrowserFailure(findResult))
            {
                Log(
                    "Primary search locator failed. Trying placeholder locator..."
                );

                locatorType =
                    "placeholder";

                locatorQuery =
                    "Search Wikipedia";

                findResult =
                    await BrowserTools.FindElementsAsync(
                        locatorType,
                        locatorQuery
                    );

                Log(findResult);
            }

            if (IsBrowserFailure(findResult))
            {
                Log(
                    "ERROR: Wikipedia search field could not be located."
                );

                return;
            }

            // -------------------------------------------------
            // STEP 6
            // Fill search field
            // -------------------------------------------------

            Log(
                "Filling search field with 'OpenAI'..."
            );

            string fillResult =
                await BrowserTools.FillAsync(
                    locatorType,
                    locatorQuery,
                    "OpenAI"
                );

            Log(fillResult);

            if (IsBrowserFailure(fillResult))
            {
                return;
            }

            await Task.Delay(500);

            // -------------------------------------------------
            // STEP 7
            // Press Enter
            // -------------------------------------------------

            Log(
                "Pressing ENTER in search field..."
            );

            string pressResult =
                await BrowserTools.PressAsync(
                    locatorType,
                    locatorQuery,
                    "Enter"
                );

            Log(pressResult);

            if (IsBrowserFailure(pressResult))
            {
                return;
            }

            await Task.Delay(2500);

            // -------------------------------------------------
            // STEP 8
            // Read results page information
            // -------------------------------------------------

            Log(
                "Reading page after search..."
            );

            string resultPageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(resultPageInfo);

            // -------------------------------------------------
            // STEP 9
            // Read visible results text
            // -------------------------------------------------

            string resultText =
                await BrowserTools.ReadPageTextAsync();

            Log(resultText);

            // -------------------------------------------------
            // STEP 10
            // Test Back
            // -------------------------------------------------

            Log(
                "Testing browser Back..."
            );

            string backResult =
                await BrowserTools.BackAsync();

            Log(backResult);

            await Task.Delay(1500);

            // -------------------------------------------------
            // STEP 11
            // Test Forward
            // -------------------------------------------------

            Log(
                "Testing browser Forward..."
            );

            string forwardResult =
                await BrowserTools.ForwardAsync();

            Log(forwardResult);

            await Task.Delay(1500);

            // -------------------------------------------------
            // STEP 12
            // Open second tab
            // -------------------------------------------------

            Log(
                "Opening a second browser tab..."
            );

            string newTabResult =
                await BrowserTools.NewTabAsync(
                    "https://example.com"
                );

            Log(newTabResult);

            await Task.Delay(1200);

            // -------------------------------------------------
            // STEP 13
            // List tabs
            // -------------------------------------------------

            Log(
                "Listing browser tabs..."
            );

            string tabs =
                await BrowserTools.ListTabsAsync();

            Log(tabs);

            // -------------------------------------------------
            // STEP 14
            // Switch back to first tab
            // -------------------------------------------------

            Log(
                "Switching to browser tab 1..."
            );

            string switchResult =
                await BrowserTools.SwitchTabAsync(
                    1
                );

            Log(switchResult);

            await Task.Delay(700);

            // -------------------------------------------------
            // STEP 15
            // List tabs again
            // -------------------------------------------------

            string tabsAfterSwitch =
                await BrowserTools.ListTabsAsync();

            Log(tabsAfterSwitch);

            // -------------------------------------------------
            // STEP 16
            // Test reload
            // -------------------------------------------------

            Log(
                "Testing browser Reload..."
            );

            string reloadResult =
                await BrowserTools.ReloadAsync();

            Log(reloadResult);

            // -------------------------------------------------
            // COMPLETE
            // -------------------------------------------------

            Log(
                "SUCCESS: Version 0.6B browser interaction test completed."
            );
        }
        catch (Exception ex)
        {
            Log(
                $"BROWSER INTERACTION TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // HELPER
    // =========================================================

    private static bool IsBrowserFailure(
        string result)
    {
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
                StringComparison.OrdinalIgnoreCase);
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