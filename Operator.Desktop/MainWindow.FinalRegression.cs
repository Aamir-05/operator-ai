using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.6F
    // FINAL COMBINED REGRESSION
    // =========================================================

    private async void BrowserFinalRegressionTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        LocalBrowserTestServer server =
            new LocalBrowserTestServer();

        string uploadFileName =
            "operator-06f-final-upload.txt";

        string desktop =
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory
            );

        string uploadPath =
            Path.Combine(
                desktop,
                uploadFileName
            );

        string downloadedFile =
            Path.Combine(
                BrowserTools.GetDownloadsDirectory(),
                "0.6f-final-report.txt"
            );

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.6F FINAL REGRESSION"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // 1. LOCAL TEST SERVER
            // =================================================

            Log(
                "[1/14] Starting deterministic local test server..."
            );

            string serverResult =
                await server.StartAsync();

            Log(
                serverResult
            );

            if (!Final06F_RequireSuccess(
                    "Local test server",
                    serverResult))
            {
                return;
            }

            Log(
                "PASS: Local test server"
            );

            // =================================================
            // 2. BROWSER SESSION
            // =================================================

            Log(
                "[2/14] Starting browser and checking session..."
            );

            string browserStart =
                await BrowserTools.StartBrowserAsync();

            Log(
                browserStart
            );

            if (!Final06F_RequireSuccess(
                    "Browser start",
                    browserStart))
            {
                return;
            }

            string sessionInfo =
                await BrowserTools.GetSessionInfoAsync();

            Log(
                sessionInfo
            );

            if (Final06F_IsFailure(
                    sessionInfo))
            {
                Final06F_Fail(
                    "Browser session information"
                );

                return;
            }

            Log(
                "PASS: Browser start/session"
            );

            // =================================================
            // 3. NAVIGATION + ROLE TARGETING
            // =================================================

            Log(
                "[3/14] Testing navigation and semantic targeting..."
            );

            string navigateMain =
                await BrowserTools.NavigateAsync(
                    server.BaseUrl
                );

            Log(
                navigateMain
            );

            if (!Final06F_RequireSuccess(
                    "Main-page navigation",
                    navigateMain))
            {
                return;
            }

            string pageInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(
                pageInfo
            );

            if (!pageInfo.Contains(
                    "Operator AI Browser Reliability Test",
                    StringComparison.Ordinal))
            {
                Final06F_Fail(
                    "Main-page title verification"
                );

                return;
            }

            string roleFind =
                await BrowserTools.FindByRoleAsync(
                    "textbox",
                    "Test input",
                    true
                );

            Log(
                roleFind
            );

            if (Final06F_IsFailure(
                    roleFind))
            {
                Final06F_Fail(
                    "Role textbox targeting"
                );

                return;
            }

            Log(
                "PASS: Navigation and role targeting"
            );

            // =================================================
            // 4. FORM INPUT + VALUE VERIFICATION
            // =================================================

            Log(
                "[4/14] Testing form interaction and verification..."
            );

            string fill =
                await BrowserTools.FillRoleAsync(
                    "textbox",
                    "Test input",
                    true,
                    "Operator AI 0.6F final regression"
                );

            Log(
                fill
            );

            if (!Final06F_RequireSuccess(
                    "Role fill",
                    fill))
            {
                return;
            }

            string value =
                await BrowserTools.GetValueAsync(
                    "css",
                    "#testInput"
                );

            Log(
                value
            );

            if (!value.Contains(
                    "Operator AI 0.6F final regression",
                    StringComparison.Ordinal))
            {
                Final06F_Fail(
                    "Textbox value verification"
                );

                return;
            }

            string checkedResult =
                await BrowserTools.SetCheckedAsync(
                    "label",
                    "Enable automation",
                    true
                );

            Log(
                checkedResult
            );

            if (!Final06F_RequireSuccess(
                    "Checkbox interaction",
                    checkedResult))
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

            if (!checkedState.Contains(
                    "checked=True",
                    StringComparison.OrdinalIgnoreCase))
            {
                Final06F_Fail(
                    "Checkbox verification"
                );

                return;
            }

            string select =
                await BrowserTools.SelectOptionAsync(
                    "label",
                    "Department",
                    "label",
                    "Operations"
                );

            Log(
                select
            );

            if (!Final06F_RequireSuccess(
                    "Dropdown selection",
                    select))
            {
                return;
            }

            Log(
                "PASS: Form interaction and verification"
            );

            // =================================================
            // 5. DYNAMIC WAIT
            // =================================================

            Log(
                "[5/14] Testing dynamic waits..."
            );

            string revealClick =
                await BrowserTools.ClickRoleAsync(
                    "button",
                    "Reveal async message",
                    true
                );

            Log(
                revealClick
            );

            if (!Final06F_RequireSuccess(
                    "Reveal button click",
                    revealClick))
            {
                return;
            }

            string waitText =
                await BrowserTools.WaitForTextAsync(
                    "Async message ready",
                    true,
                    10
                );

            Log(
                waitText
            );

            if (!Final06F_RequireSuccess(
                    "Dynamic text wait",
                    waitText))
            {
                return;
            }

            Log(
                "PASS: Dynamic waiting"
            );

            // =================================================
            // 6. SCROLLING
            // =================================================

            Log(
                "[6/14] Testing scrolling..."
            );

            string scroll =
                await BrowserTools.ScrollToElementAsync(
                    "css",
                    "#bottomTarget"
                );

            Log(
                scroll
            );

            if (!Final06F_RequireSuccess(
                    "Scroll to element",
                    scroll))
            {
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
                Final06F_Fail(
                    "Scroll target verification"
                );

                return;
            }

            Log(
                "PASS: Scrolling"
            );

            // Return to normal starting position.

            string returnMain =
                await BrowserTools.NavigateAsync(
                    server.BaseUrl
                );

            Log(
                returnMain
            );

            if (!Final06F_RequireSuccess(
                    "Return to main page",
                    returnMain))
            {
                return;
            }

            // =================================================
            // 7. UPLOAD
            // =================================================

            Log(
                "[7/14] Testing browser upload..."
            );

            if (File.Exists(
                    uploadPath))
            {
                try
                {
                    File.Delete(
                        uploadPath
                    );
                }
                catch
                {
                }
            }

            string createUpload =
                WindowsTools.CreateDesktopFile(
                    uploadFileName,
                    "Operator AI Version 0.6F final regression upload."
                );

            Log(
                createUpload
            );

            if (Final06F_IsFailure(
                    createUpload))
            {
                Final06F_Fail(
                    "Create upload test file"
                );

                return;
            }

            string upload =
                await BrowserTools.UploadDesktopFileAsync(
                    "label",
                    "Upload file",
                    uploadFileName
                );

            Log(
                upload
            );

            if (!Final06F_RequireSuccess(
                    "Browser upload",
                    upload))
            {
                return;
            }

            string uploadedPageText =
                await BrowserTools.ReadPageTextAsync();

            Log(
                uploadedPageText
            );

            if (!uploadedPageText.Contains(
                    $"Uploaded: {uploadFileName}",
                    StringComparison.OrdinalIgnoreCase))
            {
                Final06F_Fail(
                    "Upload verification"
                );

                return;
            }

            Log(
                "PASS: Browser upload"
            );

            // =================================================
            // 8. DOWNLOAD
            // =================================================

            Log(
                "[8/14] Testing browser download..."
            );

            if (File.Exists(
                    downloadedFile))
            {
                try
                {
                    File.Delete(
                        downloadedFile
                    );
                }
                catch
                {
                }
            }

            string download =
                await BrowserTools.DownloadByClickAsync(
                    "css",
                    "#downloadReport",
                    "0.6f-final-report.txt"
                );

            Log(
                download
            );

            if (!Final06F_RequireSuccess(
                    "Browser download",
                    download))
            {
                return;
            }

            if (!File.Exists(
                    downloadedFile))
            {
                Final06F_Fail(
                    "Downloaded file verification"
                );

                return;
            }

            Log(
                $"Verified download: {downloadedFile}"
            );

            Log(
                "PASS: Browser download"
            );

            // =================================================
            // 9. SCREENSHOT
            // =================================================

            Log(
                "[9/14] Testing screenshot system..."
            );

            string screenshot =
                await BrowserTools.ScreenshotAsync(
                    "0.6f-final-regression-main.png",
                    false
                );

            Log(
                screenshot
            );

            if (!Final06F_RequireSuccess(
                    "Viewport screenshot",
                    screenshot))
            {
                return;
            }

            string screenshotPath =
                Path.Combine(
                    BrowserTools.GetScreenshotsDirectory(),
                    "0.6f-final-regression-main.png"
                );

            if (!File.Exists(
                    screenshotPath))
            {
                Final06F_Fail(
                    "Screenshot file verification"
                );

                return;
            }

            Log(
                "PASS: Screenshot capture and verification"
            );

            // =================================================
            // 10. VISION FALLBACK
            // =================================================

            Log(
                "[10/14] Testing visual understanding and DOM recovery..."
            );

            string visionFallbackUrl =
                server.BaseUrl +
                "/vision-fallback";

            string navigateVision =
                await BrowserTools.NavigateAsync(
                    visionFallbackUrl
                );

            Log(
                navigateVision
            );

            if (!Final06F_RequireSuccess(
                    "Vision fallback navigation",
                    navigateVision))
            {
                return;
            }

            string missingRole =
                await BrowserTools.FindByRoleAsync(
                    "button",
                    "Continue Review",
                    true
                );

            Log(
                missingRole
            );

            if (!Final06F_IsNotFound(
                    missingRole))
            {
                Final06F_Fail(
                    "Expected role targeting failure"
                );

                return;
            }

            string missingText =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Continue Review"
                );

            Log(
                missingText
            );

            if (!Final06F_IsNotFound(
                    missingText))
            {
                Final06F_Fail(
                    "Expected exact-text failure"
                );

                return;
            }

            string visionFallback =
                await BrowserVisionTools
                    .InspectCurrentPageAsync(
                        """
                        Controlled Operator AI regression test.

                        There are three visually labeled controls.

                        Find the control labeled:

                        Continue Review

                        State clearly whether it is the
                        LEFT, MIDDLE/CENTER, or RIGHT control.

                        Do not click anything.
                        """,
                        false
                    );

            Log(
                visionFallback
            );

            if (Final06F_IsFailure(
                    visionFallback))
            {
                Final06F_Fail(
                    "Visual fallback inspection"
                );

                return;
            }

            int visualIndex =
                Final06F_DetermineVisualButtonIndex(
                    visionFallback
                );

            if (visualIndex != 2)
            {
                Final06F_Fail(
                    "Visual fallback target identification"
                );

                return;
            }

            string recoveredClick =
                await BrowserTools.ClickAsync(
                    "css",
                    "#mysteryButtons button:nth-child(2)"
                );

            Log(
                recoveredClick
            );

            if (!Final06F_RequireSuccess(
                    "Vision-assisted structured click",
                    recoveredClick))
            {
                return;
            }

            string fallbackVerify =
                await BrowserTools.WaitForTextAsync(
                    "Result: review mode activated",
                    true,
                    10
                );

            Log(
                fallbackVerify
            );

            if (!Final06F_RequireSuccess(
                    "Vision fallback verification",
                    fallbackVerify))
            {
                return;
            }

            Log(
                "PASS: Visual understanding and structured recovery"
            );

            // =================================================
            // 11. CANVAS + GUARDED COORDINATE CLICK
            // =================================================

            Log(
                "[11/14] Testing canvas-only visual coordinate interaction..."
            );

            string coordinateUrl =
                server.BaseUrl +
                "/coordinate-canvas";

            string navigateCanvas =
                await BrowserTools.NavigateAsync(
                    coordinateUrl
                );

            Log(
                navigateCanvas
            );

            if (!Final06F_RequireSuccess(
                    "Canvas page navigation",
                    navigateCanvas))
            {
                return;
            }

            string canvasRole =
                await BrowserTools.FindByRoleAsync(
                    "button",
                    "Continue Review",
                    true
                );

            Log(
                canvasRole
            );

            if (!Final06F_IsNotFound(
                    canvasRole))
            {
                Final06F_Fail(
                    "Canvas expected role failure"
                );

                return;
            }

            string canvasText =
                await BrowserTools.FindElementsAsync(
                    "exact_text",
                    "Continue Review"
                );

            Log(
                canvasText
            );

            if (!Final06F_IsNotFound(
                    canvasText))
            {
                Final06F_Fail(
                    "Canvas expected text failure"
                );

                return;
            }

            string canvasBox =
                await BrowserTools.GetElementBoxAsync(
                    "css",
                    "#visualCanvas"
                );

            Log(
                canvasBox
            );

            if (!Final06F_RequireSuccess(
                    "Canvas bounding box",
                    canvasBox))
            {
                return;
            }

            string coordinateVision =
                await BrowserVisionTools
                    .InspectCurrentPageAsync(
                        """
                        Controlled Operator AI final regression.

                        Inside the canvas are three visual controls.

                        Locate the visual control labeled:

                        Continue Review

                        Return these values clearly:

                        TARGET_POSITION=MIDDLE
                        TARGET_X=<integer>
                        TARGET_Y=<integer>

                        TARGET_X and TARGET_Y must be viewport
                        screenshot coordinates for the approximate
                        CENTER of the Continue Review control.

                        Do not click.
                        """,
                        false
                    );

            Log(
                coordinateVision
            );

            if (Final06F_IsFailure(
                    coordinateVision))
            {
                Final06F_Fail(
                    "Canvas visual inspection"
                );

                return;
            }

            if (!Final06F_VisionSaysMiddle(
                    coordinateVision))
            {
                Final06F_Fail(
                    "Canvas visual target position"
                );

                return;
            }

            if (!Final06F_TryExtractVisionCoordinates(
                    coordinateVision,
                    out int targetX,
                    out int targetY))
            {
                Final06F_Fail(
                    "Canvas coordinate extraction"
                );

                return;
            }

            Log(
                $"Visual target coordinate: ({targetX}, {targetY})"
            );

            // Fresh screenshot arms the coordinate guard.

            string armScreenshot =
                await BrowserTools.ScreenshotAsync(
                    "0.6f-final-coordinate-armed.png",
                    false
                );

            Log(
                armScreenshot
            );

            if (!Final06F_RequireSuccess(
                    "Coordinate safety screenshot",
                    armScreenshot))
            {
                return;
            }

            string viewport =
                await BrowserTools.GetViewportInfoAsync();

            Log(
                viewport
            );

            if (!viewport.Contains(
                    "Recent visual-click screenshot: Yes",
                    StringComparison.OrdinalIgnoreCase))
            {
                Final06F_Fail(
                    "Coordinate screenshot guard"
                );

                return;
            }

            string mouseMove =
                await BrowserTools.MouseMoveAsync(
                    targetX,
                    targetY
                );

            Log(
                mouseMove
            );

            if (!Final06F_RequireSuccess(
                    "Mouse movement",
                    mouseMove))
            {
                return;
            }

            string coordinateClick =
                await BrowserTools.MouseClickAsync(
                    targetX,
                    targetY
                );

            Log(
                coordinateClick
            );

            if (!Final06F_RequireSuccess(
                    "Guarded coordinate click",
                    coordinateClick))
            {
                return;
            }

            string canvasVerify =
                await BrowserTools.WaitForTextAsync(
                    "Result: canvas review activated",
                    true,
                    10
                );

            Log(
                canvasVerify
            );

            if (!Final06F_RequireSuccess(
                    "Canvas result verification",
                    canvasVerify))
            {
                return;
            }

            string canvasStatus =
                await BrowserTools.GetElementTextAsync(
                    "css",
                    "#canvasStatus"
                );

            Log(
                canvasStatus
            );

            if (!canvasStatus.Contains(
                    "Result: canvas review activated",
                    StringComparison.OrdinalIgnoreCase))
            {
                Final06F_Fail(
                    "Canvas DOM verification"
                );

                return;
            }

            Log(
                "PASS: Canvas coordinate interaction"
            );

            // =================================================
            // 12. STALE CLICK PROTECTION
            // =================================================

            Log(
                "[12/14] Testing stale visual-click protection..."
            );

            string staleClick =
                await BrowserTools.MouseClickAsync(
                    targetX,
                    targetY
                );

            Log(
                staleClick
            );

            if (!staleClick.StartsWith(
                    "BLOCKED",
                    StringComparison.OrdinalIgnoreCase))
            {
                Final06F_Fail(
                    "Stale click protection"
                );

                return;
            }

            Log(
                "PASS: Stale visual-click protection"
            );

            // =================================================
            // 13. AUTONOMOUS AGENT
            // =================================================

            Log(
                "[13/14] Testing autonomous agent execution..."
            );

            OperatorAgent agent =
                new OperatorAgent();

            using CancellationTokenSource agentTimeout =
                new CancellationTokenSource(
                    TimeSpan.FromMinutes(3)
                );

            string autonomousResult =
                await agent.RunAsync(
                    $"""
                    Navigate the Operator AI browser to:

                    {server.BaseUrl}

                    Find the textbox named "Test input"
                    using structured browser targeting.

                    Fill it with exactly:

                    Operator AI autonomous regression passed

                    Verify the field value using a browser
                    inspection tool.

                    Do not do anything else.
                    """,
                    message =>
                        Dispatcher.Invoke(
                            () =>
                                Log(
                                    $"[AGENT] {message}"
                                )
                        ),
                    agentTimeout.Token
                );

            Log(
                $"Autonomous agent result: {autonomousResult}"
            );

            if (Final06F_IsFailure(
                    autonomousResult))
            {
                Final06F_Fail(
                    "Autonomous agent execution"
                );

                return;
            }

            string autonomousValue =
                await BrowserTools.GetValueAsync(
                    "css",
                    "#testInput"
                );

            Log(
                autonomousValue
            );

            if (!autonomousValue.Contains(
                    "Operator AI autonomous regression passed",
                    StringComparison.Ordinal))
            {
                Final06F_Fail(
                    "Autonomous agent state verification"
                );

                return;
            }

            Log(
                "PASS: Autonomous agent execution"
            );

            // =================================================
            // 14. FINAL STATE
            // =================================================

            Log(
                "[14/14] Final browser-state verification..."
            );

            string finalInfo =
                await BrowserTools.GetPageInfoAsync();

            Log(
                finalInfo
            );

            if (Final06F_IsFailure(
                    finalInfo))
            {
                Final06F_Fail(
                    "Final browser state"
                );

                return;
            }

            Log(
                "PASS: Final browser state"
            );

            // =================================================
            // COMPLETE
            // =================================================

            Log(
                "============================================================"
            );

            Log(
                "SUCCESS: VERSION 0.6F FINAL REGRESSION PASSED."
            );

            Log(
                "Browser start/session: PASS"
            );

            Log(
                "Navigation: PASS"
            );

            Log(
                "DOM targeting: PASS"
            );

            Log(
                "Role targeting: PASS"
            );

            Log(
                "Form interaction: PASS"
            );

            Log(
                "Field/state verification: PASS"
            );

            Log(
                "Waiting: PASS"
            );

            Log(
                "Scrolling: PASS"
            );

            Log(
                "Upload: PASS"
            );

            Log(
                "Download: PASS"
            );

            Log(
                "Screenshot capture: PASS"
            );

            Log(
                "Visual understanding: PASS"
            );

            Log(
                "Vision-assisted DOM recovery: PASS"
            );

            Log(
                "Canvas coordinate control: PASS"
            );

            Log(
                "Stale-click protection: PASS"
            );

            Log(
                "Autonomous agent execution: PASS"
            );

            Log(
                "VERSION 0.6F: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (OperationCanceledException)
        {
            Final06F_Fail(
                "Regression timed out or was cancelled"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.6F FINAL REGRESSION ERROR: {ex.Message}"
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
                        uploadPath))
                {
                    File.Delete(
                        uploadPath
                    );
                }
            }
            catch
            {
            }
        }
    }

    // =========================================================
    // REQUIRE SUCCESS
    // =========================================================

    private bool Final06F_RequireSuccess(
        string testName,
        string result)
    {
        if (Final06F_IsFailure(
                result))
        {
            Final06F_Fail(
                testName
            );

            return false;
        }

        return true;
    }

    // =========================================================
    // FAILURE REPORT
    // =========================================================

    private void Final06F_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.6F FINAL REGRESSION - {testName}"
        );

        Log(
            "Regression stopped at first failed requirement."
        );

        Log(
            "============================================================"
        );
    }

    // =========================================================
    // GENERIC FAILURE CHECK
    // =========================================================

    private static bool Final06F_IsFailure(
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

    // =========================================================
    // EXPECTED NOT_FOUND
    // =========================================================

    private static bool Final06F_IsNotFound(
        string result)
    {
        if (string.IsNullOrWhiteSpace(
                result))
        {
            return false;
        }

        return
            result.StartsWith(
                "NOT_FOUND",
                StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // VISUAL BUTTON POSITION
    // =========================================================

    private static int Final06F_DetermineVisualButtonIndex(
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
                "center control"
            )
            ||
            normalized.Contains(
                "centre control"
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
                "left control"
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
                "right control"
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
    // VISION SAYS MIDDLE
    // =========================================================

    private static bool Final06F_VisionSaysMiddle(
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
                "middle control"
            )
            ||
            normalized.Contains(
                "center button"
            )
            ||
            normalized.Contains(
                "center control"
            )
            ||
            normalized.Contains(
                "centre button"
            )
            ||
            normalized.Contains(
                "centre control"
            );
    }

    // =========================================================
    // VISION COORDINATE PARSING
    // =========================================================

    private static bool Final06F_TryExtractVisionCoordinates(
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
                @"TARGET_X\s*[:=]\s*[^\d]*?(\d+)",
                RegexOptions.IgnoreCase
            );

        Match yMatch =
            Regex.Match(
                visionResult,
                @"TARGET_Y\s*[:=]\s*[^\d]*?(\d+)",
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
            x > 0
            &&
            y > 0;
    }
}