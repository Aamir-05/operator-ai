using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow : Window
{
    private CancellationTokenSource?
        _agentCancellation;

    public MainWindow()
    {
        InitializeComponent();

        TaskBox.Text =
            "Open Wikipedia in the browser, search for OpenAI, read the page, and tell me what it is about.";

        Log(
            "Operator AI started."
        );

        Log(
            "Status: Ready."
        );
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

        if (string.IsNullOrWhiteSpace(
                task))
        {
            Log(
                "Please enter a task."
            );

            return;
        }

        if (_agentCancellation != null)
        {
            Log(
                "A task is already running."
            );

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

            Log(
                "--------------------------------"
            );

            Log(
                $"TASK: {task}"
            );

            Log(
                "Starting autonomous agent..."
            );

            OperatorAgent agent =
                new OperatorAgent();

            string result =
                await agent.RunAsync(
                    task,
                    message =>
                        Dispatcher.Invoke(
                            () =>
                                Log(
                                    message
                                )
                        ),
                    _agentCancellation.Token
                );

            Log(
                $"AI: {result}"
            );
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
            _agentCancellation?
                .Dispose();

            _agentCancellation =
                null;

            AskAIButton.IsEnabled =
                true;

            StopTaskButton.IsEnabled =
                false;

            Log(
                "Task finished."
            );
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
            Log(
                "Opening Notepad..."
            );

            string result =
                WindowsTools
                    .OpenApplication(
                        "notepad"
                    );

            Log(
                result
            );
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
            Log(
                "Creating test.txt..."
            );

            string result =
                WindowsTools
                    .CreateDesktopFile(
                        "test.txt",
                        "Hello Aamir"
                    );

            Log(
                result
            );
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
            Log(
                "Verifying test.txt..."
            );

            string result =
                WindowsTools
                    .DesktopFileExists(
                        "test.txt"
                    );

            Log(
                result
            );
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
            Log(
                "--------------------------------"
            );

            Log(
                "Starting Windows UI test..."
            );

            string openResult =
                WindowsTools
                    .OpenApplication(
                        "notepad"
                    );

            Log(
                openResult
            );

            await Task.Delay(
                1200
            );

            string windows =
                WindowsUiTools
                    .ListWindows();

            Log(
                windows
            );

            string focusResult =
                WindowsUiTools
                    .FocusWindow(
                        "Notepad"
                    );

            Log(
                focusResult
            );

            string typeResult =
                WindowsUiTools
                    .TypeText(
                        "Notepad",
                        "Operator AI can control Windows UI."
                    );

            Log(
                typeResult
            );

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
            Log(
                "--------------------------------"
            );

            Log(
                "Testing keyboard control..."
            );

            string openResult =
                WindowsTools
                    .OpenApplication(
                        "notepad"
                    );

            Log(
                openResult
            );

            await Task.Delay(
                1200
            );

            string focusResult =
                WindowsUiTools
                    .FocusWindow(
                        "Notepad"
                    );

            Log(
                focusResult
            );

            string typeResult =
                WindowsUiTools
                    .TypeText(
                        "Notepad",
                        "Keyboard automation test"
                    );

            Log(
                typeResult
            );

            await Task.Delay(
                500
            );

            string keyResult =
                WindowsInputTools
                    .PressKey(
                        "CTRL+S"
                    );

            Log(
                keyResult
            );

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
            Log(
                "--------------------------------"
            );

            Log(
                "Starting Save As automation test..."
            );

            string desktop =
                Environment.GetFolderPath(
                    Environment
                        .SpecialFolder
                        .DesktopDirectory
                );

            string targetPath =
                Path.Combine(
                    desktop,
                    "agent-test.txt"
                );

            Log(
                $"Target file: {targetPath}"
            );

            if (File.Exists(
                    targetPath))
            {
                try
                {
                    File.Delete(
                        targetPath
                    );

                    Log(
                        "Removed previous agent-test.txt."
                    );
                }
                catch (Exception deleteEx)
                {
                    Log(
                        "WARNING: Could not remove " +
                        $"existing test file: {deleteEx.Message}"
                    );
                }
            }

            string openResult =
                WindowsTools
                    .OpenApplication(
                        "notepad"
                    );

            Log(
                openResult
            );

            await Task.Delay(
                1500
            );

            string focusResult =
                WindowsUiTools
                    .FocusWindow(
                        "Notepad"
                    );

            Log(
                focusResult
            );

            await Task.Delay(
                300
            );

            string typeResult =
                WindowsUiTools
                    .TypeText(
                        "Notepad",
                        "Operator AI save dialog test"
                    );

            Log(
                typeResult
            );

            await Task.Delay(
                600
            );

            Log(
                "Opening Save As with CTRL+SHIFT+S..."
            );

            string saveAsResult =
                WindowsInputTools
                    .PressKey(
                        "CTRL+SHIFT+S"
                    );

            Log(
                saveAsResult
            );

            await Task.Delay(
                2000
            );

            Log(
                "Selecting existing filename..."
            );

            string selectResult =
                WindowsInputTools
                    .PressKey(
                        "CTRL+A"
                    );

            Log(
                selectResult
            );

            await Task.Delay(
                300
            );

            Clipboard.SetText(
                targetPath
            );

            await Task.Delay(
                200
            );

            Log(
                "Entering target file path..."
            );

            string pasteResult =
                WindowsInputTools
                    .PressKey(
                        "CTRL+V"
                    );

            Log(
                pasteResult
            );

            await Task.Delay(
                500
            );

            Log(
                "Pressing ENTER to save..."
            );

            string enterResult =
                WindowsInputTools
                    .PressKey(
                        "ENTER"
                    );

            Log(
                enterResult
            );

            await Task.Delay(
                2000
            );

            Log(
                "Verifying saved file..."
            );

            string verify =
                WindowsTools
                    .DesktopFileExists(
                        "agent-test.txt"
                    );

            Log(
                verify
            );

            if (File.Exists(
                    targetPath))
            {
                string readResult =
                    WindowsTools
                        .ReadDesktopFile(
                            "agent-test.txt"
                        );

                Log(
                    readResult
                );

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
    // BASIC BROWSER TEST
    // =========================================================

    private async void BrowserTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log(
                "--------------------------------"
            );

            Log(
                "Starting browser test..."
            );

            string startResult =
                await BrowserTools
                    .StartBrowserAsync();

            Log(
                startResult
            );

            if (IsBrowserFailure(
                    startResult))
            {
                return;
            }

            string navigateResult =
                await BrowserTools
                    .NavigateAsync(
                        "https://example.com"
                    );

            Log(
                navigateResult
            );

            if (IsBrowserFailure(
                    navigateResult))
            {
                return;
            }

            string pageInfo =
                await BrowserTools
                    .GetPageInfoAsync();

            Log(
                pageInfo
            );

            string pageText =
                await BrowserTools
                    .ReadPageTextAsync();

            Log(
                pageText
            );

            string links =
                await BrowserTools
                    .ListLinksAsync();

            Log(
                links
            );

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
    // BROWSER INTERACTION TEST
    // =========================================================

    private async void BrowserInteractionTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log(
                "--------------------------------"
            );

            Log(
                "Starting Version 0.6B browser interaction test..."
            );

            string startResult =
                await BrowserTools
                    .StartBrowserAsync();

            Log(
                startResult
            );

            if (IsBrowserFailure(
                    startResult))
            {
                return;
            }

            string navigateResult =
                await BrowserTools
                    .NavigateAsync(
                        "https://www.wikipedia.org"
                    );

            Log(
                navigateResult
            );

            if (IsBrowserFailure(
                    navigateResult))
            {
                return;
            }

            await Task.Delay(
                1000
            );

            string initialPageInfo =
                await BrowserTools
                    .GetPageInfoAsync();

            Log(
                initialPageInfo
            );

            string interactiveElements =
                await BrowserTools
                    .ListInteractiveElementsAsync();

            Log(
                interactiveElements
            );

            string findResult =
                await BrowserTools
                    .FindElementsAsync(
                        "css",
                        "input[name='search']"
                    );

            Log(
                findResult
            );

            string locatorType =
                "css";

            string locatorQuery =
                "input[name='search']";

            if (IsBrowserFailure(
                    findResult))
            {
                locatorType =
                    "placeholder";

                locatorQuery =
                    "Search Wikipedia";

                findResult =
                    await BrowserTools
                        .FindElementsAsync(
                            locatorType,
                            locatorQuery
                        );

                Log(
                    findResult
                );
            }

            if (IsBrowserFailure(
                    findResult))
            {
                Log(
                    "ERROR: Wikipedia search field could not be located."
                );

                return;
            }

            string fillResult =
                await BrowserTools
                    .FillAsync(
                        locatorType,
                        locatorQuery,
                        "OpenAI"
                    );

            Log(
                fillResult
            );

            if (IsBrowserFailure(
                    fillResult))
            {
                return;
            }

            await Task.Delay(
                500
            );

            string pressResult =
                await BrowserTools
                    .PressAsync(
                        locatorType,
                        locatorQuery,
                        "Enter"
                    );

            Log(
                pressResult
            );

            if (IsBrowserFailure(
                    pressResult))
            {
                return;
            }

            await Task.Delay(
                2500
            );

            string resultPageInfo =
                await BrowserTools
                    .GetPageInfoAsync();

            Log(
                resultPageInfo
            );

            string resultText =
                await BrowserTools
                    .ReadPageTextAsync();

            Log(
                resultText
            );

            string backResult =
                await BrowserTools
                    .BackAsync();

            Log(
                backResult
            );

            await Task.Delay(
                1000
            );

            string forwardResult =
                await BrowserTools
                    .ForwardAsync();

            Log(
                forwardResult
            );

            await Task.Delay(
                1000
            );

            string newTabResult =
                await BrowserTools
                    .NewTabAsync(
                        "https://example.com"
                    );

            Log(
                newTabResult
            );

            string tabs =
                await BrowserTools
                    .ListTabsAsync();

            Log(
                tabs
            );

            string switchResult =
                await BrowserTools
                    .SwitchTabAsync(
                        1
                    );

            Log(
                switchResult
            );

            string reloadResult =
                await BrowserTools
                    .ReloadAsync();

            Log(
                reloadResult
            );

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
    // VERSION 0.6D
    // BROWSER CONTROLS TEST
    // =========================================================

    private async void BrowserControlsTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        LocalBrowserTestServer server =
            new LocalBrowserTestServer();

        string uploadFileName =
            "operator-browser-upload-test.txt";

        string desktop =
            Environment.GetFolderPath(
                Environment
                    .SpecialFolder
                    .DesktopDirectory
            );

        string uploadFullPath =
            Path.Combine(
                desktop,
                uploadFileName
            );

        try
        {
            Log(
                "--------------------------------"
            );

            Log(
                "Starting Version 0.6D Browser Controls Test..."
            );

            // =================================================
            // STEP 1
            // START LOCAL TEST SERVER
            // =================================================

            Log(
                "Starting local controls test server..."
            );

            string serverResult =
                await server.StartAsync();

            Log(
                serverResult
            );

            if (IsBrowserFailure(
                    serverResult))
            {
                return;
            }

            // =================================================
            // STEP 2
            // CREATE UPLOAD TEST FILE
            // =================================================

            Log(
                "Creating Desktop upload test file..."
            );

            string createFileResult =
                WindowsTools
                    .CreateDesktopFile(
                        uploadFileName,
                        "Operator AI browser upload test file."
                    );

            Log(
                createFileResult
            );

            if (IsBrowserFailure(
                    createFileResult))
            {
                return;
            }

            // =================================================
            // STEP 3
            // START BROWSER
            // =================================================

            Log(
                "Starting Chromium..."
            );

            string startResult =
                await BrowserTools
                    .StartBrowserAsync();

            Log(
                startResult
            );

            if (IsBrowserFailure(
                    startResult))
            {
                return;
            }

            // =================================================
            // STEP 4
            // NAVIGATE TO LOCAL TEST PAGE
            // =================================================

            Log(
                $"Navigating to local test page: {server.BaseUrl}"
            );

            string navigateResult =
                await BrowserTools
                    .NavigateAsync(
                        server.BaseUrl
                    );

            Log(
                navigateResult
            );

            if (IsBrowserFailure(
                    navigateResult))
            {
                return;
            }

            // =================================================
            // STEP 5
            // WAIT FOR CHECKBOX
            // =================================================

            Log(
                "Waiting for automation checkbox..."
            );

            string waitResult =
                await BrowserTools
                    .WaitForElementAsync(
                        "css",
                        "#enableAutomation",
                        "visible",
                        10
                    );

            Log(
                waitResult
            );

            if (IsBrowserFailure(
                    waitResult))
            {
                return;
            }

            // =================================================
            // STEP 6
            // CHECK CHECKBOX
            // =================================================

            Log(
                "Checking Enable automation..."
            );

            string checkResult =
                await BrowserTools
                    .SetCheckedAsync(
                        "label",
                        "Enable automation",
                        true
                    );

            Log(
                checkResult
            );

            if (IsBrowserFailure(
                    checkResult))
            {
                return;
            }

            // =================================================
            // STEP 7
            // VERIFY CHECKBOX
            // =================================================

            Log(
                "Verifying checkbox state..."
            );

            string checkedState =
                await BrowserTools
                    .GetCheckedStateAsync(
                        "label",
                        "Enable automation"
                    );

            Log(
                checkedState
            );

            if (
                IsBrowserFailure(
                    checkedState
                )
                ||
                !checkedState.Contains(
                    "checked=True",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                Log(
                    "ERROR: Checkbox verification failed."
                );

                return;
            }

            // =================================================
            // STEP 8
            // SELECT OPERATIONS DEPARTMENT
            // =================================================

            Log(
                "Selecting Operations department..."
            );

            string selectResult =
                await BrowserTools
                    .SelectOptionAsync(
                        "label",
                        "Department",
                        "label",
                        "Operations"
                    );

            Log(
                selectResult
            );

            if (IsBrowserFailure(
                    selectResult))
            {
                return;
            }

            // =================================================
            // STEP 9
            // UPLOAD DESKTOP FILE
            // =================================================

            Log(
                $"Uploading Desktop\\{uploadFileName}..."
            );

            string uploadResult =
                await BrowserTools
                    .UploadDesktopFileAsync(
                        "label",
                        "Upload file",
                        uploadFileName
                    );

            Log(
                uploadResult
            );

            if (IsBrowserFailure(
                    uploadResult))
            {
                return;
            }

            await Task.Delay(
                300
            );

            // =================================================
            // STEP 10
            // VERIFY PAGE STATE
            // =================================================

            Log(
                "Reading page to verify form state..."
            );

            string pageText =
                await BrowserTools
                    .ReadPageTextAsync();

            Log(
                pageText
            );

            if (!pageText.Contains(
                    "Checkbox: enabled",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "ERROR: Page did not confirm checkbox state."
                );

                return;
            }

            if (!pageText.Contains(
                    "Department: Operations",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "ERROR: Page did not confirm dropdown selection."
                );

                return;
            }

            if (!pageText.Contains(
                    $"Uploaded: {uploadFileName}",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "ERROR: Page did not confirm uploaded filename."
                );

                return;
            }

            Log(
                "SUCCESS: Checkbox, dropdown and upload states verified."
            );

            // =================================================
            // STEP 11
            // REMOVE OLD DOWNLOAD IF PRESENT
            // =================================================

            string downloadsDirectory =
                BrowserTools
                    .GetDownloadsDirectory();

            string expectedDownload =
                Path.Combine(
                    downloadsDirectory,
                    "test-report.txt"
                );

            if (File.Exists(
                    expectedDownload))
            {
                try
                {
                    File.Delete(
                        expectedDownload
                    );

                    Log(
                        "Removed previous test-report.txt."
                    );
                }
                catch (Exception deleteEx)
                {
                    Log(
                        $"WARNING: Could not remove previous download: {deleteEx.Message}"
                    );
                }
            }

            // =================================================
            // STEP 12
            // DOWNLOAD TEST REPORT
            // =================================================

            Log(
                "Downloading Test Report..."
            );

            string downloadResult =
                await BrowserTools
                    .DownloadByClickAsync(
                        "css",
                        "#downloadReport",
                        "test-report.txt"
                    );

            Log(
                downloadResult
            );

            if (IsBrowserFailure(
                    downloadResult))
            {
                return;
            }

            // =================================================
            // STEP 13
            // VERIFY DOWNLOAD
            // =================================================

            Log(
                "Listing Operator AI downloads..."
            );

            string downloads =
                BrowserTools
                    .ListDownloads();

            Log(
                downloads
            );

            if (!File.Exists(
                    expectedDownload))
            {
                Log(
                    $"ERROR: Download verification failed: {expectedDownload}"
                );

                return;
            }

            string downloadedText =
                await File.ReadAllTextAsync(
                    expectedDownload
                );

            Log(
                "Downloaded file contents:"
            );

            Log(
                downloadedText
            );

            if (!downloadedText.Contains(
                    "Browser download system is working",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "ERROR: Downloaded report contents were not correct."
                );

                return;
            }

            // =================================================
            // COMPLETE
            // =================================================

            Log(
                "================================"
            );

            Log(
                "SUCCESS: VERSION 0.6D BROWSER CONTROLS TEST PASSED."
            );

            Log(
                "Checkbox: PASS"
            );

            Log(
                "Dropdown: PASS"
            );

            Log(
                "Upload: PASS"
            );

            Log(
                "Wait: PASS"
            );

            Log(
                "Download: PASS"
            );

            Log(
                $"Downloaded file: {expectedDownload}"
            );

            Log(
                "================================"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"BROWSER CONTROLS TEST ERROR: {ex.Message}"
            );
        }
        finally
        {
            try
            {
                await server.StopAsync();
            }
            catch
            {
            }

            // Remove only our temporary upload test file.
            try
            {
                if (File.Exists(
                        uploadFullPath))
                {
                    File.Delete(
                        uploadFullPath
                    );
                }
            }
            catch
            {
            }
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