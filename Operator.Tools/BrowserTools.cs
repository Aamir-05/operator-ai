using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Operator.Tools;

public static class BrowserTools
{
    // =========================================================
    // BROWSER STATE
    // =========================================================

    private static IPlaywright? _playwright;
    private static IBrowserContext? _context;
    private static IPage? _page;

    // =========================================================
    // VERSION 0.6F-4
    // VISUAL CLICK SAFETY STATE
    // =========================================================

    private const int VisualScreenshotMaximumAgeSeconds = 20;

    private sealed record ViewportState(
        int Width,
        int Height,
        double ScrollX,
        double ScrollY,
        string Url
    );

    private sealed record VisualScreenshotState(
        DateTimeOffset CapturedUtc,
        string ScreenshotPath,
        int Width,
        int Height,
        double ScrollX,
        double ScrollY,
        string Url
    );

    private static VisualScreenshotState?
        _latestViewportScreenshot;

    // =========================================================
    // PATHS
    // =========================================================

    private static readonly string PersistentProfileDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ),
            "OperatorAI",
            "BrowserProfile"
        );

    private static readonly string DownloadsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory
            ),
            "OperatorDownloads"
        );

    private static readonly string ScreenshotsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory
            ),
            "OperatorScreenshots"
        );

    // =========================================================
    // PUBLIC PATH HELPERS
    // =========================================================

    public static string GetPersistentProfilePath()
    {
        return PersistentProfileDirectory;
    }

    public static string GetDownloadsDirectory()
    {
        return DownloadsDirectory;
    }

    public static string GetScreenshotsDirectory()
    {
        return ScreenshotsDirectory;
    }

    // =========================================================
    // START BROWSER
    // =========================================================

    public static async Task<string> StartBrowserAsync()
    {
        try
        {
            if (IsBrowserRunning())
            {
                await EnsureCurrentPageAsync();

                return
                    "SUCCESS: Persistent Operator AI browser is already running.";
            }

            _page = null;
            _context = null;
            _latestViewportScreenshot = null;

            if (_playwright != null)
            {
                try
                {
                    _playwright.Dispose();
                }
                catch
                {
                }

                _playwright = null;
            }

            Directory.CreateDirectory(
                PersistentProfileDirectory
            );

            Directory.CreateDirectory(
                DownloadsDirectory
            );

            Directory.CreateDirectory(
                ScreenshotsDirectory
            );

            _playwright =
                await Playwright.CreateAsync();

            _context =
                await _playwright
                    .Chromium
                    .LaunchPersistentContextAsync(
                        PersistentProfileDirectory,
                        new BrowserTypeLaunchPersistentContextOptions
                        {
                            Headless = false,
                            SlowMo = 80,
                            AcceptDownloads = true,
                            Timeout = 30000,

                            ViewportSize =
                                new ViewportSize
                                {
                                    Width = 1400,
                                    Height = 900
                                },

                            DeviceScaleFactor = 1
                        }
                    );

            if (_context.Pages.Count > 0)
            {
                _page =
                    _context.Pages[
                        _context.Pages.Count - 1
                    ];
            }
            else
            {
                _page =
                    await _context.NewPageAsync();
            }

            ConfigurePage(
                _page
            );

            await _page.BringToFrontAsync();

            return
                "SUCCESS: Chromium browser started with persistent Operator AI profile.\n" +
                $"Profile: {PersistentProfileDirectory}\n" +
                $"Downloads: {DownloadsDirectory}\n" +
                $"Screenshots: {ScreenshotsDirectory}";
        }
        catch (Exception ex)
        {
            return
                "ERROR: Could not start persistent browser. " +
                $"{ex.Message}\n" +
                "Close any existing Operator AI Chromium window and try again.";
        }
    }

    // =========================================================
    // SESSION INFO
    // =========================================================

    public static async Task<string> GetSessionInfoAsync()
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "BROWSER SESSION\n" +
                    "Status: Not running\n" +
                    $"Persistent profile: {PersistentProfileDirectory}\n" +
                    $"Downloads: {DownloadsDirectory}\n" +
                    $"Screenshots: {ScreenshotsDirectory}";
            }

            await EnsureCurrentPageAsync();

            string title = "";

            try
            {
                title =
                    await _page!.TitleAsync();
            }
            catch
            {
            }

            return
                "BROWSER SESSION\n" +
                "Status: Running\n" +
                $"Persistent profile: {PersistentProfileDirectory}\n" +
                $"Downloads: {DownloadsDirectory}\n" +
                $"Screenshots: {ScreenshotsDirectory}\n" +
                $"Open tabs: {_context!.Pages.Count}\n" +
                $"Current title: {title}\n" +
                $"Current URL: {_page!.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read browser session information: {ex.Message}";
        }
    }

    // =========================================================
    // NAVIGATE
    // =========================================================

    public static async Task<string> NavigateAsync(
        string url)
    {
        try
        {
            string ready =
                await EnsureBrowserAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            if (string.IsNullOrWhiteSpace(
                    url))
            {
                return
                    "ERROR: URL cannot be empty.";
            }

            await EnsureCurrentPageAsync();

            url =
                NormalizeUrl(
                    url
                );

            InvalidateVisualClickGuard();

            await _page!.GotoAsync(
                url,
                new PageGotoOptions
                {
                    WaitUntil =
                        WaitUntilState.DOMContentLoaded,

                    Timeout =
                        30000
                }
            );

            return
                $"SUCCESS: Navigated to {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Navigation failed: {ex.Message}";
        }
    }

    // =========================================================
    // PAGE INFORMATION
    // =========================================================

    public static async Task<string> GetPageInfoAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            string title =
                await _page!.TitleAsync();

            return
                "PAGE INFORMATION\n" +
                $"Title: {title}\n" +
                $"URL: {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read page information: {ex.Message}";
        }
    }

    // =========================================================
    // READ PAGE
    // =========================================================

    public static async Task<string> ReadPageTextAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            string text =
                await _page!
                    .Locator(
                        "body"
                    )
                    .InnerTextAsync();

            if (text.Length > 15000)
            {
                text =
                    text.Substring(
                        0,
                        15000
                    )
                    +
                    "\n\n[Page text truncated]";
            }

            return
                $"PAGE TEXT\n{text}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read page text: {ex.Message}";
        }
    }

    // =========================================================
    // SCREENSHOT
    //
    // IMPORTANT:
    // ScreenshotScale.Css makes screenshot pixels correspond
    // to CSS viewport pixels.
    // =========================================================

    public static async Task<string> ScreenshotAsync(
        string relativePath,
        bool fullPage)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            Directory.CreateDirectory(
                ScreenshotsDirectory
            );

            if (string.IsNullOrWhiteSpace(
                    relativePath))
            {
                relativePath =
                    $"browser-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            }

            if (
                !relativePath.EndsWith(
                    ".png",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !relativePath.EndsWith(
                    ".jpg",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !relativePath.EndsWith(
                    ".jpeg",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !relativePath.EndsWith(
                    ".webp",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                relativePath +=
                    ".png";
            }

            string screenshotPath =
                ResolvePathInsideRoot(
                    ScreenshotsDirectory,
                    relativePath
                );

            string? parent =
                Path.GetDirectoryName(
                    screenshotPath
                );

            if (!string.IsNullOrWhiteSpace(
                    parent))
            {
                Directory.CreateDirectory(
                    parent
                );
            }

            ViewportState before =
                await GetViewportStateAsync();

            await _page!
                .ScreenshotAsync(
                    new PageScreenshotOptions
                    {
                        Path =
                            screenshotPath,

                        FullPage =
                            fullPage,

                        Scale =
                            ScreenshotScale.Css
                    }
                );

            if (!File.Exists(
                    screenshotPath))
            {
                return
                    $"ERROR: Screenshot was not found after capture: {screenshotPath}";
            }

            long size =
                new FileInfo(
                    screenshotPath
                ).Length;

            if (!fullPage)
            {
                ViewportState after =
                    await GetViewportStateAsync();

                if (
                    before.Url.Equals(
                        after.Url,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    before.Width ==
                        after.Width
                    &&
                    before.Height ==
                        after.Height
                    &&
                    NearlyEqual(
                        before.ScrollX,
                        after.ScrollX)
                    &&
                    NearlyEqual(
                        before.ScrollY,
                        after.ScrollY)
                )
                {
                    _latestViewportScreenshot =
                        new VisualScreenshotState(
                            DateTimeOffset.UtcNow,
                            screenshotPath,
                            after.Width,
                            after.Height,
                            after.ScrollX,
                            after.ScrollY,
                            after.Url
                        );
                }
                else
                {
                    _latestViewportScreenshot =
                        null;
                }
            }
            else
            {
                // A full-page screenshot does not map directly
                // to viewport mouse coordinates.
                _latestViewportScreenshot =
                    null;
            }

            return
                "SUCCESS: Browser screenshot captured.\n" +
                $"File: {screenshotPath}\n" +
                $"Full page: {fullPage}\n" +
                $"Scale: CSS pixels\n" +
                $"Size: {size} bytes";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser screenshot failed: {ex.Message}";
        }
    }

    // =========================================================
    // LIST SCREENSHOTS
    // =========================================================

    public static string ListScreenshots()
    {
        try
        {
            Directory.CreateDirectory(
                ScreenshotsDirectory
            );

            string[] files =
                Directory.GetFiles(
                    ScreenshotsDirectory,
                    "*",
                    SearchOption.AllDirectories
                );

            if (files.Length == 0)
            {
                return
                    "SCREENSHOTS\n" +
                    $"Directory: {ScreenshotsDirectory}\n" +
                    "No screenshots found.";
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "SCREENSHOTS"
            );

            result.AppendLine(
                $"Directory: {ScreenshotsDirectory}"
            );

            for (int i = 0;
                 i < files.Length;
                 i++)
            {
                FileInfo info =
                    new FileInfo(
                        files[i]
                    );

                string relative =
                    Path.GetRelativePath(
                        ScreenshotsDirectory,
                        files[i]
                    );

                result.AppendLine(
                    $"[{i + 1}] {relative} ({info.Length} bytes)"
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not list screenshots: {ex.Message}";
        }
    }

    // =========================================================
    // VERSION 0.6F-4
    // VIEWPORT INFORMATION
    // =========================================================

    public static async Task<string> GetViewportInfoAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ViewportState state =
                await GetViewportStateAsync();

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "BROWSER VIEWPORT"
            );

            result.AppendLine(
                $"Width: {state.Width}"
            );

            result.AppendLine(
                $"Height: {state.Height}"
            );

            result.AppendLine(
                $"Scroll X: {state.ScrollX:0.##}"
            );

            result.AppendLine(
                $"Scroll Y: {state.ScrollY:0.##}"
            );

            result.AppendLine(
                $"URL: {state.Url}"
            );

            if (_latestViewportScreenshot == null)
            {
                result.AppendLine(
                    "Recent visual-click screenshot: No"
                );
            }
            else
            {
                double ageSeconds =
                    (
                        DateTimeOffset.UtcNow
                        -
                        _latestViewportScreenshot.CapturedUtc
                    )
                    .TotalSeconds;

                result.AppendLine(
                    "Recent visual-click screenshot: Yes"
                );

                result.AppendLine(
                    $"Screenshot age: {ageSeconds:0.0} seconds"
                );

                result.AppendLine(
                    $"Screenshot: {_latestViewportScreenshot.ScreenshotPath}"
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read browser viewport: {ex.Message}";
        }
    }

    // =========================================================
    // VERSION 0.6F-4
    // MOUSE MOVE
    // =========================================================

    public static async Task<string> MouseMoveAsync(
        int x,
        int y)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ViewportState state =
                await GetViewportStateAsync();

            string validation =
                ValidateCoordinate(
                    x,
                    y,
                    state
                );

            if (IsError(
                    validation))
            {
                return validation;
            }

            await _page!
                .Mouse
                .MoveAsync(
                    x,
                    y
                );

            return
                $"SUCCESS: Moved browser mouse to viewport coordinate ({x}, {y}).";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser mouse move failed: {ex.Message}";
        }
    }

    // =========================================================
    // VERSION 0.6F-4
    // SAFE COORDINATE CLICK
    // =========================================================

    public static async Task<string> MouseClickAsync(
        int x,
        int y)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            string guard =
                await ValidateVisualClickGuardAsync(
                    x,
                    y
                );

            if (IsError(
                    guard))
            {
                return guard;
            }

            await _page!
                .Mouse
                .ClickAsync(
                    x,
                    y
                );

            InvalidateVisualClickGuard();

            return
                "SUCCESS: Visual coordinate click performed.\n" +
                $"Coordinate: ({x}, {y})";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser coordinate click failed: {ex.Message}";
        }
    }

    // =========================================================
    // VERSION 0.6F-4
    // SAFE DOUBLE CLICK
    // =========================================================

    public static async Task<string> MouseDoubleClickAsync(
        int x,
        int y)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            string guard =
                await ValidateVisualClickGuardAsync(
                    x,
                    y
                );

            if (IsError(
                    guard))
            {
                return guard;
            }

            await _page!
                .Mouse
                .DblClickAsync(
                    x,
                    y
                );

            InvalidateVisualClickGuard();

            return
                "SUCCESS: Visual coordinate double-click performed.\n" +
                $"Coordinate: ({x}, {y})";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser coordinate double-click failed: {ex.Message}";
        }
    }

    // =========================================================
    // VERSION 0.6F-4
    // ELEMENT BOUNDING BOX
    // =========================================================

    public static async Task<string> GetElementBoxAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            int count =
                await locator.CountAsync();

            if (count == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser element {locatorType}='{query}'.";
            }

            LocatorBoundingBoxResult? box =
                await locator
                    .First
                    .BoundingBoxAsync();

            if (box == null)
            {
                return
                    $"NOT_FOUND: Browser element {locatorType}='{query}' is not currently visible.";
            }

            double centerX =
                box.X +
                box.Width / 2.0;

            double centerY =
                box.Y +
                box.Height / 2.0;

            return
                "SUCCESS: Browser element bounding box.\n" +
                $"Locator: {locatorType}='{query}'\n" +
                $"X: {box.X:0.##}\n" +
                $"Y: {box.Y:0.##}\n" +
                $"Width: {box.Width:0.##}\n" +
                $"Height: {box.Height:0.##}\n" +
                $"Center X: {centerX:0.##}\n" +
                $"Center Y: {centerY:0.##}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read browser element bounding box: {ex.Message}";
        }
    }

    // =========================================================
    // LIST LINKS
    // =========================================================

    public static async Task<string> ListLinksAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator links =
                _page!.Locator(
                    "a"
                );

            int count =
                await links.CountAsync();

            int maximum =
                Math.Min(
                    count,
                    60
                );

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "PAGE LINKS"
            );

            for (int i = 0;
                 i < maximum;
                 i++)
            {
                try
                {
                    ILocator link =
                        links.Nth(
                            i
                        );

                    string text = "";

                    try
                    {
                        text =
                            (
                                await link.InnerTextAsync()
                            )
                            .Trim();
                    }
                    catch
                    {
                    }

                    string? href =
                        await link.GetAttributeAsync(
                            "href"
                        );

                    if (
                        string.IsNullOrWhiteSpace(
                            text)
                        &&
                        string.IsNullOrWhiteSpace(
                            href)
                    )
                    {
                        continue;
                    }

                    result.AppendLine(
                        $"[{i + 1}] Text=\"{text}\" Href=\"{href}\""
                    );
                }
                catch
                {
                }
            }

            if (count > maximum)
            {
                result.AppendLine(
                    $"... limited to {maximum} of {count} links."
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not list links: {ex.Message}";
        }
    }

    // =========================================================
    // LIST INTERACTIVE ELEMENTS
    // =========================================================

    public static async Task<string> ListInteractiveElementsAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator elements =
                _page!.Locator(
                    "a, button, input, textarea, select, [role]"
                );

            int count =
                await elements.CountAsync();

            int maximum =
                Math.Min(
                    count,
                    120
                );

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "INTERACTIVE PAGE ELEMENTS"
            );

            for (int i = 0;
                 i < maximum;
                 i++)
            {
                try
                {
                    ILocator element =
                        elements.Nth(
                            i
                        );

                    string tag = "";

                    try
                    {
                        tag =
                            await element
                                .EvaluateAsync<string>(
                                    "el => el.tagName.toLowerCase()"
                                );
                    }
                    catch
                    {
                    }

                    string text = "";

                    try
                    {
                        text =
                            (
                                await element.InnerTextAsync()
                            )
                            .Trim();
                    }
                    catch
                    {
                    }

                    string? id =
                        await element.GetAttributeAsync(
                            "id"
                        );

                    string? name =
                        await element.GetAttributeAsync(
                            "name"
                        );

                    string? type =
                        await element.GetAttributeAsync(
                            "type"
                        );

                    string? role =
                        await element.GetAttributeAsync(
                            "role"
                        );

                    string? placeholder =
                        await element.GetAttributeAsync(
                            "placeholder"
                        );

                    string? ariaLabel =
                        await element.GetAttributeAsync(
                            "aria-label"
                        );

                    string? href =
                        await element.GetAttributeAsync(
                            "href"
                        );

                    bool visible = false;

                    try
                    {
                        visible =
                            await element.IsVisibleAsync();
                    }
                    catch
                    {
                    }

                    result.AppendLine(
                        $"[{i + 1}] " +
                        $"Tag={tag}, " +
                        $"Role=\"{role}\", " +
                        $"Text=\"{text}\", " +
                        $"Id=\"{id}\", " +
                        $"Name=\"{name}\", " +
                        $"Type=\"{type}\", " +
                        $"Placeholder=\"{placeholder}\", " +
                        $"AriaLabel=\"{ariaLabel}\", " +
                        $"Href=\"{href}\", " +
                        $"Visible={visible}"
                    );
                }
                catch
                {
                }
            }

            if (count > maximum)
            {
                result.AppendLine(
                    $"... limited to {maximum} of {count} elements."
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not inspect interactive elements: {ex.Message}";
        }
    }

    // =========================================================
    // FIND
    // =========================================================

    public static async Task<string> FindElementsAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            return
                await DescribeLocatorMatchesAsync(
                    locator,
                    $"{locatorType}='{query}'"
                );
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Element search failed: {ex.Message}";
        }
    }

    // =========================================================
    // FIND BY ROLE
    // =========================================================

    public static async Task<string> FindByRoleAsync(
        string role,
        string accessibleName,
        bool exact)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveRoleLocator(
                    role,
                    accessibleName,
                    exact
                );

            return
                await DescribeLocatorMatchesAsync(
                    locator,
                    $"role='{role}', name='{accessibleName}', exact={exact}"
                );
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Role element search failed: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT FOR ELEMENT
    // =========================================================

    public static async Task<string> WaitForElementAsync(
        string locatorType,
        string query,
        string state,
        int timeoutSeconds)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            WaitForSelectorState waitState =
                ParseWaitState(
                    state
                );

            int timeout =
                Math.Clamp(
                    timeoutSeconds,
                    1,
                    120
                );

            await locator.WaitForAsync(
                new LocatorWaitForOptions
                {
                    State =
                        waitState,

                    Timeout =
                        timeout * 1000
                }
            );

            return
                $"SUCCESS: Browser element {locatorType}='{query}' reached state '{state}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for browser element failed: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT BY ROLE
    // =========================================================

    public static async Task<string> WaitForRoleAsync(
        string role,
        string accessibleName,
        bool exact,
        string state,
        int timeoutSeconds)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveRoleLocator(
                    role,
                    accessibleName,
                    exact
                );

            int timeout =
                Math.Clamp(
                    timeoutSeconds,
                    1,
                    120
                );

            await locator.WaitForAsync(
                new LocatorWaitForOptions
                {
                    State =
                        ParseWaitState(
                            state
                        ),

                    Timeout =
                        timeout * 1000
                }
            );

            return
                $"SUCCESS: Role '{role}' named '{accessibleName}' reached state '{state}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for browser role failed: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT FOR URL
    // =========================================================

    public static async Task<string> WaitForUrlAsync(
        string urlPattern,
        int timeoutSeconds)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            if (string.IsNullOrWhiteSpace(
                    urlPattern))
            {
                return
                    "ERROR: URL pattern cannot be empty.";
            }

            int timeout =
                Math.Clamp(
                    timeoutSeconds,
                    1,
                    120
                );

            await _page!.WaitForURLAsync(
                urlPattern,
                new PageWaitForURLOptions
                {
                    Timeout =
                        timeout * 1000
                }
            );

            return
                $"SUCCESS: Browser URL matched '{urlPattern}'. Current URL: {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for browser URL failed: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT FOR TEXT
    // =========================================================

    public static async Task<string> WaitForTextAsync(
        string text,
        bool exact,
        int timeoutSeconds)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return
                    "ERROR: Text cannot be empty.";
            }

            int timeout =
                Math.Clamp(
                    timeoutSeconds,
                    1,
                    120
                );

            ILocator locator =
                _page!.GetByText(
                    text,
                    new PageGetByTextOptions
                    {
                        Exact =
                            exact
                    }
                );

            await locator.First.WaitForAsync(
                new LocatorWaitForOptions
                {
                    State =
                        WaitForSelectorState.Visible,

                    Timeout =
                        timeout * 1000
                }
            );

            return
                $"SUCCESS: Browser text '{text}' became visible.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for browser text failed: {ex.Message}";
        }
    }

    // =========================================================
    // CLICK
    // =========================================================

    public static async Task<string> ClickAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser element {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First.ClickAsync();

            return
                $"SUCCESS: Clicked browser element {locatorType}='{query}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser click failed: {ex.Message}";
        }
    }

    // =========================================================
    // ROLE CLICK
    // =========================================================

    public static async Task<string> ClickRoleAsync(
        string role,
        string accessibleName,
        bool exact)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveRoleLocator(
                    role,
                    accessibleName,
                    exact
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find role '{role}' named '{accessibleName}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First.ClickAsync();

            return
                $"SUCCESS: Clicked role '{role}' named '{accessibleName}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser role click failed: {ex.Message}";
        }
    }

    // =========================================================
    // FILL
    // =========================================================

    public static async Task<string> FillAsync(
        string locatorType,
        string query,
        string text)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser field {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First.FillAsync(
                text
            );

            return
                $"SUCCESS: Filled browser field {locatorType}='{query}' with {text.Length} characters.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser fill failed: {ex.Message}";
        }
    }

    // =========================================================
    // ROLE FILL
    // =========================================================

    public static async Task<string> FillRoleAsync(
        string role,
        string accessibleName,
        bool exact,
        string text)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveRoleLocator(
                    role,
                    accessibleName,
                    exact
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find role '{role}' named '{accessibleName}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First.FillAsync(
                text
            );

            return
                $"SUCCESS: Filled role '{role}' named '{accessibleName}' with {text.Length} characters.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser role fill failed: {ex.Message}";
        }
    }

    // =========================================================
    // TYPE
    // =========================================================

    public static async Task<string> TypeAsync(
        string locatorType,
        string query,
        string text)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser field {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First
                .PressSequentiallyAsync(
                    text,
                    new LocatorPressSequentiallyOptions
                    {
                        Delay = 40
                    }
                );

            return
                $"SUCCESS: Typed {text.Length} characters into {locatorType}='{query}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser typing failed: {ex.Message}";
        }
    }

    // =========================================================
    // PRESS ON ELEMENT
    // =========================================================

    public static async Task<string> PressAsync(
        string locatorType,
        string query,
        string key)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser element {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First.PressAsync(
                key
            );

            return
                $"SUCCESS: Pressed '{key}' on {locatorType}='{query}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser key press failed: {ex.Message}";
        }
    }

    // =========================================================
    // PAGE KEY
    // =========================================================

    public static async Task<string> PressPageKeyAsync(
        string key)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            InvalidateVisualClickGuard();

            await _page!.Keyboard.PressAsync(
                key
            );

            return
                $"SUCCESS: Pressed browser key '{key}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser key press failed: {ex.Message}";
        }
    }

    // =========================================================
    // GET ELEMENT TEXT
    // =========================================================

    public static async Task<string> GetElementTextAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser element {locatorType}='{query}'.";
            }

            string text;

            try
            {
                text =
                    await locator.First
                        .InnerTextAsync();
            }
            catch
            {
                text =
                    await locator.First
                        .TextContentAsync()
                    ?? "";
            }

            return
                "SUCCESS: Element text read.\n" +
                $"Locator: {locatorType}='{query}'\n" +
                $"Text: {text}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read browser element text: {ex.Message}";
        }
    }

    // =========================================================
    // GET ROLE TEXT
    // =========================================================

    public static async Task<string> GetRoleTextAsync(
        string role,
        string accessibleName,
        bool exact)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveRoleLocator(
                    role,
                    accessibleName,
                    exact
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find role '{role}' named '{accessibleName}'.";
            }

            string text;

            try
            {
                text =
                    await locator.First
                        .InnerTextAsync();
            }
            catch
            {
                text =
                    await locator.First
                        .TextContentAsync()
                    ?? "";
            }

            return
                "SUCCESS: Role element text read.\n" +
                $"Role: {role}\n" +
                $"Name: {accessibleName}\n" +
                $"Text: {text}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read role element text: {ex.Message}";
        }
    }

    // =========================================================
    // GET ATTRIBUTE
    // =========================================================

    public static async Task<string> GetAttributeAsync(
        string locatorType,
        string query,
        string attributeName)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            if (string.IsNullOrWhiteSpace(
                    attributeName))
            {
                return
                    "ERROR: Attribute name cannot be empty.";
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser element {locatorType}='{query}'.";
            }

            string? value =
                await locator.First
                    .GetAttributeAsync(
                        attributeName
                    );

            return
                "SUCCESS: Attribute read.\n" +
                $"Attribute: {attributeName}\n" +
                $"Value: {value ?? "[null]"}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read browser attribute: {ex.Message}";
        }
    }

    // =========================================================
    // GET VALUE
    // =========================================================

    public static async Task<string> GetValueAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser field {locatorType}='{query}'.";
            }

            string value =
                await locator.First
                    .InputValueAsync();

            return
                "SUCCESS: Browser field value read.\n" +
                $"Value: {value}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read browser field value: {ex.Message}";
        }
    }

    // =========================================================
    // IS VISIBLE
    // =========================================================

    public static async Task<string> IsVisibleAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            int count =
                await locator.CountAsync();

            if (count == 0)
            {
                return
                    $"SUCCESS: Visible=False. No element matched {locatorType}='{query}'.";
            }

            bool visible =
                await locator.First
                    .IsVisibleAsync();

            return
                $"SUCCESS: Visible={visible}. Locator {locatorType}='{query}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser visibility check failed: {ex.Message}";
        }
    }

    // =========================================================
    // SCROLL PAGE
    // =========================================================

    public static async Task<string> ScrollPageAsync(
        int deltaY)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            int safeDelta =
                Math.Clamp(
                    deltaY,
                    -10000,
                    10000
                );

            InvalidateVisualClickGuard();

            await _page!.Mouse.WheelAsync(
                0,
                safeDelta
            );

            return
                $"SUCCESS: Scrolled browser page vertically by {safeDelta} pixels.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser page scroll failed: {ex.Message}";
        }
    }

    // =========================================================
    // SCROLL ELEMENT INTO VIEW
    // =========================================================

    public static async Task<string> ScrollToElementAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find browser element {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First
                .ScrollIntoViewIfNeededAsync();

            return
                $"SUCCESS: Scrolled browser element {locatorType}='{query}' into view.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser scroll-to-element failed: {ex.Message}";
        }
    }

    // =========================================================
    // CHECKBOX / RADIO
    // =========================================================

    public static async Task<string> SetCheckedAsync(
        string locatorType,
        string query,
        bool checkedState)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find checkbox/radio {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            ILocator target =
                locator.First;

            await target.SetCheckedAsync(
                checkedState
            );

            bool actual =
                await target.IsCheckedAsync();

            if (actual != checkedState)
            {
                return
                    $"ERROR: Requested checked state '{checkedState}' was not confirmed.";
            }

            return
                $"SUCCESS: Set {locatorType}='{query}' checked={actual}.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser checkbox/radio operation failed: {ex.Message}";
        }
    }

    public static async Task<string> GetCheckedStateAsync(
        string locatorType,
        string query)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find checkbox/radio {locatorType}='{query}'.";
            }

            bool state =
                await locator.First
                    .IsCheckedAsync();

            return
                $"SUCCESS: {locatorType}='{query}' checked={state}.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read checkbox/radio state: {ex.Message}";
        }
    }

    // =========================================================
    // SELECT OPTION
    // =========================================================

    public static async Task<string> SelectOptionAsync(
        string locatorType,
        string query,
        string selectionType,
        string selection)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find select element {locatorType}='{query}'.";
            }

            SelectOptionValue option =
                BuildSelectOption(
                    selectionType,
                    selection
                );

            InvalidateVisualClickGuard();

            IReadOnlyList<string> selected =
                await locator.First
                    .SelectOptionAsync(
                        new[]
                        {
                            option
                        }
                    );

            if (selected.Count == 0)
            {
                return
                    $"ERROR: Dropdown option '{selection}' was not selected.";
            }

            return
                "SUCCESS: Selected dropdown option. " +
                $"Requested {selectionType}='{selection}'. " +
                $"Selected value(s): {string.Join(", ", selected)}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser dropdown selection failed: {ex.Message}";
        }
    }

    // =========================================================
    // UPLOAD
    // =========================================================

    public static async Task<string> UploadDesktopFileAsync(
        string locatorType,
        string query,
        string relativePath)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory
                );

            string filePath =
                ResolvePathInsideRoot(
                    desktop,
                    relativePath
                );

            if (!File.Exists(
                    filePath))
            {
                return
                    $"NOT_FOUND: Upload file does not exist: {filePath}";
            }

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find file upload input {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            await locator.First
                .SetInputFilesAsync(
                    filePath
                );

            return
                $"SUCCESS: Uploaded Desktop file '{filePath}' into {locatorType}='{query}'.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser file upload failed: {ex.Message}";
        }
    }

    // =========================================================
    // DOWNLOAD
    // =========================================================

    public static async Task<string> DownloadByClickAsync(
        string locatorType,
        string query,
        string preferredRelativePath)
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            Directory.CreateDirectory(
                DownloadsDirectory
            );

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            if (await locator.CountAsync() == 0)
            {
                return
                    $"NOT_FOUND: Could not find download element {locatorType}='{query}'.";
            }

            InvalidateVisualClickGuard();

            Task<IDownload> downloadTask =
                _page!.WaitForDownloadAsync();

            await locator.First.ClickAsync();

            IDownload download =
                await downloadTask;

            string targetPath;

            if (string.IsNullOrWhiteSpace(
                    preferredRelativePath))
            {
                targetPath =
                    ResolvePathInsideRoot(
                        DownloadsDirectory,
                        Path.GetFileName(
                            download.SuggestedFilename
                        )
                    );
            }
            else
            {
                targetPath =
                    ResolvePathInsideRoot(
                        DownloadsDirectory,
                        preferredRelativePath
                    );
            }

            string? parent =
                Path.GetDirectoryName(
                    targetPath
                );

            if (!string.IsNullOrWhiteSpace(
                    parent))
            {
                Directory.CreateDirectory(
                    parent
                );
            }

            await download.SaveAsAsync(
                targetPath
            );

            if (!File.Exists(
                    targetPath))
            {
                return
                    $"ERROR: Browser reported a download but the file was not found at {targetPath}.";
            }

            long size =
                new FileInfo(
                    targetPath
                ).Length;

            return
                "SUCCESS: Download completed and verified.\n" +
                $"File: {targetPath}\n" +
                $"Size: {size} bytes";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser download failed: {ex.Message}";
        }
    }

    // =========================================================
    // LIST DOWNLOADS
    // =========================================================

    public static string ListDownloads()
    {
        try
        {
            Directory.CreateDirectory(
                DownloadsDirectory
            );

            string[] files =
                Directory.GetFiles(
                    DownloadsDirectory,
                    "*",
                    SearchOption.AllDirectories
                );

            if (files.Length == 0)
            {
                return
                    $"DOWNLOADS\nDirectory: {DownloadsDirectory}\nNo files found.";
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "DOWNLOADS"
            );

            result.AppendLine(
                $"Directory: {DownloadsDirectory}"
            );

            for (int i = 0;
                 i < files.Length;
                 i++)
            {
                FileInfo info =
                    new FileInfo(
                        files[i]
                    );

                string relative =
                    Path.GetRelativePath(
                        DownloadsDirectory,
                        files[i]
                    );

                result.AppendLine(
                    $"[{i + 1}] {relative} ({info.Length} bytes)"
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not list downloaded files: {ex.Message}";
        }
    }

    // =========================================================
    // BACK
    // =========================================================

    public static async Task<string> BackAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            InvalidateVisualClickGuard();

            await _page!.GoBackAsync(
                new PageGoBackOptions
                {
                    WaitUntil =
                        WaitUntilState.DOMContentLoaded,

                    Timeout =
                        30000
                }
            );

            return
                $"SUCCESS: Browser went back. Current URL: {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser Back failed: {ex.Message}";
        }
    }

    // =========================================================
    // FORWARD
    // =========================================================

    public static async Task<string> ForwardAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            InvalidateVisualClickGuard();

            await _page!.GoForwardAsync(
                new PageGoForwardOptions
                {
                    WaitUntil =
                        WaitUntilState.DOMContentLoaded,

                    Timeout =
                        30000
                }
            );

            return
                $"SUCCESS: Browser went forward. Current URL: {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser Forward failed: {ex.Message}";
        }
    }

    // =========================================================
    // RELOAD
    // =========================================================

    public static async Task<string> ReloadAsync()
    {
        try
        {
            string ready =
                await RequirePageAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            InvalidateVisualClickGuard();

            await _page!.ReloadAsync(
                new PageReloadOptions
                {
                    WaitUntil =
                        WaitUntilState.DOMContentLoaded,

                    Timeout =
                        30000
                }
            );

            return
                $"SUCCESS: Reloaded {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser reload failed: {ex.Message}";
        }
    }

    // =========================================================
    // NEW TAB
    // =========================================================

    public static async Task<string> NewTabAsync(
        string? url = null)
    {
        try
        {
            string ready =
                await EnsureBrowserAsync();

            if (IsError(
                    ready))
            {
                return ready;
            }

            InvalidateVisualClickGuard();

            _page =
                await _context!.NewPageAsync();

            ConfigurePage(
                _page
            );

            await _page.BringToFrontAsync();

            if (!string.IsNullOrWhiteSpace(
                    url))
            {
                url =
                    NormalizeUrl(
                        url
                    );

                await _page.GotoAsync(
                    url,
                    new PageGotoOptions
                    {
                        WaitUntil =
                            WaitUntilState.DOMContentLoaded,

                        Timeout =
                            30000
                    }
                );
            }

            return
                $"SUCCESS: Opened browser tab {_context.Pages.Count}. URL: {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not open new browser tab: {ex.Message}";
        }
    }

    // =========================================================
    // LIST TABS
    // =========================================================

    public static async Task<string> ListTabsAsync()
    {
        try
        {
            if (
                !IsBrowserRunning()
                ||
                _context == null
            )
            {
                return
                    "ERROR: Browser is not running.";
            }

            if (_context.Pages.Count == 0)
            {
                return
                    "ERROR: No browser tabs are open.";
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "BROWSER TABS"
            );

            for (int i = 0;
                 i < _context.Pages.Count;
                 i++)
            {
                IPage page =
                    _context.Pages[i];

                string title = "";

                try
                {
                    title =
                        await page.TitleAsync();
                }
                catch
                {
                }

                bool current =
                    ReferenceEquals(
                        page,
                        _page
                    );

                result.AppendLine(
                    $"[{i + 1}] " +
                    $"{(current ? "[CURRENT] " : "")}" +
                    $"Title=\"{title}\" " +
                    $"URL=\"{page.Url}\""
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not list browser tabs: {ex.Message}";
        }
    }

    // =========================================================
    // SWITCH TAB
    // =========================================================

    public static async Task<string> SwitchTabAsync(
        int tabNumber)
    {
        try
        {
            if (
                !IsBrowserRunning()
                ||
                _context == null
            )
            {
                return
                    "ERROR: Browser is not running.";
            }

            if (
                tabNumber < 1
                ||
                tabNumber > _context.Pages.Count
            )
            {
                return
                    $"ERROR: Tab {tabNumber} does not exist.";
            }

            InvalidateVisualClickGuard();

            _page =
                _context.Pages[
                    tabNumber - 1
                ];

            ConfigurePage(
                _page
            );

            await _page.BringToFrontAsync();

            string title =
                await _page.TitleAsync();

            return
                $"SUCCESS: Switched to tab {tabNumber}. " +
                $"Title: {title}. URL: {_page.Url}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not switch browser tab: {ex.Message}";
        }
    }

    // =========================================================
    // CLOSE TAB
    // =========================================================

    public static async Task<string> CloseTabAsync(
        int tabNumber)
    {
        try
        {
            if (
                !IsBrowserRunning()
                ||
                _context == null
            )
            {
                return
                    "ERROR: Browser is not running.";
            }

            if (
                tabNumber < 1
                ||
                tabNumber > _context.Pages.Count
            )
            {
                return
                    $"ERROR: Tab {tabNumber} does not exist.";
            }

            InvalidateVisualClickGuard();

            IPage tab =
                _context.Pages[
                    tabNumber - 1
                ];

            bool closingCurrent =
                ReferenceEquals(
                    tab,
                    _page
                );

            if (_context.Pages.Count == 1)
            {
                IPage replacement =
                    await _context.NewPageAsync();

                ConfigurePage(
                    replacement
                );

                _page =
                    replacement;

                await replacement.BringToFrontAsync();
            }

            await tab.CloseAsync();

            if (
                _context.Pages.Count > 0
                &&
                closingCurrent
                &&
                (
                    _page == null
                    ||
                    ReferenceEquals(
                        _page,
                        tab)
                )
            )
            {
                _page =
                    _context.Pages[
                        _context.Pages.Count - 1
                    ];

                ConfigurePage(
                    _page
                );

                await _page.BringToFrontAsync();
            }

            return
                $"SUCCESS: Closed browser tab {tabNumber}.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not close browser tab: {ex.Message}";
        }
    }

    // =========================================================
    // STOP BROWSER
    // =========================================================

    public static async Task<string> StopBrowserAsync()
    {
        try
        {
            InvalidateVisualClickGuard();

            if (_context != null)
            {
                try
                {
                    await _context.CloseAsync();
                }
                catch
                {
                }
            }

            _page = null;
            _context = null;

            if (_playwright != null)
            {
                try
                {
                    _playwright.Dispose();
                }
                catch
                {
                }

                _playwright = null;
            }

            return
                "SUCCESS: Browser closed. Persistent Operator AI session data was retained.";
        }
        catch (Exception ex)
        {
            _page = null;
            _context = null;
            _latestViewportScreenshot = null;

            if (_playwright != null)
            {
                try
                {
                    _playwright.Dispose();
                }
                catch
                {
                }

                _playwright = null;
            }

            return
                $"ERROR: Could not close browser cleanly: {ex.Message}";
        }
    }

    // =========================================================
    // SAFE VISUAL CLICK VALIDATION
    // =========================================================

    private static async Task<string> ValidateVisualClickGuardAsync(
        int x,
        int y)
    {
        if (_latestViewportScreenshot == null)
        {
            return
                "BLOCKED: Coordinate clicking requires a recent viewport screenshot. " +
                "Use browser visual inspection or capture a non-full-page screenshot first.";
        }

        if (!File.Exists(
                _latestViewportScreenshot.ScreenshotPath))
        {
            InvalidateVisualClickGuard();

            return
                "BLOCKED: The screenshot used for coordinate clicking no longer exists.";
        }

        double ageSeconds =
            (
                DateTimeOffset.UtcNow
                -
                _latestViewportScreenshot.CapturedUtc
            )
            .TotalSeconds;

        if (ageSeconds >
            VisualScreenshotMaximumAgeSeconds)
        {
            InvalidateVisualClickGuard();

            return
                $"BLOCKED: The visual screenshot is too old ({ageSeconds:0.0} seconds). " +
                "Capture a fresh viewport screenshot before clicking.";
        }

        ViewportState current =
            await GetViewportStateAsync();

        string coordinateValidation =
            ValidateCoordinate(
                x,
                y,
                current
            );

        if (IsError(
                coordinateValidation))
        {
            return
                coordinateValidation;
        }

        if (!current.Url.Equals(
                _latestViewportScreenshot.Url,
                StringComparison.OrdinalIgnoreCase))
        {
            InvalidateVisualClickGuard();

            return
                "BLOCKED: Browser URL changed after the visual screenshot. " +
                "Capture a fresh screenshot before clicking.";
        }

        if (
            current.Width !=
                _latestViewportScreenshot.Width
            ||
            current.Height !=
                _latestViewportScreenshot.Height
        )
        {
            InvalidateVisualClickGuard();

            return
                "BLOCKED: Browser viewport size changed after the visual screenshot. " +
                "Capture a fresh screenshot before clicking.";
        }

        if (
            !NearlyEqual(
                current.ScrollX,
                _latestViewportScreenshot.ScrollX)
            ||
            !NearlyEqual(
                current.ScrollY,
                _latestViewportScreenshot.ScrollY)
        )
        {
            InvalidateVisualClickGuard();

            return
                "BLOCKED: Browser scroll position changed after the visual screenshot. " +
                "Capture a fresh screenshot before clicking.";
        }

        return
            "SUCCESS";
    }

    // =========================================================
    // COORDINATE VALIDATION
    // =========================================================

    private static string ValidateCoordinate(
        int x,
        int y,
        ViewportState state)
    {
        if (x < 0 ||
            y < 0)
        {
            return
                "ERROR: Browser coordinates cannot be negative.";
        }

        if (
            x >= state.Width
            ||
            y >= state.Height
        )
        {
            return
                "ERROR: Browser coordinate is outside the current viewport. " +
                $"Requested ({x}, {y}), viewport is {state.Width}x{state.Height}.";
        }

        return
            "SUCCESS";
    }

    // =========================================================
    // VIEWPORT STATE
    // =========================================================

    private static async Task<ViewportState> GetViewportStateAsync()
    {
        if (_page == null)
        {
            throw new InvalidOperationException(
                "No browser page is currently open."
            );
        }

        JsonElement state =
            await _page
                .EvaluateAsync<JsonElement>(
                    """
                    () => ({
                        width: window.innerWidth,
                        height: window.innerHeight,
                        scrollX: window.scrollX,
                        scrollY: window.scrollY
                    })
                    """
                );

        int width =
            state
                .GetProperty(
                    "width"
                )
                .GetInt32();

        int height =
            state
                .GetProperty(
                    "height"
                )
                .GetInt32();

        double scrollX =
            state
                .GetProperty(
                    "scrollX"
                )
                .GetDouble();

        double scrollY =
            state
                .GetProperty(
                    "scrollY"
                )
                .GetDouble();

        return
            new ViewportState(
                width,
                height,
                scrollX,
                scrollY,
                _page.Url
            );
    }

    // =========================================================
    // INVALIDATE VISUAL CLICK PERMISSION
    // =========================================================

    private static void InvalidateVisualClickGuard()
    {
        _latestViewportScreenshot =
            null;
    }

    // =========================================================
    // FLOAT COMPARISON
    // =========================================================

    private static bool NearlyEqual(
        double a,
        double b)
    {
        return
            Math.Abs(
                a - b
            )
            <= 1.0;
    }

    // =========================================================
    // GENERIC LOCATOR
    // =========================================================

    private static ILocator ResolveLocator(
        string locatorType,
        string query)
    {
        if (_page == null)
        {
            throw new InvalidOperationException(
                "No browser page is currently open."
            );
        }

        if (string.IsNullOrWhiteSpace(
                query))
        {
            throw new ArgumentException(
                "Locator query cannot be empty."
            );
        }

        string type =
            locatorType
                .Trim()
                .ToLowerInvariant();

        return type switch
        {
            "css" =>
                _page.Locator(
                    query
                ),

            "text" =>
                _page.GetByText(
                    query
                ),

            "exact_text" =>
                _page.GetByText(
                    query,
                    new PageGetByTextOptions
                    {
                        Exact = true
                    }
                ),

            "label" =>
                _page.GetByLabel(
                    query
                ),

            "placeholder" =>
                _page.GetByPlaceholder(
                    query
                ),

            "title" =>
                _page.GetByTitle(
                    query
                ),

            "testid" =>
                _page.GetByTestId(
                    query
                ),

            "alt" =>
                _page.GetByAltText(
                    query
                ),

            _ =>
                throw new ArgumentException(
                    $"Unsupported browser locator type '{locatorType}'. " +
                    "Supported types: css, text, exact_text, label, placeholder, title, testid, alt."
                )
        };
    }

    // =========================================================
    // ROLE LOCATOR
    // =========================================================

    private static ILocator ResolveRoleLocator(
        string role,
        string accessibleName,
        bool exact)
    {
        if (_page == null)
        {
            throw new InvalidOperationException(
                "No browser page is currently open."
            );
        }

        AriaRole ariaRole =
            ParseAriaRole(
                role
            );

        if (string.IsNullOrWhiteSpace(
                accessibleName))
        {
            return
                _page.GetByRole(
                    ariaRole
                );
        }

        return
            _page.GetByRole(
                ariaRole,
                new PageGetByRoleOptions
                {
                    Name =
                        accessibleName,

                    Exact =
                        exact
                }
            );
    }

    // =========================================================
    // ROLE PARSER
    // =========================================================

    private static AriaRole ParseAriaRole(
        string role)
    {
        if (string.IsNullOrWhiteSpace(
                role))
        {
            throw new ArgumentException(
                "ARIA role cannot be empty."
            );
        }

        if (Enum.TryParse(
                role.Trim(),
                true,
                out AriaRole parsed))
        {
            return parsed;
        }

        throw new ArgumentException(
            $"Unsupported ARIA role '{role}'."
        );
    }

    // =========================================================
    // DESCRIBE LOCATOR
    // =========================================================

    private static async Task<string> DescribeLocatorMatchesAsync(
        ILocator locator,
        string description)
    {
        int count =
            await locator.CountAsync();

        if (count == 0)
        {
            return
                $"NOT_FOUND: No browser elements matched {description}.";
        }

        StringBuilder result =
            new StringBuilder();

        result.AppendLine(
            $"FOUND: {count} element(s) matched {description}."
        );

        int maximum =
            Math.Min(
                count,
                20
            );

        for (int i = 0;
             i < maximum;
             i++)
        {
            try
            {
                ILocator element =
                    locator.Nth(
                        i
                    );

                string text = "";

                try
                {
                    text =
                        (
                            await element.InnerTextAsync()
                        )
                        .Trim();
                }
                catch
                {
                }

                string? id =
                    await element.GetAttributeAsync(
                        "id"
                    );

                string? name =
                    await element.GetAttributeAsync(
                        "name"
                    );

                string? placeholder =
                    await element.GetAttributeAsync(
                        "placeholder"
                    );

                string? ariaLabel =
                    await element.GetAttributeAsync(
                        "aria-label"
                    );

                bool visible = false;

                try
                {
                    visible =
                        await element.IsVisibleAsync();
                }
                catch
                {
                }

                result.AppendLine(
                    $"[{i + 1}] " +
                    $"Text=\"{text}\", " +
                    $"Id=\"{id}\", " +
                    $"Name=\"{name}\", " +
                    $"Placeholder=\"{placeholder}\", " +
                    $"AriaLabel=\"{ariaLabel}\", " +
                    $"Visible={visible}"
                );
            }
            catch
            {
            }
        }

        return
            result.ToString();
    }

    // =========================================================
    // SELECT OPTION BUILDER
    // =========================================================

    private static SelectOptionValue BuildSelectOption(
        string selectionType,
        string selection)
    {
        string type =
            selectionType
                .Trim()
                .ToLowerInvariant();

        switch (type)
        {
            case "value":
                {
                    return
                        new SelectOptionValue
                        {
                            Value = selection
                        };
                }

            case "label":
                {
                    return
                        new SelectOptionValue
                        {
                            Label = selection
                        };
                }

            case "index":
                {
                    if (!int.TryParse(
                            selection,
                            out int index))
                    {
                        throw new ArgumentException(
                            $"Dropdown index '{selection}' is not a valid integer."
                        );
                    }

                    if (index < 0)
                    {
                        throw new ArgumentException(
                            "Dropdown index cannot be negative."
                        );
                    }

                    return
                        new SelectOptionValue
                        {
                            Index = index
                        };
                }

            default:
                {
                    throw new ArgumentException(
                        $"Unsupported selection type '{selectionType}'. " +
                        "Supported types: value, label, index."
                    );
                }
        }
    }

    // =========================================================
    // WAIT STATE
    // =========================================================

    private static WaitForSelectorState ParseWaitState(
        string state)
    {
        string normalized =
            state
                .Trim()
                .ToLowerInvariant();

        return normalized switch
        {
            "visible" =>
                WaitForSelectorState.Visible,

            "hidden" =>
                WaitForSelectorState.Hidden,

            "attached" =>
                WaitForSelectorState.Attached,

            "detached" =>
                WaitForSelectorState.Detached,

            _ =>
                throw new ArgumentException(
                    $"Unsupported wait state '{state}'."
                )
        };
    }

    // =========================================================
    // SAFE PATH
    // =========================================================

    private static string ResolvePathInsideRoot(
        string rootDirectory,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            throw new ArgumentException(
                "File path cannot be empty."
            );
        }

        if (Path.IsPathRooted(
                relativePath))
        {
            throw new InvalidOperationException(
                "Absolute paths are not allowed for this operation."
            );
        }

        string root =
            Path.GetFullPath(
                rootDirectory
            )
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

        string resolved =
            Path.GetFullPath(
                Path.Combine(
                    root,
                    relativePath
                )
            );

        string requiredPrefix =
            root +
            Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(
                requiredPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Requested file path escapes the allowed directory."
            );
        }

        return
            resolved;
    }

    // =========================================================
    // REQUIRE PAGE
    // =========================================================

    private static async Task<string> RequirePageAsync()
    {
        if (!IsBrowserRunning())
        {
            return
                "ERROR: Browser is not running.";
        }

        await EnsureCurrentPageAsync();

        if (_page == null)
        {
            return
                "ERROR: No browser page is currently open.";
        }

        return
            "SUCCESS";
    }

    // =========================================================
    // ENSURE BROWSER
    // =========================================================

    private static async Task<string> EnsureBrowserAsync()
    {
        if (IsBrowserRunning())
        {
            await EnsureCurrentPageAsync();

            return
                "SUCCESS";
        }

        return
            await StartBrowserAsync();
    }

    // =========================================================
    // ENSURE CURRENT PAGE
    // =========================================================

    private static async Task EnsureCurrentPageAsync()
    {
        if (_context == null)
        {
            return;
        }

        if (_page != null)
        {
            try
            {
                if (!_page.IsClosed)
                {
                    ConfigurePage(
                        _page
                    );

                    return;
                }
            }
            catch
            {
                _page = null;
            }
        }

        InvalidateVisualClickGuard();

        if (_context.Pages.Count > 0)
        {
            _page =
                _context.Pages[
                    _context.Pages.Count - 1
                ];

            ConfigurePage(
                _page
            );

            await _page.BringToFrontAsync();

            return;
        }

        _page =
            await _context.NewPageAsync();

        ConfigurePage(
            _page
        );

        await _page.BringToFrontAsync();
    }

    // =========================================================
    // BROWSER STATE
    // =========================================================

    private static bool IsBrowserRunning()
    {
        try
        {
            if (_context == null)
            {
                return false;
            }

            IBrowser? browser =
                _context.Browser;

            return
                browser != null
                &&
                browser.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    // =========================================================
    // PAGE CONFIGURATION
    // =========================================================

    private static void ConfigurePage(
        IPage? page)
    {
        if (page == null)
        {
            return;
        }

        page.SetDefaultTimeout(
            15000
        );

        page.SetDefaultNavigationTimeout(
            30000
        );
    }

    // =========================================================
    // URL
    // =========================================================

    private static string NormalizeUrl(
        string url)
    {
        string result =
            url.Trim();

        if (
            !result.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase)
            &&
            !result.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result =
                "https://" +
                result;
        }

        return
            result;
    }

    // =========================================================
    // FAILURE CHECK
    // =========================================================

    private static bool IsError(
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
}