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
            "Open Wikipedia, search for OpenAI, read the page, and tell me what it is about.";

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
                WindowsTools.OpenApplication(
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
                WindowsTools.CreateDesktopFile(
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
                WindowsTools.DesktopFileExists(
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
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(
                openResult
            );

            await Task.Delay(
                1200
            );

            string windows =
                WindowsUiTools.ListWindows();

            Log(
                windows
            );

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(
                focusResult
            );

            string typeResult =
                WindowsUiTools.TypeText(
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
    // CTRL+S TEST
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
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(
                openResult
            );

            await Task.Delay(
                1200
            );

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(
                focusResult
            );

            string typeResult =
                WindowsUiTools.TypeText(
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
                WindowsInputTools.PressKey(
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
                    Environment.SpecialFolder.DesktopDirectory
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
                }
                catch
                {
                }
            }

            string openResult =
                WindowsTools.OpenApplication(
                    "notepad"
                );

            Log(
                openResult
            );

            await Task.Delay(
                1500
            );

            string focusResult =
                WindowsUiTools.FocusWindow(
                    "Notepad"
                );

            Log(
                focusResult
            );

            await Task.Delay(
                300
            );

            string typeResult =
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI save dialog test"
                );

            Log(
                typeResult
            );

            await Task.Delay(
                600
            );

            string saveAsResult =
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                );

            Log(
                saveAsResult
            );

            await Task.Delay(
                2000
            );

            WindowsInputTools.PressKey(
                "CTRL+A"
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

            WindowsInputTools.PressKey(
                "CTRL+V"
            );

            await Task.Delay(
                400
            );

            WindowsInputTools.PressKey(
                "ENTER"
            );

            await Task.Delay(
                2000
            );

            string verify =
                WindowsTools.DesktopFileExists(
                    "agent-test.txt"
                );

            Log(
                verify
            );

            if (File.Exists(
                    targetPath))
            {
                Log(
                    WindowsTools.ReadDesktopFile(
                        "agent-test.txt"
                    )
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
                await BrowserTools.StartBrowserAsync();

            Log(
                startResult
            );

            if (IsBrowserFailure(
                    startResult))
            {
                return;
            }

            string navigateResult =
                await BrowserTools.NavigateAsync(
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

            Log(
                await BrowserTools.GetPageInfoAsync()
            );

            Log(
                await BrowserTools.ReadPageTextAsync()
            );

            Log(
                await BrowserTools.ListLinksAsync()
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
    // 0.6B BROWSER INTERACTION TEST
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
                await BrowserTools.StartBrowserAsync();

            Log(
                startResult
            );

            if (IsBrowserFailure(
                    startResult))
            {
                return;
            }

            string navigateResult =
                await BrowserTools.NavigateAsync(
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

            string findResult =
                await BrowserTools.FindElementsAsync(
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
                    await BrowserTools.FindElementsAsync(
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
                return;
            }

            Log(
                await BrowserTools.FillAsync(
                    locatorType,
                    locatorQuery,
                    "OpenAI"
                )
            );

            Log(
                await BrowserTools.PressAsync(
                    locatorType,
                    locatorQuery,
                    "Enter"
                )
            );

            await Task.Delay(
                2200
            );

            Log(
                await BrowserTools.GetPageInfoAsync()
            );

            Log(
                await BrowserTools.ReadPageTextAsync()
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
    // 0.6D CONTROLS TEST
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
                Environment.SpecialFolder.DesktopDirectory
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

            string createFileResult =
                WindowsTools.CreateDesktopFile(
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

            string startResult =
                await BrowserTools.StartBrowserAsync();

            Log(
                startResult
            );

            if (IsBrowserFailure(
                    startResult))
            {
                return;
            }

            string navigateResult =
                await BrowserTools.NavigateAsync(
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

            string waitResult =
                await BrowserTools.WaitForElementAsync(
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

            string checkResult =
                await BrowserTools.SetCheckedAsync(
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

            string checkedState =
                await BrowserTools.GetCheckedStateAsync(
                    "label",
                    "Enable automation"
                );

            Log(
                checkedState
            );

            string selectResult =
                await BrowserTools.SelectOptionAsync(
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

            string uploadResult =
                await BrowserTools.UploadDesktopFileAsync(
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

            string pageText =
                await BrowserTools.ReadPageTextAsync();

            Log(
                pageText
            );

            if (!pageText.Contains(
                    "Checkbox: enabled",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !pageText.Contains(
                    "Department: Operations",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !pageText.Contains(
                    $"Uploaded: {uploadFileName}",
                    StringComparison.OrdinalIgnoreCase))
            {
                Log(
                    "ERROR: Form verification failed."
                );

                return;
            }

            string expectedDownload =
                Path.Combine(
                    BrowserTools.GetDownloadsDirectory(),
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
                }
                catch
                {
                }
            }

            string downloadResult =
                await BrowserTools.DownloadByClickAsync(
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

            Log(
                BrowserTools.ListDownloads()
            );

            if (!File.Exists(
                    expectedDownload))
            {
                Log(
                    "ERROR: Download verification failed."
                );

                return;
            }

            Log(
                "SUCCESS: VERSION 0.6D BROWSER CONTROLS TEST PASSED."
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
    // VERSION 0.6E
    // RELIABILITY TEST
    // =========================================================

    private async void BrowserReliabilityTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        LocalBrowserTestServer server =
            new LocalBrowserTestServer();

        try
        {
            Log(
                "--------------------------------"
            );

            Log(
                "Starting Version 0.6E Reliability Test..."
            );

            // =================================================
            // 1. START LOCAL SERVER
            // =================================================

            Log(
                "Starting deterministic local test page..."
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
            // 2. START BROWSER
            // =================================================

            Log(
                "Starting persistent Chromium..."
            );

            string browserResult =
                await BrowserTools.StartBrowserAsync();

            Log(
                browserResult
            );

            if (IsBrowserFailure(
                    browserResult))
            {
                return;
            }

            // =================================================
            // 3. NAVIGATE
            // =================================================

            Log(
                $"Navigating to {server.BaseUrl}..."
            );

            string navigateResult =
                await BrowserTools.NavigateAsync(
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
            // 4. ROLE FIND
            // =================================================

            Log(
                "TEST: Role-based textbox discovery..."
            );

            string roleFind =
                await BrowserTools.FindByRoleAsync(
                    "textbox",
                    "Test input",
                    true
                );

            Log(
                roleFind
            );

            if (IsBrowserFailure(
                    roleFind))
            {
                FailTest(
                    "Role Find"
                );

                return;
            }

            Log(
                "PASS: Role Find"
            );

            // =================================================
            // 5. ROLE WAIT
            // =================================================

            Log(
                "TEST: Role wait..."
            );

            string roleWait =
                await BrowserTools.WaitForRoleAsync(
                    "textbox",
                    "Test input",
                    true,
                    "visible",
                    10
                );

            Log(
                roleWait
            );

            if (IsBrowserFailure(
                    roleWait))
            {
                FailTest(
                    "Role Wait"
                );

                return;
            }

            Log(
                "PASS: Role Wait"
            );

            // =================================================
            // 6. ROLE FILL
            // =================================================

            Log(
                "TEST: Role-based fill..."
            );

            string roleFill =
                await BrowserTools.FillRoleAsync(
                    "textbox",
                    "Test input",
                    true,
                    "Operator AI 0.6E"
                );

            Log(
                roleFill
            );

            if (IsBrowserFailure(
                    roleFill))
            {
                FailTest(
                    "Role Fill"
                );

                return;
            }

            // =================================================
            // 7. GET VALUE
            // =================================================

            Log(
                "TEST: Get field value..."
            );

            string valueResult =
                await BrowserTools.GetValueAsync(
                    "css",
                    "#testInput"
                );

            Log(
                valueResult
            );

            if (IsBrowserFailure(
                    valueResult)
                ||
                !valueResult.Contains(
                    "Operator AI 0.6E",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Value"
                );

                return;
            }

            Log(
                "PASS: Role Fill + Get Value"
            );

            // =================================================
            // 8. EXACT TEXT
            // =================================================

            Log(
                "TEST: Exact text targeting..."
            );

            string exactTextResult =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Exact Target 0.6E"
                );

            Log(
                exactTextResult
            );

            if (IsBrowserFailure(
                    exactTextResult))
            {
                FailTest(
                    "Exact Text"
                );

                return;
            }

            Log(
                "PASS: Exact Text"
            );

            // =================================================
            // 9. GET ELEMENT TEXT
            // =================================================

            Log(
                "TEST: Get element text..."
            );

            string elementText =
                await BrowserTools.GetElementTextAsync(
                    "css",
                    "#exactTarget"
                );

            Log(
                elementText
            );

            if (IsBrowserFailure(
                    elementText)
                ||
                !elementText.Contains(
                    "Exact Target 0.6E",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Element Text"
                );

                return;
            }

            Log(
                "PASS: Get Element Text"
            );

            // =================================================
            // 10. GET ATTRIBUTE
            // =================================================

            Log(
                "TEST: Get attribute..."
            );

            string attributeResult =
                await BrowserTools.GetAttributeAsync(
                    "css",
                    "#attributeLink",
                    "data-purpose"
                );

            Log(
                attributeResult
            );

            if (IsBrowserFailure(
                    attributeResult)
                ||
                !attributeResult.Contains(
                    "navigation-test",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Attribute"
                );

                return;
            }

            Log(
                "PASS: Get Attribute"
            );

            // =================================================
            // 11. VISIBILITY - BEFORE REVEAL
            // =================================================

            Log(
                "TEST: Visibility before reveal..."
            );

            string hiddenResult =
                await BrowserTools.IsVisibleAsync(
                    "css",
                    "#asyncMessage"
                );

            Log(
                hiddenResult
            );

            if (!hiddenResult.Contains(
                    "Visible=False",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Initial Visibility"
                );

                return;
            }

            Log(
                "PASS: Hidden state detected"
            );

            // =================================================
            // 12. ROLE BUTTON FIND + CLICK
            // =================================================

            Log(
                "TEST: Role-based button targeting..."
            );

            string buttonFind =
                await BrowserTools.FindByRoleAsync(
                    "button",
                    "Reveal async message",
                    true
                );

            Log(
                buttonFind
            );

            if (IsBrowserFailure(
                    buttonFind))
            {
                FailTest(
                    "Role Button Find"
                );

                return;
            }

            string buttonClick =
                await BrowserTools.ClickRoleAsync(
                    "button",
                    "Reveal async message",
                    true
                );

            Log(
                buttonClick
            );

            if (IsBrowserFailure(
                    buttonClick))
            {
                FailTest(
                    "Role Click"
                );

                return;
            }

            Log(
                "PASS: Role Click"
            );

            // =================================================
            // 13. WAIT FOR TEXT
            // =================================================

            Log(
                "TEST: Wait for dynamically appearing text..."
            );

            string waitText =
                await BrowserTools.WaitForTextAsync(
                    "Async message ready",
                    true,
                    10
                );

            Log(
                waitText
            );

            if (IsBrowserFailure(
                    waitText))
            {
                FailTest(
                    "Wait For Text"
                );

                return;
            }

            // =================================================
            // 14. VISIBILITY - AFTER REVEAL
            // =================================================

            string visibleResult =
                await BrowserTools.IsVisibleAsync(
                    "css",
                    "#asyncMessage"
                );

            Log(
                visibleResult
            );

            if (!visibleResult.Contains(
                    "Visible=True",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Final Visibility"
                );

                return;
            }

            Log(
                "PASS: Wait For Text + Visibility"
            );

            // =================================================
            // 15. PAGE SCROLL
            // =================================================

            Log(
                "TEST: Page scrolling..."
            );

            string scrollResult =
                await BrowserTools.ScrollPageAsync(
                    900
                );

            Log(
                scrollResult
            );

            if (IsBrowserFailure(
                    scrollResult))
            {
                FailTest(
                    "Page Scroll"
                );

                return;
            }

            Log(
                "PASS: Page Scroll"
            );

            // =================================================
            // 16. SCROLL TO ELEMENT
            // =================================================

            Log(
                "TEST: Scroll target into view..."
            );

            string scrollToResult =
                await BrowserTools.ScrollToElementAsync(
                    "css",
                    "#bottomTarget"
                );

            Log(
                scrollToResult
            );

            if (IsBrowserFailure(
                    scrollToResult))
            {
                FailTest(
                    "Scroll To Element"
                );

                return;
            }

            string bottomText =
                await BrowserTools.GetElementTextAsync(
                    "css",
                    "#bottomTarget"
                );

            Log(
                bottomText
            );

            if (!bottomText.Contains(
                    "Bottom Target Reached",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Scroll Target Verification"
                );

                return;
            }

            Log(
                "PASS: Scroll To Element"
            );

            // =================================================
            // 17. SCREENSHOT
            // =================================================

            Log(
                "TEST: Full-page screenshot..."
            );

            string screenshotName =
                "0.6e-reliability.png";

            string screenshotFullPath =
                Path.Combine(
                    BrowserTools.GetScreenshotsDirectory(),
                    screenshotName
                );

            if (File.Exists(
                    screenshotFullPath))
            {
                try
                {
                    File.Delete(
                        screenshotFullPath
                    );
                }
                catch
                {
                }
            }

            string screenshotResult =
                await BrowserTools.ScreenshotAsync(
                    screenshotName,
                    true
                );

            Log(
                screenshotResult
            );

            if (IsBrowserFailure(
                    screenshotResult)
                ||
                !File.Exists(
                    screenshotFullPath))
            {
                FailTest(
                    "Screenshot"
                );

                return;
            }

            Log(
                BrowserTools.ListScreenshots()
            );

            Log(
                "PASS: Screenshot"
            );

            // =================================================
            // 18. ROLE LINK FIND
            // =================================================

            Log(
                "TEST: Role-based link discovery..."
            );

            string linkFind =
                await BrowserTools.FindByRoleAsync(
                    "link",
                    "Go to next page",
                    true
                );

            Log(
                linkFind
            );

            if (IsBrowserFailure(
                    linkFind))
            {
                FailTest(
                    "Role Link Find"
                );

                return;
            }

            // =================================================
            // 19. ROLE LINK CLICK
            // =================================================

            Log(
                "TEST: Role-based navigation click..."
            );

            string linkClick =
                await BrowserTools.ClickRoleAsync(
                    "link",
                    "Go to next page",
                    true
                );

            Log(
                linkClick
            );

            if (IsBrowserFailure(
                    linkClick))
            {
                FailTest(
                    "Role Link Click"
                );

                return;
            }

            // =================================================
            // 20. WAIT FOR URL
            // =================================================

            Log(
                "TEST: Wait for navigation URL..."
            );

            string waitUrlResult =
                await BrowserTools.WaitForUrlAsync(
                    "**/next",
                    10
                );

            Log(
                waitUrlResult
            );

            if (IsBrowserFailure(
                    waitUrlResult))
            {
                FailTest(
                    "Wait For URL"
                );

                return;
            }

            Log(
                "PASS: Wait For URL"
            );

            // =================================================
            // 21. VERIFY NEXT PAGE TEXT
            // =================================================

            Log(
                "TEST: Verify final page..."
            );

            string nextPageWait =
                await BrowserTools.WaitForTextAsync(
                    "Navigation Complete",
                    true,
                    10
                );

            Log(
                nextPageWait
            );

            if (IsBrowserFailure(
                    nextPageWait))
            {
                FailTest(
                    "Navigation Page Text"
                );

                return;
            }

            string pageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(
                pageInfo
            );

            if (!pageInfo.Contains(
                    "Operator AI Navigation Complete",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Final Page Title"
                );

                return;
            }

            Log(
                "PASS: Final Navigation Verification"
            );

            // =================================================
            // COMPLETE
            // =================================================

            Log(
                "=========================================="
            );

            Log(
                "SUCCESS: VERSION 0.6E RELIABILITY TEST PASSED."
            );

            Log(
                "Role Find: PASS"
            );

            Log(
                "Role Wait: PASS"
            );

            Log(
                "Role Fill: PASS"
            );

            Log(
                "Get Value: PASS"
            );

            Log(
                "Exact Text: PASS"
            );

            Log(
                "Get Element Text: PASS"
            );

            Log(
                "Get Attribute: PASS"
            );

            Log(
                "Visibility: PASS"
            );

            Log(
                "Role Click: PASS"
            );

            Log(
                "Wait For Text: PASS"
            );

            Log(
                "Page Scroll: PASS"
            );

            Log(
                "Scroll To Element: PASS"
            );

            Log(
                "Screenshot: PASS"
            );

            Log(
                "Role Link Navigation: PASS"
            );

            Log(
                "Wait For URL: PASS"
            );

            Log(
                "Final Page Verification: PASS"
            );

            Log(
                $"Screenshot: {screenshotFullPath}"
            );

            Log(
                "=========================================="
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.6E RELIABILITY TEST ERROR: {ex.Message}"
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
        }
    }

    // =========================================================
    // TEST FAILURE
    // =========================================================

    private void FailTest(
        string testName)
    {
        Log(
            $"FAIL: {testName}"
        );

        Log(
            "VERSION 0.6E RELIABILITY TEST STOPPED."
        );
    }

    // =========================================================
    // RESULT HELPER
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
    // LOG
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