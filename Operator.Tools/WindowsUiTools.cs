using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace Operator.Tools;

public static class WindowsUiTools
{
    // =========================================================
    // WINDOWS NATIVE API
    // =========================================================

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize
    );

    // =========================================================
    // LIST TOP-LEVEL WINDOWS
    // =========================================================

    public static string ListWindows()
    {
        try
        {
            AutomationElement root =
                AutomationElement.RootElement;

            AutomationElementCollection children =
                root.FindAll(
                    TreeScope.Children,
                    System.Windows.Automation.Condition.TrueCondition
                );

            List<string> windows = new();

            foreach (AutomationElement element in children)
            {
                try
                {
                    string name =
                        element.Current.Name ?? "";

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        windows.Add(name);
                    }
                }
                catch
                {
                    // Ignore elements that disappear.
                }
            }

            if (windows.Count == 0)
            {
                return "No visible top-level elements found.";
            }

            return
                "VISIBLE TOP-LEVEL WINDOWS/ELEMENTS:\n" +
                string.Join(
                    Environment.NewLine,
                    windows
                );
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // INSPECT WINDOW / DIALOG
    // =========================================================

    public static string InspectWindow(
        string windowTitle)
    {
        try
        {
            AutomationElement? window =
                FindWindow(windowTitle);

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            StringBuilder output =
                new StringBuilder();

            output.AppendLine(
                $"WINDOW: {window.Current.Name}"
            );

            output.AppendLine(
                $"Type: {window.Current.ControlType.ProgrammaticName}"
            );

            output.AppendLine(
                $"AutomationId: {window.Current.AutomationId}"
            );

            output.AppendLine();

            AutomationElementCollection controls =
                window.FindAll(
                    TreeScope.Descendants,
                    System.Windows.Automation.Condition.TrueCondition
                );

            int count = 0;

            foreach (AutomationElement control in controls)
            {
                if (count >= 150)
                {
                    output.AppendLine(
                        "... output limited to 150 controls"
                    );

                    break;
                }

                try
                {
                    string name =
                        control.Current.Name ?? "";

                    string type =
                        control.Current
                            .ControlType
                            .ProgrammaticName;

                    string automationId =
                        control.Current.AutomationId ?? "";

                    bool enabled =
                        control.Current.IsEnabled;

                    bool focusable =
                        control.Current.IsKeyboardFocusable;

                    output.AppendLine(
                        $"[{count + 1}] " +
                        $"Type={type}, " +
                        $"Name=\"{name}\", " +
                        $"AutomationId=\"{automationId}\", " +
                        $"Enabled={enabled}, " +
                        $"Focusable={focusable}"
                    );

                    count++;
                }
                catch
                {
                    // Ignore inaccessible controls.
                }
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
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
            AutomationElement? window =
                FindWindow(windowTitle);

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            window.SetFocus();

            Thread.Sleep(300);

            return
                $"SUCCESS: Focused '{window.Current.Name}'.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // TYPE TEXT
    // =========================================================

    public static string TypeText(
        string windowTitle,
        string text)
    {
        try
        {
            AutomationElement? window =
                FindWindow(windowTitle);

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            window.SetFocus();

            Thread.Sleep(300);

            AutomationElement? editor =
                FindEditableControl(window);

            if (editor != null)
            {
                try
                {
                    editor.SetFocus();

                    Thread.Sleep(300);
                }
                catch
                {
                    // Continue using focused window.
                }
            }

            System.Windows.Clipboard.SetText(text);

            Thread.Sleep(200);

            SendCtrlV();

            Thread.Sleep(300);

            return
                $"SUCCESS: Inserted {text.Length} characters into '{window.Current.Name}'.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // FIND WINDOW
    //
    // "__FOREGROUND__" means:
    // get whatever native window/dialog currently has focus.
    // =========================================================

    private static AutomationElement? FindWindow(
        string partialTitle)
    {
        if (string.Equals(
            partialTitle,
            "__FOREGROUND__",
            StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                IntPtr hwnd =
                    GetForegroundWindow();

                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                return AutomationElement.FromHandle(
                    hwnd
                );
            }
            catch
            {
                return null;
            }
        }

        AutomationElement root =
            AutomationElement.RootElement;

        AutomationElementCollection elements =
            root.FindAll(
                TreeScope.Children,
                System.Windows.Automation.Condition.TrueCondition
            );

        // Exact match first.
        foreach (AutomationElement element in elements)
        {
            try
            {
                string name =
                    element.Current.Name ?? "";

                if (string.Equals(
                    name,
                    partialTitle,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }
            catch
            {
            }
        }

        // Partial match second.
        foreach (AutomationElement element in elements)
        {
            try
            {
                string name =
                    element.Current.Name ?? "";

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (name.Contains(
                    partialTitle,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    // =========================================================
    // FIND EDITABLE CONTROL
    // =========================================================

    private static AutomationElement? FindEditableControl(
        AutomationElement window)
    {
        try
        {
            System.Windows.Automation.Condition editableCondition =
                new OrCondition(
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Edit
                    ),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Document
                    )
                );

            AutomationElement? directMatch =
                window.FindFirst(
                    TreeScope.Descendants,
                    editableCondition
                );

            if (directMatch != null)
            {
                return directMatch;
            }

            AutomationElementCollection descendants =
                window.FindAll(
                    TreeScope.Descendants,
                    System.Windows.Automation.Condition.TrueCondition
                );

            foreach (AutomationElement element in descendants)
            {
                try
                {
                    if (element.TryGetCurrentPattern(
                        ValuePattern.Pattern,
                        out _))
                    {
                        return element;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return null;
    }

    // =========================================================
    // CTRL+V
    // =========================================================

    private static void SendCtrlV()
    {
        const ushort VK_CONTROL = 0x11;
        const ushort VK_V = 0x56;

        INPUT[] inputs =
        [
            CreateVirtualKeyInput(
                VK_CONTROL,
                false
            ),

            CreateVirtualKeyInput(
                VK_V,
                false
            ),

            CreateVirtualKeyInput(
                VK_V,
                true
            ),

            CreateVirtualKeyInput(
                VK_CONTROL,
                true
            )
        ];

        uint sent =
            SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<INPUT>()
            );

        if (sent != inputs.Length)
        {
            int error =
                Marshal.GetLastWin32Error();

            throw new InvalidOperationException(
                $"Ctrl+V SendInput failed. Win32 error: {error}."
            );
        }
    }

    // =========================================================
    // CREATE KEYBOARD INPUT
    // =========================================================

    private static INPUT CreateVirtualKeyInput(
        ushort virtualKey,
        bool keyUp)
    {
        uint flags =
            keyUp
                ? KEYEVENTF_KEYUP
                : 0;

        return new INPUT
        {
            type = INPUT_KEYBOARD,

            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

    // =========================================================
    // NATIVE STRUCTURES
    // =========================================================

    private const uint INPUT_KEYBOARD =
        1;

    private const uint KEYEVENTF_KEYUP =
        0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}