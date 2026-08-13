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
                _browser.IsConnected)
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
                            SlowMo = 100
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
            string startResult =
                await EnsureBrowserAsync();

            if (startResult.StartsWith(
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase))
            {
                return startResult;
            }

            if (!url.StartsWith(
                    "http://",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !url.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase))
            {
                url =
                    "https://" + url;
            }

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
    // GET PAGE INFORMATION
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
                $"PAGE INFORMATION\n" +
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

            if (text.Length > 12000)
            {
                text =
                    text.Substring(
                        0,
                        12000
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
                    50
                );

            for (int i = 0;
                 i < maximum;
                 i++)
            {
                try
                {
                    ILocator link =
                        links.Nth(i);

                    string text =
                        (await link.InnerTextAsync())
                            .Trim();

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
                    // Ignore links that disappear.
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
    // INTERNAL BROWSER CHECK
    // =========================================================

    private static async Task<string> EnsureBrowserAsync()
    {
        if (_browser != null &&
            _browser.IsConnected &&
            _page != null)
        {
            return "SUCCESS";
        }

        return
            await StartBrowserAsync();
    }
}