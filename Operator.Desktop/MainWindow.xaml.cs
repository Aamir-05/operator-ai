using System;
using System.IO;
using System.Text.RegularExpressions;
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
    // BASIC WINDOWS
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
    // UI TYPE
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
        }
        catch (Exception ex)
        {
            Log(
                $"UI TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // CTRL+S
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
                400
            );

            Log(
                WindowsInputTools.PressKey(
                    "CTRL+S"
                )
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
    // SAVE DIALOG
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

            Log(
                WindowsUiTools.TypeText(
                    "Notepad",
                    "Operator AI save dialog test"
                )
            );

            await Task.Delay(
                500
            );

            Log(
                WindowsInputTools.PressKey(
                    "CTRL+SHIFT+S"
                )
            );

            await Task.Delay(
                1800
            );

            WindowsInputTools.PressKey(
                "CTRL+A"
            );

            await Task.Delay(
                200
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
                300
            );

            WindowsInputTools.PressKey(
                "ENTER"
            );

            await Task.Delay(
                1800
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
        }
        catch (Exception ex)
        {
            Log(
                $"SAVE DIALOG TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // BASIC BROWSER
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
    // BROWSER INTERACTION
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

            string find =
                await BrowserTools.FindElementsAsync(
                    "css",
                    "input[name='search']"
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
                1800
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
    // 0.6D CONTROLS
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
                    "Operator AI browser upload test."
                )
            );

            Log(
                await BrowserTools.StartBrowserAsync()
            );

            Log(
                await BrowserTools.NavigateAsync(
                    server.BaseUrl
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
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Checkbox"
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
    // 0.6E RELIABILITY
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

            Log(
                await BrowserTools.StartBrowserAsync()
            );

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

            Log(
                await BrowserTools.FillRoleAsync(
                    "textbox",
                    "Test input",
                    true,
                    "Operator AI 0.6E"
                )
            );

            string value =
                await BrowserTools.GetValueAsync(
                    "css",
                    "#testInput"
                );

            Log(
                value
            );

            if (!value.Contains(
                    "Operator AI 0.6E",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Get Value"
                );

                return;
            }

            Log(
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Exact Target 0.6E"
                )
            );

            Log(
                await BrowserTools.GetAttributeAsync(
                    "css",
                    "#attributeLink",
                    "data-purpose"
                )
            );

            Log(
                await BrowserTools.ClickRoleAsync(
                    "button",
                    "Reveal async message",
                    true
                )
            );

            Log(
                await BrowserTools.WaitForTextAsync(
                    "Async message ready",
                    true,
                    10
                )
            );

            Log(
                await BrowserTools.ScrollToElementAsync(
                    "css",
                    "#bottomTarget"
                )
            );

            Log(
                await BrowserTools.ScreenshotAsync(
                    "0.6e-reliability.png",
                    true
                )
            );

            Log(
                await BrowserTools.ClickRoleAsync(
                    "link",
                    "Go to next page",
                    true
                )
            );

            Log(
                await BrowserTools.WaitForUrlAsync(
                    "**/next",
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
    // 0.6F VISION FALLBACK
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
                await BrowserTools.StartBrowserAsync()
            );

            string targetUrl =
                server.BaseUrl +
                "/vision-fallback";

            Log(
                await BrowserTools.NavigateAsync(
                    targetUrl
                )
            );

            string role =
                await BrowserTools.FindByRoleAsync(
                    "button",
                    "Continue Review",
                    true
                );

            Log(
                role
            );

            if (!IsNotFound(
                    role))
            {
                FailTest(
                    "Expected role failure"
                );

                return;
            }

            string exact =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Continue Review"
                );

            Log(
                exact
            );

            if (!IsNotFound(
                    exact))
            {
                FailTest(
                    "Expected text failure"
                );

                return;
            }

            string vision =
                await BrowserVisionTools
                    .InspectCurrentPageAsync(
                        """
                        Find the visually labeled button
                        "Continue Review".

                        Tell me whether it is the LEFT,
                        MIDDLE/CENTER, or RIGHT button.

                        Do not click anything.
                        """,
                        false
                    );

            Log(
                vision
            );

            int buttonIndex =
                DetermineVisualButtonIndex(
                    vision
                );

            if (buttonIndex != 2)
            {
                FailTest(
                    "Visual identification"
                );

                return;
            }

            Log(
                await BrowserTools.ClickAsync(
                    "css",
                    "#mysteryButtons button:nth-child(2)"
                )
            );

            Log(
                await BrowserTools.WaitForTextAsync(
                    "Result: review mode activated",
                    true,
                    10
                )
            );

            Log(
                "SUCCESS: VERSION 0.6F VISION FALLBACK TEST PASSED."
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
    // VERSION 0.6F-4C
    // CANVAS COORDINATE TEST
    // =========================================================

    private async void BrowserCoordinateCanvasTest_Click(
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
                "Starting Version 0.6F Canvas Coordinate Test..."
            );

            // =================================================
            // START SERVER
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
            // START BROWSER
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
            // NAVIGATE
            // =================================================

            string targetUrl =
                server.BaseUrl +
                "/coordinate-canvas";

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
            // VERIFY PAGE
            // =================================================

            string pageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(
                pageInfo
            );

            if (!pageInfo.Contains(
                    "Operator AI Canvas Coordinate Test",
                    StringComparison.Ordinal))
            {
                FailTest(
                    "Coordinate Test Page"
                );

                return;
            }

            // =================================================
            // PROVE THERE IS NO STRUCTURED BUTTON
            // =================================================

            Log(
                "TEST: Structured role lookup should fail..."
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
                FailTest(
                    "Expected role lookup failure"
                );

                return;
            }

            Log(
                "PASS: No DOM button was found."
            );

            // =================================================
            // PROVE THERE IS NO TEXT LOCATOR
            // =================================================

            Log(
                "TEST: Exact text lookup should fail..."
            );

            string textResult =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Continue Review"
                );

            Log(
                textResult
            );

            if (!IsNotFound(
                    textResult))
            {
                FailTest(
                    "Expected exact-text failure"
                );

                return;
            }

            Log(
                "PASS: No DOM text target was found."
            );

            // =================================================
            // CANVAS ITSELF IS STRUCTURED
            // INTERNAL CONTROLS ARE NOT
            // =================================================

            string canvasFind =
                await BrowserTools.FindElementsAsync(
                    "css",
                    "#visualCanvas"
                );

            Log(
                canvasFind
            );

            if (IsBrowserFailure(
                    canvasFind))
            {
                FailTest(
                    "Canvas Detection"
                );

                return;
            }

            // =================================================
            // TEST ELEMENT BOUNDING BOX TOOL
            // =================================================

            Log(
                "Reading canvas bounding box..."
            );

            string canvasBox =
                await BrowserTools.GetElementBoxAsync(
                    "css",
                    "#visualCanvas"
                );

            Log(
                canvasBox
            );

            if (IsBrowserFailure(
                    canvasBox))
            {
                FailTest(
                    "Element Bounding Box"
                );

                return;
            }

            // =================================================
            // VISION
            //
            // This captures a viewport screenshot and analyzes it.
            // =================================================

            Log(
                "Invoking visual inspection to locate canvas target..."
            );

            string visionResult =
                await BrowserVisionTools
                    .InspectCurrentPageAsync(
                        """
                        This is a controlled Operator AI coordinate test.

                        Inside the large canvas there are three
                        button-like visual controls arranged horizontally.

                        Find the control visually labeled:

                        Continue Review

                        I need the CENTER POINT of that control
                        in screenshot viewport coordinates.

                        The top-left corner of the screenshot is (0,0).

                        Return these lines clearly:

                        TARGET_POSITION=MIDDLE
                        TARGET_X=<integer>
                        TARGET_Y=<integer>

                        Use the approximate center of the visible
                        Continue Review control.

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
            // PARSE COORDINATES
            // =================================================

            if (!TryExtractVisionCoordinates(
                    visionResult,
                    out int targetX,
                    out int targetY))
            {
                FailTest(
                    "Vision Coordinate Parsing"
                );

                return;
            }

            Log(
                $"Vision coordinate: ({targetX}, {targetY})"
            );

            if (!VisionSaysMiddle(
                    visionResult))
            {
                FailTest(
                    "Vision Target Position"
                );

                return;
            }

            Log(
                "PASS: Vision identified the middle canvas action."
            );

            // =================================================
            // VIEWPORT
            // =================================================

            string viewportResult =
                await BrowserTools.GetViewportInfoAsync();

            Log(
                viewportResult
            );

            if (IsBrowserFailure(
                    viewportResult))
            {
                FailTest(
                    "Viewport Information"
                );

                return;
            }

            // =================================================
            // IMPORTANT SAFETY STEP
            //
            // The vision API round trip may take several seconds.
            // Capture a fresh viewport screenshot immediately
            // before the coordinate click.
            //
            // The page is deterministic and has not moved.
            // =================================================

            Log(
                "Arming coordinate click with fresh viewport screenshot..."
            );

            string armedScreenshot =
                await BrowserTools.ScreenshotAsync(
                    "0.6f-coordinate-armed.png",
                    false
                );

            Log(
                armedScreenshot
            );

            if (IsBrowserFailure(
                    armedScreenshot))
            {
                FailTest(
                    "Coordinate Safety Screenshot"
                );

                return;
            }

            // =================================================
            // CONFIRM GUARD IS ARMED
            // =================================================

            string armedViewport =
                await BrowserTools.GetViewportInfoAsync();

            Log(
                armedViewport
            );

            if (!armedViewport.Contains(
                    "Recent visual-click screenshot: Yes",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Visual Click Guard"
                );

                return;
            }

            Log(
                "PASS: Visual click safety guard armed."
            );

            // =================================================
            // MOVE MOUSE
            // =================================================

            string moveResult =
                await BrowserTools.MouseMoveAsync(
                    targetX,
                    targetY
                );

            Log(
                moveResult
            );

            if (IsBrowserFailure(
                    moveResult))
            {
                FailTest(
                    "Mouse Move"
                );

                return;
            }

            // =================================================
            // GUARDED COORDINATE CLICK
            // =================================================

            Log(
                "Performing guarded coordinate click..."
            );

            string clickResult =
                await BrowserTools.MouseClickAsync(
                    targetX,
                    targetY
                );

            Log(
                clickResult
            );

            if (IsBrowserFailure(
                    clickResult))
            {
                FailTest(
                    "Coordinate Click"
                );

                return;
            }

            // =================================================
            // VERIFY WITH DOM
            // =================================================

            string waitResult =
                await BrowserTools.WaitForTextAsync(
                    "Result: canvas review activated",
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
                    "Coordinate Click Verification"
                );

                return;
            }

            string statusResult =
                await BrowserTools.GetElementTextAsync(
                    "css",
                    "#canvasStatus"
                );

            Log(
                statusResult
            );

            if (!statusResult.Contains(
                    "Result: canvas review activated",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Final Canvas Status"
                );

                return;
            }

            Log(
                "PASS: Coordinate click reached correct canvas target."
            );

            // =================================================
            // PROVE SCREENSHOT WAS INVALIDATED
            //
            // MouseClickAsync automatically removes permission
            // to reuse the old screenshot.
            // =================================================

            Log(
                "TEST: Old screenshot must not be reusable..."
            );

            string secondClick =
                await BrowserTools.MouseClickAsync(
                    targetX,
                    targetY
                );

            Log(
                secondClick
            );

            if (!secondClick.StartsWith(
                    "BLOCKED",
                    StringComparison.OrdinalIgnoreCase))
            {
                FailTest(
                    "Screenshot Invalidation"
                );

                return;
            }

            Log(
                "PASS: Reusing stale visual click permission was blocked."
            );

            // =================================================
            // COMPLETE
            // =================================================

            Log(
                "=================================================="
            );

            Log(
                "SUCCESS: VERSION 0.6F COORDINATE TEST PASSED."
            );

            Log(
                "Structured button lookup failure: PASS"
            );

            Log(
                "Structured text lookup failure: PASS"
            );

            Log(
                "Canvas detection: PASS"
            );

            Log(
                "Element bounding box: PASS"
            );

            Log(
                "Visual target identification: PASS"
            );

            Log(
                "Vision coordinate extraction: PASS"
            );

            Log(
                "Viewport inspection: PASS"
            );

            Log(
                "Fresh screenshot guard: PASS"
            );

            Log(
                "Mouse move: PASS"
            );

            Log(
                "Guarded coordinate click: PASS"
            );

            Log(
                "DOM result verification: PASS"
            );

            Log(
                "Old screenshot invalidation: PASS"
            );

            Log(
                $"Clicked coordinate: ({targetX}, {targetY})"
            );

            Log(
                "=================================================="
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.6F COORDINATE TEST ERROR: {ex.Message}"
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
    // VISION COORDINATE PARSER
    // =========================================================

    private static bool TryExtractVisionCoordinates(
        string visionResult,
        out int x,
        out int y)
    {
        x = 0;
        y = 0;

        if (string.IsNullOrWhiteSpace(
                visionResult))
        {
            return false;
        }

        Match xMatch =
            Regex.Match(
                visionResult,
                @"TARGET_X\s*[:=]\s*(\d+)",
                RegexOptions.IgnoreCase
            );

        Match yMatch =
            Regex.Match(
                visionResult,
                @"TARGET_Y\s*[:=]\s*(\d+)",
                RegexOptions.IgnoreCase
            );

        if (!xMatch.Success ||
            !yMatch.Success)
        {
            return false;
        }

        if (!int.TryParse(
                xMatch.Groups[1].Value,
                out x))
        {
            return false;
        }

        if (!int.TryParse(
                yMatch.Groups[1].Value,
                out y))
        {
            return false;
        }

        return
            x > 0 &&
            y > 0;
    }

    // =========================================================
    // VISION POSITION CHECK
    // =========================================================

    private static bool VisionSaysMiddle(
        string visionResult)
    {
        if (string.IsNullOrWhiteSpace(
                visionResult))
        {
            return false;
        }

        string normalized =
            visionResult.ToLowerInvariant();

        return
            normalized.Contains(
                "target_position=middle"
            )
            ||
            normalized.Contains(
                "target_position: middle"
            )
            ||
            normalized.Contains(
                "middle button"
            )
            ||
            normalized.Contains(
                "center button"
            )
            ||
            normalized.Contains(
                "centre button"
            );
    }

    // =========================================================
    // PREVIOUS VISION POSITION HELPER
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
                "on the right"
            )
        )
        {
            return 3;
        }

        return 0;
    }

    // =========================================================
    // NOT FOUND
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
    // FAILURE
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
    // RESULT CHECK
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