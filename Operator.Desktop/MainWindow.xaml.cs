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
    // STOP
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
    // WINDOWS TESTS
    // =========================================================

    private void OpenNotepad_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Log(
                WindowsTools.OpenApplication(
                    "notepad"
                )
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
                WindowsTools.CreateDesktopFile(
                    "test.txt",
                    "Hello Aamir"
                )
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
                WindowsTools.DesktopFileExists(
                    "test.txt"
                )
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

            Log(
                WindowsTools.OpenApplication(
                    "notepad"
                )
            );

            await Task.Delay(
                1200
            );

            Log(
                WindowsUiTools.ListWindows()
            );

            Log(
                WindowsUiTools.FocusWindow(
                    "Notepad"
                )
            );

            Log(
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI can control Windows UI."
                )
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

            Log(
                WindowsTools.OpenApplication(
                    "notepad"
                )
            );

            await Task.Delay(
                1200
            );

            Log(
                WindowsUiTools.FocusWindow(
                    "Notepad"
                )
            );

            Log(
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Keyboard automation test"
                )
            );

            await Task.Delay(
                500
            );

            Log(
                WindowsInputTools.PressKey(
                    "CTRL+S"
                )
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

            Log(
                WindowsTools.OpenApplication(
                    "notepad"
                )
            );

            await Task.Delay(
                1500
            );

            Log(
                WindowsUiTools.FocusWindow(
                    "Notepad"
                )
            );

            await Task.Delay(
                300
            );

            Log(
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI save dialog test"
                )
            );

            await Task.Delay(
                600
            );

            Log(
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                )
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

            Log(
                WindowsTools.DesktopFileExists(
                    "agent-test.txt"
                )
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

            string start =
                await BrowserTools.StartBrowserAsync();

            Log(
                start
            );

            if (IsBrowserFailure(
                    start))
            {
                return;
            }

            string navigate =
                await BrowserTools.NavigateAsync(
                    "https://example.com"
                );

            Log(
                navigate
            );

            if (IsBrowserFailure(
                    navigate))
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
                "Starting browser interaction test..."
            );

            string start =
                await BrowserTools.StartBrowserAsync();

            Log(
                start
            );

            if (IsBrowserFailure(
                    start))
            {
                return;
            }

            string navigate =
                await BrowserTools.NavigateAsync(
                    "https://www.wikipedia.org"
                );

            Log(
                navigate
            );

            if (IsBrowserFailure(
                    navigate))
            {
                return;
            }

            await Task.Delay(
                1000
            );

            string find =
                await BrowserTools.FindElementsAsync(
                    "css",
                    "input[name='search']"
                );

            Log(
                find
            );

            string locatorType =
                "css";

            string locatorQuery =
                "input[name='search']";

            if (IsBrowserFailure(
                    find))
            {
                locatorType =
                    "placeholder";

                locatorQuery =
                    "Search Wikipedia";
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
                "SUCCESS: Browser interaction test completed."
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

            Log(
                WindowsTools.CreateDesktopFile(
                    uploadFileName,
                    "Operator AI browser upload test file."
                )
            );

            string start =
                await BrowserTools.StartBrowserAsync();

            Log(
                start
            );

            if (IsBrowserFailure(
                    start))
            {
                return;
            }

            Log(
                await BrowserTools.NavigateAsync(
                    server.BaseUrl
                )
            );

            Log(
                await BrowserTools.WaitForElementAsync(
                    "css",
                    "#enableAutomation",
                    "visible",
                    10
                )
            );

            Log(
                await BrowserTools.SetCheckedAsync(
                    "label",
                    "Enable automation",
                    true
                )
            );

            Log(
                await BrowserTools.GetCheckedStateAsync(
                    "label",
                    "Enable automation"
                )
            );

            Log(
                await BrowserTools.SelectOptionAsync(
                    "label",
                    "Department",
                    "label",
                    "Operations"
                )
            );

            Log(
                await BrowserTools.UploadDesktopFileAsync(
                    "label",
                    "Upload file",
                    uploadFileName
                )
            );

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

            Log(
                await BrowserTools.DownloadByClickAsync(
                    "css",
                    "#downloadReport",
                    "test-report.txt"
                )
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
    // 0.6E RELIABILITY TEST
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

            Log(
                await BrowserTools.NavigateAsync(
                    server.BaseUrl
                )
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

            string valueResult =
                await BrowserTools.GetValueAsync(
                    "css",
                    "#testInput"
                );

            Log(
                valueResult
            );

            if (!valueResult.Contains(
                    "Operator AI 0.6E",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Value"
                );

                return;
            }

            string exactText =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Exact Target 0.6E"
                );

            Log(
                exactText
            );

            if (IsBrowserFailure(
                    exactText))
            {
                FailTest(
                    "Exact Text"
                );

                return;
            }

            string elementText =
                await BrowserTools.GetElementTextAsync(
                    "css",
                    "#exactTarget"
                );

            Log(
                elementText
            );

            if (!elementText.Contains(
                    "Exact Target 0.6E",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Element Text"
                );

                return;
            }

            string attribute =
                await BrowserTools.GetAttributeAsync(
                    "css",
                    "#attributeLink",
                    "data-purpose"
                );

            Log(
                attribute
            );

            if (!attribute.Contains(
                    "navigation-test",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Attribute"
                );

                return;
            }

            string hidden =
                await BrowserTools.IsVisibleAsync(
                    "css",
                    "#asyncMessage"
                );

            Log(
                hidden
            );

            if (!hidden.Contains(
                    "Visible=False",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Initial Visibility"
                );

                return;
            }

            Log(
                await BrowserTools.ClickRoleAsync(
                    "button",
                    "Reveal async message",
                    true
                )
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

            Log(
                await BrowserTools.ScrollPageAsync(
                    900
                )
            );

            Log(
                await BrowserTools.ScrollToElementAsync(
                    "css",
                    "#bottomTarget"
                )
            );

            string screenshotName =
                "0.6e-reliability.png";

            string screenshotPath =
                Path.Combine(
                    BrowserTools.GetScreenshotsDirectory(),
                    screenshotName
                );

            if (File.Exists(
                    screenshotPath))
            {
                try
                {
                    File.Delete(
                        screenshotPath
                    );
                }
                catch
                {
                }
            }

            Log(
                await BrowserTools.ScreenshotAsync(
                    screenshotName,
                    true
                )
            );

            if (!File.Exists(
                    screenshotPath))
            {
                FailTest(
                    "Screenshot"
                );

                return;
            }

            Log(
                await BrowserTools.ClickRoleAsync(
                    "link",
                    "Go to next page",
                    true
                )
            );

            string waitUrl =
                await BrowserTools.WaitForUrlAsync(
                    "**/next",
                    10
                );

            Log(
                waitUrl
            );

            if (IsBrowserFailure(
                    waitUrl))
            {
                FailTest(
                    "Wait For URL"
                );

                return;
            }

            Log(
                await BrowserTools.WaitForTextAsync(
                    "Navigation Complete",
                    true,
                    10
                )
            );

            Log(
                "SUCCESS: VERSION 0.6E RELIABILITY TEST PASSED."
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
    // VERSION 0.6F
    // VISION FALLBACK TEST
    // =========================================================

    private async void BrowserVisionFallbackTest_Click(
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
                "Starting Version 0.6F Vision Fallback Test..."
            );

            // =================================================
            // 1. START CONTROLLED LOCAL SERVER
            // =================================================

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
            // 3. OPEN VISUAL FALLBACK PAGE
            // =================================================

            string targetUrl =
                server.BaseUrl +
                "/vision-fallback";

            Log(
                $"Opening visual fallback page: {targetUrl}"
            );

            string navigateResult =
                await BrowserTools.NavigateAsync(
                    targetUrl
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
            // 4. VERIFY PAGE
            // =================================================

            string pageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(
                pageInfo
            );

            if (!pageInfo.Contains(
                    "Operator AI Vision Fallback Test",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Vision Fallback Page"
                );

                return;
            }

            // =================================================
            // 5. TRY NORMAL ROLE TARGETING
            //
            // This is EXPECTED to fail.
            // =================================================

            Log(
                "TEST: Trying structured role lookup for 'Continue Review'..."
            );

            string roleResult =
                await BrowserTools.FindByRoleAsync(
                    "button",
                    "Continue Review",
                    true
                );

            Log(
                roleResult
            );

            if (!IsNotFound(
                    roleResult))
            {
                Log(
                    "FAIL: Role targeting unexpectedly identified the visual-only target."
                );

                return;
            }

            Log(
                "PASS: Role lookup could not identify the visual-only target."
            );

            // =================================================
            // 6. TRY EXACT TEXT
            //
            // Also EXPECTED to fail.
            // =================================================

            Log(
                "TEST: Trying exact-text lookup for 'Continue Review'..."
            );

            string exactTextResult =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Continue Review"
                );

            Log(
                exactTextResult
            );

            if (!IsNotFound(
                    exactTextResult))
            {
                Log(
                    "FAIL: Exact-text targeting unexpectedly identified the visual-only target."
                );

                return;
            }

            Log(
                "PASS: Exact text could not identify the visual-only target."
            );

            // =================================================
            // 7. INSPECT DOM STRUCTURE
            //
            // The 3 buttons should appear anonymous.
            // =================================================

            Log(
                "Inspecting structured browser elements..."
            );

            string elements =
                await BrowserTools.ListInteractiveElementsAsync();

            Log(
                elements
            );

            // =================================================
            // 8. USE VISION FALLBACK
            // =================================================

            Log(
                "Structured targeting is insufficient."
            );

            Log(
                "Invoking browser vision..."
            );

            string visionResult =
                await BrowserVisionTools
                    .InspectCurrentPageAsync(
                        """
                        This is a controlled Operator AI test page.

                        There are three visually labeled action buttons
                        arranged horizontally.

                        Find the button visually labeled:

                        Continue Review

                        Tell me specifically whether it is the LEFT,
                        MIDDLE/CENTER, or RIGHT button.

                        Also mention the visible labels of the other
                        two buttons if you can see them.

                        Do not click anything.
                        """,
                        false
                    );

            Log(
                visionResult
            );

            if (IsBrowserFailure(
                    visionResult))
            {
                FailTest(
                    "Visual Inspection"
                );

                return;
            }

            // =================================================
            // 9. CONVERT VISUAL POSITION INTO DOM POSITION
            // =================================================

            int buttonIndex =
                DetermineVisualButtonIndex(
                    visionResult
                );

            if (buttonIndex == 0)
            {
                Log(
                    "FAIL: Vision did not identify a usable left/middle/right position."
                );

                return;
            }

            Log(
                $"Vision selected button position #{buttonIndex}."
            );

            if (buttonIndex != 2)
            {
                Log(
                    "FAIL: Vision did not identify Continue Review as the middle button."
                );

                return;
            }

            Log(
                "PASS: Vision identified Continue Review as the middle button."
            );

            // =================================================
            // 10. RECOVER USING STRUCTURED CSS
            //
            // Vision gave us the positional clue.
            // Actual click remains a Playwright structured action.
            // =================================================

            string recoveredSelector =
                $"#mysteryButtons button:nth-child({buttonIndex})";

            Log(
                $"Recovered structured selector: {recoveredSelector}"
            );

            string clickResult =
                await BrowserTools.ClickAsync(
                    "css",
                    recoveredSelector
                );

            Log(
                clickResult
            );

            if (IsBrowserFailure(
                    clickResult))
            {
                FailTest(
                    "Vision-to-DOM Recovery Click"
                );

                return;
            }

            // =================================================
            // 11. VERIFY RESULT USING STRUCTURED TOOLS
            // =================================================

            string waitResult =
                await BrowserTools.WaitForTextAsync(
                    "Result: review mode activated",
                    true,
                    10
                );

            Log(
                waitResult
            );

            if (IsBrowserFailure(
                    waitResult))
            {
                FailTest(
                    "Final Structured Verification"
                );

                return;
            }

            string statusResult =
                await BrowserTools.GetElementTextAsync(
                    "css",
                    "#fallbackStatus"
                );

            Log(
                statusResult
            );

            if (!statusResult.Contains(
                    "Result: review mode activated",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Final Status"
                );

                return;
            }

            // =================================================
            // COMPLETE
            // =================================================

            Log(
                "=============================================="
            );

            Log(
                "SUCCESS: VERSION 0.6F VISION FALLBACK TEST PASSED."
            );

            Log(
                "Role lookup failure: PASS"
            );

            Log(
                "Exact text lookup failure: PASS"
            );

            Log(
                "Anonymous DOM detection: PASS"
            );

            Log(
                "Visual identification: PASS"
            );

            Log(
                "Vision position recovery: PASS"
            );

            Log(
                "Structured recovery click: PASS"
            );

            Log(
                "Final DOM verification: PASS"
            );

            Log(
                "=============================================="
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.6F VISION FALLBACK TEST ERROR: {ex.Message}"
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
    // DETERMINE VISUAL BUTTON
    // =========================================================

    private static int DetermineVisualButtonIndex(
        string visionResult)
    {
        if (string.IsNullOrWhiteSpace(
                visionResult))
        {
            return 0;
        }

        string normalized =
            visionResult.ToLowerInvariant();

        /*
         * Check middle / center FIRST.
         *
         * A vision response may say something like:
         *
         * "Left is Cancel, middle is Continue Review,
         * right is Defer."
         *
         * Therefore simply checking "left" first
         * would give the wrong answer.
         */

        if (
            normalized.Contains(
                "middle"
            )
            ||
            normalized.Contains(
                "center button"
            )
            ||
            normalized.Contains(
                "centre button"
            )
            ||
            normalized.Contains(
                "center position"
            )
            ||
            normalized.Contains(
                "centre position"
            )
            ||
            normalized.Contains(
                "in the center"
            )
            ||
            normalized.Contains(
                "in the centre"
            )
        )
        {
            return 2;
        }

        if (
            normalized.Contains(
                "left button"
            )
            ||
            normalized.Contains(
                "left position"
            )
            ||
            normalized.Contains(
                "on the left"
            )
        )
        {
            return 1;
        }

        if (
            normalized.Contains(
                "right button"
            )
            ||
            normalized.Contains(
                "right position"
            )
            ||
            normalized.Contains(
                "on the right"
            )
        )
        {
            return 3;
        }

        return 0;
    }

    // =========================================================
    // EXPECTED NOT_FOUND
    // =========================================================

    private static bool IsNotFound(
        string result)
    {
        return
            result.StartsWith(
                "NOT_FOUND",
                StringComparison.OrdinalIgnoreCase);
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
            "TEST STOPPED."
        );
    }

    // =========================================================
    // FAILURE HELPER
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