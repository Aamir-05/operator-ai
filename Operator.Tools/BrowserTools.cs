using Microsoft.Playwright;
using System;
using System.IO;
using System.Text;
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
    // PERSISTENT PROFILE
    // =========================================================

    private static readonly string PersistentProfileDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ),
            "OperatorAI",
            "BrowserProfile"
        );

    // =========================================================
    // DOWNLOAD DIRECTORY
    //
    // Browser downloads are kept in one predictable location.
    // =========================================================

    private static readonly string DownloadsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory
            ),
            "OperatorDownloads"
        );

    // =========================================================
    // GET PROFILE PATH
    // =========================================================

    public static string GetPersistentProfilePath()
    {
        return PersistentProfileDirectory;
    }

    // =========================================================
    // GET DOWNLOAD PATH
    // =========================================================

    public static string GetDownloadsDirectory()
    {
        return DownloadsDirectory;
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

            // -------------------------------------------------
            // Clear stale references
            // -------------------------------------------------

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

            // -------------------------------------------------
            // Ensure directories exist
            // -------------------------------------------------

            Directory.CreateDirectory(
                PersistentProfileDirectory
            );

            Directory.CreateDirectory(
                DownloadsDirectory
            );

            // -------------------------------------------------
            // Start Playwright
            // -------------------------------------------------

            _playwright =
                await Playwright.CreateAsync();

            // -------------------------------------------------
            // Launch persistent Chromium
            // -------------------------------------------------

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
                                }
                        }
                    );

            // -------------------------------------------------
            // Use existing page if Chromium created one
            // -------------------------------------------------

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
                $"Downloads: {DownloadsDirectory}";
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
    // SESSION INFORMATION
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
                    $"Downloads: {DownloadsDirectory}";
            }

            await EnsureCurrentPageAsync();

            string title = "";

            if (_page != null)
            {
                try
                {
                    title =
                        await _page.TitleAsync();
                }
                catch
                {
                }
            }

            return
                "BROWSER SESSION\n" +
                "Status: Running\n" +
                $"Persistent profile: {PersistentProfileDirectory}\n" +
                $"Downloads: {DownloadsDirectory}\n" +
                $"Open tabs: {_context!.Pages.Count}\n" +
                $"Current title: {title}\n" +
                $"Current URL: {_page?.Url}";
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

            if (IsError(ready))
            {
                return ready;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return
                    "ERROR: URL cannot be empty.";
            }

            await EnsureCurrentPageAsync();

            url =
                NormalizeUrl(
                    url
                );

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
    // GET PAGE INFORMATION
    // =========================================================

    public static async Task<string> GetPageInfoAsync()
    {
        try
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

            string title =
                await _page.TitleAsync();

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
    // READ PAGE TEXT
    // =========================================================

    public static async Task<string> ReadPageTextAsync()
    {
        try
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

            string text =
                await _page
                    .Locator("body")
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
    // LIST LINKS
    // =========================================================

    public static async Task<string> ListLinksAsync()
    {
        try
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

            ILocator links =
                _page.Locator(
                    "a"
                );

            int count =
                await links.CountAsync();

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "PAGE LINKS"
            );

            int maximum =
                Math.Min(
                    count,
                    60
                );

            for (int i = 0;
                 i < maximum;
                 i++)
            {
                try
                {
                    ILocator link =
                        links.Nth(i);

                    string text = "";

                    try
                    {
                        text =
                            (await link.InnerTextAsync())
                                .Trim();
                    }
                    catch
                    {
                    }

                    string? href =
                        await link.GetAttributeAsync(
                            "href"
                        );

                    if (string.IsNullOrWhiteSpace(text) &&
                        string.IsNullOrWhiteSpace(href))
                    {
                        continue;
                    }

                    result.AppendLine(
                        $"[{i + 1}] " +
                        $"Text=\"{text}\" " +
                        $"Href=\"{href}\""
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

            return result.ToString();
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

            ILocator elements =
                _page.Locator(
                    "a, button, input, textarea, select"
                );

            int count =
                await elements.CountAsync();

            int maximum =
                Math.Min(
                    count,
                    100
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
                        elements.Nth(i);

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
                            (await element.InnerTextAsync())
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

                    result.AppendLine(
                        $"[{i + 1}] " +
                        $"Tag={tag}, " +
                        $"Text=\"{text}\", " +
                        $"Id=\"{id}\", " +
                        $"Name=\"{name}\", " +
                        $"Type=\"{type}\", " +
                        $"Placeholder=\"{placeholder}\", " +
                        $"AriaLabel=\"{ariaLabel}\", " +
                        $"Href=\"{href}\""
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

            return result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not inspect interactive elements: {ex.Message}";
        }
    }

    // =========================================================
    // FIND ELEMENTS
    // =========================================================

    public static async Task<string> FindElementsAsync(
        string locatorType,
        string query)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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
                    $"NOT_FOUND: No browser elements matched {locatorType}='{query}'.";
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                $"FOUND: {count} element(s) matched {locatorType}='{query}'."
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
                        locator.Nth(i);

                    string text = "";

                    try
                    {
                        text =
                            (await element.InnerTextAsync())
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

                    result.AppendLine(
                        $"[{i + 1}] " +
                        $"Text=\"{text}\", " +
                        $"Id=\"{id}\", " +
                        $"Name=\"{name}\", " +
                        $"Placeholder=\"{placeholder}\", " +
                        $"AriaLabel=\"{ariaLabel}\""
                    );
                }
                catch
                {
                }
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Element search failed: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT FOR ELEMENT
    //
    // state:
    // visible
    // hidden
    // attached
    // detached
    // =========================================================

    public static async Task<string> WaitForElementAsync(
        string locatorType,
        string query,
        string state,
        int timeoutSeconds)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

            ILocator locator =
                ResolveLocator(
                    locatorType,
                    query
                );

            WaitForSelectorState waitState =
                ParseWaitState(
                    state
                );

            int safeTimeoutSeconds =
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
                        safeTimeoutSeconds
                        * 1000
                }
            );

            return
                $"SUCCESS: Browser element {locatorType}='{query}' reached state '{state}' within {safeTimeoutSeconds} seconds.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for browser element failed: {ex.Message}";
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
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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

            await locator
                .Nth(0)
                .ClickAsync();

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
    // FILL
    // =========================================================

    public static async Task<string> FillAsync(
        string locatorType,
        string query,
        string text)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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
                    $"NOT_FOUND: Could not find browser field {locatorType}='{query}'.";
            }

            await locator
                .Nth(0)
                .FillAsync(
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
    // TYPE
    // =========================================================

    public static async Task<string> TypeAsync(
        string locatorType,
        string query,
        string text)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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
                    $"NOT_FOUND: Could not find browser field {locatorType}='{query}'.";
            }

            await locator
                .Nth(0)
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
    // PRESS KEY ON ELEMENT
    // =========================================================

    public static async Task<string> PressAsync(
        string locatorType,
        string query,
        string key)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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

            await locator
                .Nth(0)
                .PressAsync(
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
    // PRESS PAGE KEY
    // =========================================================

    public static async Task<string> PressPageKeyAsync(
        string key)
    {
        try
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

            await _page.Keyboard.PressAsync(
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
    // SET CHECKBOX / RADIO STATE
    // =========================================================

    public static async Task<string> SetCheckedAsync(
        string locatorType,
        string query,
        bool checkedState)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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
                    $"NOT_FOUND: Could not find checkbox/radio {locatorType}='{query}'.";
            }

            ILocator target =
                locator.Nth(0);

            await target.SetCheckedAsync(
                checkedState
            );

            bool actualState =
                await target.IsCheckedAsync();

            if (actualState != checkedState)
            {
                return
                    $"ERROR: Requested checked state '{checkedState}' was not confirmed.";
            }

            return
                $"SUCCESS: Set {locatorType}='{query}' checked={actualState}.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser checkbox/radio operation failed: {ex.Message}";
        }
    }

    // =========================================================
    // GET CHECKBOX STATE
    // =========================================================

    public static async Task<string> GetCheckedStateAsync(
        string locatorType,
        string query)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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
                    $"NOT_FOUND: Could not find checkbox/radio {locatorType}='{query}'.";
            }

            bool state =
                await locator
                    .Nth(0)
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
    // SELECT DROPDOWN OPTION
    //
    // selectionType:
    // value
    // label
    // index
    // =========================================================

    public static async Task<string> SelectOptionAsync(
        string locatorType,
        string query,
        string selectionType,
        string selection)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

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
                    $"NOT_FOUND: Could not find select element {locatorType}='{query}'.";
            }

            SelectOptionValue option =
                BuildSelectOption(
                    selectionType,
                    selection
                );

            IReadOnlyList<string> selected =
                await locator
                    .Nth(0)
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
    // UPLOAD DESKTOP FILE
    //
    // Security boundary:
    // upload source must be inside Desktop.
    // =========================================================

    public static async Task<string> UploadDesktopFileAsync(
        string locatorType,
        string query,
        string relativePath)
    {
        try
        {
            if (!IsBrowserRunning())
            {
                return
                    "ERROR: Browser is not running.";
            }

            await EnsureCurrentPageAsync();

            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory
                );

            string filePath =
                ResolvePathInsideRoot(
                    desktop,
                    relativePath
                );

            if (!File.Exists(filePath))
            {
                return
                    $"NOT_FOUND: Upload file does not exist: {filePath}";
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
                    $"NOT_FOUND: Could not find file upload input {locatorType}='{query}'.";
            }

            await locator
                .Nth(0)
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
    // DOWNLOAD BY CLICK
    //
    // Downloads are always saved under:
    // Desktop\OperatorDownloads
    //
    // preferredRelativePath may be:
    // ""
    // report.pdf
    // Reports\report.pdf
    // =========================================================

    public static async Task<string> DownloadByClickAsync(
        string locatorType,
        string query,
        string preferredRelativePath)
    {
        try
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

            Directory.CreateDirectory(
                DownloadsDirectory
            );

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
                    $"NOT_FOUND: Could not find download element {locatorType}='{query}'.";
            }

            // -------------------------------------------------
            // Begin waiting BEFORE click
            // -------------------------------------------------

            Task<IDownload> downloadTask =
                _page.WaitForDownloadAsync();

            await locator
                .Nth(0)
                .ClickAsync();

            IDownload download =
                await downloadTask;

            // -------------------------------------------------
            // Determine target filename
            // -------------------------------------------------

            string targetPath;

            if (string.IsNullOrWhiteSpace(
                    preferredRelativePath))
            {
                string safeSuggestedName =
                    Path.GetFileName(
                        download.SuggestedFilename
                    );

                targetPath =
                    ResolvePathInsideRoot(
                        DownloadsDirectory,
                        safeSuggestedName
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

            string? parentDirectory =
                Path.GetDirectoryName(
                    targetPath
                );

            if (!string.IsNullOrWhiteSpace(
                    parentDirectory))
            {
                Directory.CreateDirectory(
                    parentDirectory
                );
            }

            // -------------------------------------------------
            // Save download
            // -------------------------------------------------

            await download.SaveAsAsync(
                targetPath
            );

            if (!File.Exists(targetPath))
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
    // LIST DOWNLOADED FILES
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

            return result.ToString();
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

            await _page.GoBackAsync(
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

            await _page.GoForwardAsync(
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

            await _page.ReloadAsync(
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

            if (IsError(ready))
            {
                return ready;
            }

            _page =
                await _context!.NewPageAsync();

            ConfigurePage(
                _page
            );

            await _page.BringToFrontAsync();

            if (!string.IsNullOrWhiteSpace(url))
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

            int tabCount =
                _context.Pages.Count;

            return
                $"SUCCESS: Opened browser tab {tabCount}. URL: {_page.Url}";
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
            if (!IsBrowserRunning() ||
                _context == null)
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

            return result.ToString();
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
            if (!IsBrowserRunning() ||
                _context == null)
            {
                return
                    "ERROR: Browser is not running.";
            }

            if (tabNumber < 1 ||
                tabNumber > _context.Pages.Count)
            {
                return
                    $"ERROR: Tab {tabNumber} does not exist.";
            }

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
            if (!IsBrowserRunning() ||
                _context == null)
            {
                return
                    "ERROR: Browser is not running.";
            }

            if (tabNumber < 1 ||
                tabNumber > _context.Pages.Count)
            {
                return
                    $"ERROR: Tab {tabNumber} does not exist.";
            }

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

            if (_context.Pages.Count > 0 &&
                closingCurrent &&
                (_page == null ||
                 ReferenceEquals(
                     _page,
                     tab)))
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
    // RESOLVE LOCATOR
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

        if (string.IsNullOrWhiteSpace(query))
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

            _ =>
                throw new ArgumentException(
                    $"Unsupported browser locator type '{locatorType}'. " +
                    "Supported types: css, text, label, placeholder, title, testid."
                )
        };
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
                            Value =
                                selection
                        };
                }

            case "label":
                {
                    return
                        new SelectOptionValue
                        {
                            Label =
                                selection
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
                            Index =
                                index
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
    // WAIT STATE PARSER
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
                    $"Unsupported wait state '{state}'. " +
                    "Supported states: visible, hidden, attached, detached."
                )
        };
    }

    // =========================================================
    // SAFE PATH RESOLUTION
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

        return resolved;
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
    // CHECK BROWSER STATE
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

            if (browser == null)
            {
                return false;
            }

            return
                browser.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    // =========================================================
    // CONFIGURE PAGE
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
    // NORMALIZE URL
    // =========================================================

    private static string NormalizeUrl(
        string url)
    {
        string result =
            url.Trim();

        if (!result.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase)
            &&
            !result.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            result =
                "https://" + result;
        }

        return result;
    }

    // =========================================================
    // ERROR CHECK
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