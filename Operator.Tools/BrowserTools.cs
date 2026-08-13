using Microsoft.Playwright;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Operator.Tools;

public static class BrowserTools
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static IBrowserContext? _context;
    private static IPage? _page;

    // =========================================================
    // START BROWSER
    // =========================================================

    public static async Task<string> StartBrowserAsync()
    {
        try
        {
            if (_browser != null &&
                _browser.IsConnected &&
                _page != null)
            {
                return
                    "SUCCESS: Browser is already running.";
            }

            _playwright =
                await Playwright.CreateAsync();

            _browser =
                await _playwright
                    .Chromium
                    .LaunchAsync(
                        new BrowserTypeLaunchOptions
                        {
                            Headless = false,
                            SlowMo = 80
                        }
                    );

            _context =
                await _browser.NewContextAsync(
                    new BrowserNewContextOptions
                    {
                        ViewportSize =
                            new ViewportSize
                            {
                                Width = 1400,
                                Height = 900
                            }
                    }
                );

            _page =
                await _context.NewPageAsync();

            _page.SetDefaultTimeout(
                15000
            );

            return
                "SUCCESS: Chromium browser started.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not start browser: {ex.Message}";
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

            url =
                NormalizeUrl(url);

            await _page!.GotoAsync(
                url,
                new PageGotoOptions
                {
                    WaitUntil =
                        WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
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
            if (_page == null)
            {
                return
                    "ERROR: No browser page is currently open.";
            }

            ILocator links =
                _page.Locator("a");

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
                    // Ignore disappearing links.
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
                    // Ignore elements that disappear.
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
            if (_page == null)
            {
                return
                    "ERROR: No browser page is currently open.";
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
    // CLICK
    // =========================================================

    public static async Task<string> ClickAsync(
        string locatorType,
        string query)
    {
        try
        {
            if (_page == null)
            {
                return
                    "ERROR: No browser page is currently open.";
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

            ILocator target =
                locator.Nth(0);

            await target.ClickAsync();

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
    // Replaces existing field contents.
    // =========================================================

    public static async Task<string> FillAsync(
        string locatorType,
        string query,
        string text)
    {
        try
        {
            if (_page == null)
            {
                return
                    "ERROR: No browser page is currently open.";
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
    // Types characters sequentially like a user.
    // =========================================================

    public static async Task<string> TypeAsync(
        string locatorType,
        string query,
        string text)
    {
        try
        {
            if (_page == null)
            {
                return
                    "ERROR: No browser page is currently open.";
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
            if (_page == null)
            {
                return
                    "ERROR: No browser page is currently open.";
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
    // PRESS GLOBAL PAGE KEY
    // =========================================================

    public static async Task<string> PressPageKeyAsync(
        string key)
    {
        try
        {
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
    // BACK
    // =========================================================

    public static async Task<string> BackAsync()
    {
        try
        {
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
                    Timeout = 30000
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
                    Timeout = 30000
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
                    Timeout = 30000
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

            _page.SetDefaultTimeout(
                15000
            );

            if (!string.IsNullOrWhiteSpace(url))
            {
                url =
                    NormalizeUrl(url);

                await _page.GotoAsync(
                    url,
                    new PageGotoOptions
                    {
                        WaitUntil =
                            WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
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
            if (_context == null)
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
            if (_context == null)
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

            await _page.BringToFrontAsync();

            string title =
                await _page.TitleAsync();

            return
                $"SUCCESS: Switched to tab {tabNumber}. Title: {title}. URL: {_page.Url}";
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
            if (_context == null)
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

            await tab.CloseAsync();

            if (_context.Pages.Count == 0)
            {
                _page =
                    await _context.NewPageAsync();

                _page.SetDefaultTimeout(
                    15000
                );
            }
            else if (closingCurrent)
            {
                _page =
                    _context.Pages[
                        _context.Pages.Count - 1
                    ];

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
                await _context.CloseAsync();
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
            }

            _page = null;
            _context = null;
            _browser = null;

            _playwright?.Dispose();

            _playwright = null;

            return
                "SUCCESS: Browser closed.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not close browser: {ex.Message}";
        }
    }

    // =========================================================
    // RESOLVE LOCATOR
    //
    // Supported locator types:
    //
    // css
    // text
    // label
    // placeholder
    // title
    // testid
    //
    // Playwright recommends user-facing locators such as role,
    // label, placeholder, and text where practical.
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
    // ENSURE BROWSER
    // =========================================================

    private static async Task<string> EnsureBrowserAsync()
    {
        if (_browser != null &&
            _browser.IsConnected &&
            _context != null &&
            _page != null)
        {
            return
                "SUCCESS";
        }

        return
            await StartBrowserAsync();
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
                StringComparison.OrdinalIgnoreCase);
    }
}