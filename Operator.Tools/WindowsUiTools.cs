using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace Operator.Tools;

public static class WindowsUiTools
{
    public static string ListWindows()
    {
        try
        {
            AutomationElement root =
                AutomationElement.RootElement;

            AutomationElementCollection children =
                root.FindAll(
                    TreeScope.Children,
                    Condition.TrueCondition
                );

            List<string> windows = new();

            foreach (AutomationElement element in children)
            {
                try
                {
                    if (element.Current.ControlType !=
                        ControlType.Window)
                    {
                        continue;
                    }

                    string name =
                        element.Current.Name;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        windows.Add(name);
                    }
                }
                catch
                {
                    // Window may close while scanning.
                }
            }

            if (windows.Count == 0)
            {
                return "No visible windows found.";
            }

            return
                "VISIBLE WINDOWS:\n" +
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

            AutomationElementCollection controls =
                window.FindAll(
                    TreeScope.Descendants,
                    Condition.TrueCondition
                );

            int count = 0;

            foreach (AutomationElement control in controls)
            {
                if (count >= 80)
                {
                    output.AppendLine(
                        "... limited to 80 controls"
                    );

                    break;
                }

                try
                {
                    string name =
                        control.Current.Name;

                    string type =
                        control.Current
                            .ControlType
                            .ProgrammaticName;

                    string automationId =
                        control.Current.AutomationId;

                    output.AppendLine(
                        $"[{count + 1}] " +
                        $"Type={type}, " +
                        $"Name=\"{name}\", " +
                        $"AutomationId=\"{automationId}\""
                    );

                    count++;
                }
                catch
                {
                    // Ignore disappearing controls.
                }
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

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
                $"SUCCESS: Focused window '{window.Current.Name}'.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

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
                    // Continue with window focus.
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

    private static AutomationElement? FindWindow(
        string partialTitle)
    {
        AutomationElement root =
            AutomationElement.RootElement;

        AutomationElementCollection children =
            root.FindAll(
                TreeScope.Children,
                Condition.TrueCondition
            );

        foreach (AutomationElement element in children)
        {
            try
            {
                if (element.Current.ControlType !=
                    ControlType.Window)
                {
                    continue;
                }

                string name =
                    element.Current.Name;

                if (name.Contains(
                    partialTitle,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }
            catch
            {
                // Ignore disappearing windows.
            }
        }

        return null;
    }

    private static AutomationElement? FindEditableControl(
        AutomationElement window)
    {
        Condition condition =
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

        return window.FindFirst(
            TreeScope.Descendants,
            condition
        );
    }

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
            throw new InvalidOperationException(
                "Ctrl+V SendInput failed."
            );
        }
    }

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

    private const uint INPUT_KEYBOARD =
        1;

    private const uint KEYEVENTF_KEYUP =
        0x0002;

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize
    );

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