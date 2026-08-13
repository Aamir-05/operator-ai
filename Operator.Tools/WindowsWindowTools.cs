using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Operator.Tools;

public static class WindowsWindowTools
{
    // =========================================================
    // VERSION 0.7C
    // ROBUST TOP-LEVEL WINDOWS DISCOVERY / SWITCHING
    // =========================================================

    private const int SW_SHOW =
        5;

    private const int SW_RESTORE =
        9;

    private delegate bool EnumWindowsProc(
        IntPtr hWnd,
        IntPtr lParam);

    // =========================================================
    // WIN32
    // =========================================================

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool EnumWindows(
        EnumWindowsProc callback,
        IntPtr lParam);

    [DllImport(
        "user32.dll")]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(
        IntPtr hWnd);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern int GetWindowTextLength(
        IntPtr hWnd);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern int GetWindowText(
        IntPtr hWnd,
        StringBuilder text,
        int maximumCount);

    [DllImport(
        "user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out uint processId);

    [DllImport(
        "user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport(
        "user32.dll")]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(
        IntPtr hWnd);

    [DllImport(
        "user32.dll")]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(
        IntPtr hWnd);

    [DllImport(
        "user32.dll")]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool IsIconic(
        IntPtr hWnd);

    [DllImport(
        "user32.dll")]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(
        IntPtr hWnd,
        int command);

    // =========================================================
    // LIST WINDOWS
    // =========================================================

    public static string ListWindows()
    {
        try
        {
            List<WindowRecord> windows =
                EnumerateWindows();

            if (windows.Count == 0)
            {
                return
                    "NOT_FOUND: No visible titled top-level Windows were found.";
            }

            IntPtr foreground =
                GetForegroundWindow();

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "TOP-LEVEL WINDOWS"
            );

            result.AppendLine(
                $"Total: {windows.Count}"
            );

            for (int index = 0;
                 index < windows.Count;
                 index++)
            {
                WindowRecord window =
                    windows[index];

                string active =
                    window.Handle ==
                    foreground
                        ? " [FOREGROUND]"
                        : "";

                result.AppendLine(
                    $"[{index + 1}] " +
                    $"Title=\"{window.Title}\" " +
                    $"PID={window.ProcessId} " +
                    $"HWND=0x{window.Handle.ToInt64():X}" +
                    active
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not enumerate top-level Windows: {ex.Message}";
        }
    }

    // =========================================================
    // WINDOW EXISTS
    // =========================================================

    public static string WindowExists(
        string windowTitle)
    {
        try
        {
            WindowRecord? window =
                FindWindow(
                    windowTitle
                );

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            return
                "SUCCESS: Window found.\n" +
                $"Title: {window.Title}\n" +
                $"PID: {window.ProcessId}\n" +
                $"HWND: 0x{window.Handle.ToInt64():X}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Window search failed: {ex.Message}";
        }
    }

    // =========================================================
    // WAIT FOR WINDOW
    // =========================================================

    public static string WaitForWindow(
        string windowTitle,
        int timeoutSeconds = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    windowTitle))
            {
                return
                    "ERROR: Window title cannot be empty.";
            }

            int timeout =
                Math.Clamp(
                    timeoutSeconds,
                    1,
                    120
                );

            DateTime deadline =
                DateTime.UtcNow.AddSeconds(
                    timeout
                );

            while (DateTime.UtcNow <
                   deadline)
            {
                WindowRecord? window =
                    FindWindow(
                        windowTitle
                    );

                if (window != null)
                {
                    return
                        "SUCCESS: Window became available.\n" +
                        $"Title: {window.Title}\n" +
                        $"PID: {window.ProcessId}\n" +
                        $"HWND: 0x{window.Handle.ToInt64():X}";
                }

                Thread.Sleep(
                    100
                );
            }

            return
                $"NOT_FOUND: Window containing '{windowTitle}' did not appear within {timeout} seconds.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for window failed: {ex.Message}";
        }
    }

    // =========================================================
    // FOCUS WINDOW
    // =========================================================

    public static string FocusWindow(
        string windowTitle)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    windowTitle))
            {
                return
                    "ERROR: Window title cannot be empty.";
            }

            WindowRecord? window =
                FindWindow(
                    windowTitle
                );

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            IntPtr handle =
                window.Handle;

            if (IsIconic(
                    handle))
            {
                ShowWindowAsync(
                    handle,
                    SW_RESTORE
                );
            }
            else
            {
                ShowWindowAsync(
                    handle,
                    SW_SHOW
                );
            }

            Thread.Sleep(
                80
            );

            BringWindowToTop(
                handle
            );

            SetForegroundWindow(
                handle
            );

            Thread.Sleep(
                250
            );

            IntPtr foreground =
                GetForegroundWindow();

            if (foreground !=
                handle)
            {
                // Second controlled attempt.

                BringWindowToTop(
                    handle
                );

                SetForegroundWindow(
                    handle
                );

                Thread.Sleep(
                    250
                );

                foreground =
                    GetForegroundWindow();
            }

            if (foreground !=
                handle)
            {
                string foregroundTitle =
                    GetWindowTitle(
                        foreground
                    );

                return
                    "ERROR: Requested window did not become foreground.\n" +
                    $"Requested: {window.Title}\n" +
                    $"Current foreground: {foregroundTitle}";
            }

            return
                "SUCCESS: Window focused.\n" +
                $"Title: {window.Title}\n" +
                $"PID: {window.ProcessId}\n" +
                $"HWND: 0x{handle.ToInt64():X}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not focus window: {ex.Message}";
        }
    }

    // =========================================================
    // FOREGROUND WINDOW INFO
    // =========================================================

    public static string GetForegroundWindowInfo()
    {
        try
        {
            IntPtr handle =
                GetForegroundWindow();

            if (handle ==
                IntPtr.Zero)
            {
                return
                    "NOT_FOUND: No foreground window was detected.";
            }

            string title =
                GetWindowTitle(
                    handle
                );

            GetWindowThreadProcessId(
                handle,
                out uint processId
            );

            return
                "FOREGROUND WINDOW\n" +
                $"Title: {title}\n" +
                $"PID: {processId}\n" +
                $"HWND: 0x{handle.ToInt64():X}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not inspect foreground window: {ex.Message}";
        }
    }

    // =========================================================
    // VERIFY FOREGROUND
    // =========================================================

    public static string VerifyForegroundWindow(
        string expectedTitle)
    {
        try
        {
            IntPtr handle =
                GetForegroundWindow();

            if (handle ==
                IntPtr.Zero)
            {
                return
                    "NOT_FOUND: No foreground window was detected.";
            }

            string title =
                GetWindowTitle(
                    handle
                );

            bool exact =
                title.Equals(
                    expectedTitle,
                    StringComparison.OrdinalIgnoreCase
                );

            bool partial =
                title.Contains(
                    expectedTitle,
                    StringComparison.OrdinalIgnoreCase
                );

            if (!exact &&
                !partial)
            {
                return
                    "ERROR: Foreground window does not match expected window.\n" +
                    $"Expected: {expectedTitle}\n" +
                    $"Actual: {title}";
            }

            return
                "SUCCESS: Foreground window verified.\n" +
                $"Title: {title}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Foreground verification failed: {ex.Message}";
        }
    }

    // =========================================================
    // FIND WINDOW
    //
    // Exact title first.
    // Partial title second.
    // =========================================================

    private static WindowRecord? FindWindow(
        string windowTitle)
    {
        if (string.IsNullOrWhiteSpace(
                windowTitle))
        {
            return null;
        }

        List<WindowRecord> windows =
            EnumerateWindows();

        foreach (
            WindowRecord window
            in windows)
        {
            if (window.Title.Equals(
                    windowTitle,
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    window;
            }
        }

        foreach (
            WindowRecord window
            in windows)
        {
            if (window.Title.Contains(
                    windowTitle,
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    window;
            }
        }

        return null;
    }

    // =========================================================
    // ENUMERATE WINDOWS
    // =========================================================

    private static List<WindowRecord>
        EnumerateWindows()
    {
        List<WindowRecord> windows =
            new List<WindowRecord>();

        EnumWindowsProc callback =
            delegate (
                IntPtr handle,
                IntPtr parameter)
            {
                try
                {
                    if (!IsWindowVisible(
                            handle))
                    {
                        return true;
                    }

                    string title =
                        GetWindowTitle(
                            handle
                        );

                    if (string.IsNullOrWhiteSpace(
                            title))
                    {
                        return true;
                    }

                    GetWindowThreadProcessId(
                        handle,
                        out uint processId
                    );

                    windows.Add(
                        new WindowRecord
                        {
                            Handle =
                                handle,

                            Title =
                                title,

                            ProcessId =
                                processId
                        }
                    );
                }
                catch
                {
                }

                return true;
            };

        EnumWindows(
            callback,
            IntPtr.Zero
        );

        windows.Sort(
            (left, right) =>
                string.Compare(
                    left.Title,
                    right.Title,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        return
            windows;
    }

    // =========================================================
    // WINDOW TITLE
    // =========================================================

    private static string GetWindowTitle(
        IntPtr handle)
    {
        if (handle ==
            IntPtr.Zero)
        {
            return "";
        }

        int length =
            GetWindowTextLength(
                handle
            );

        if (length <= 0)
        {
            return "";
        }

        StringBuilder text =
            new StringBuilder(
                length + 1
            );

        GetWindowText(
            handle,
            text,
            text.Capacity
        );

        return
            text.ToString()
                .Trim();
    }

    // =========================================================
    // RECORD
    // =========================================================

    private sealed class WindowRecord
    {
        public IntPtr Handle
        {
            get;
            init;
        }

        public string Title
        {
            get;
            init;
        } = "";

        public uint ProcessId
        {
            get;
            init;
        }
    }
}