using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Operator.Tools;

public static class WindowsInputTools
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint INPUT_HARDWARE = 2;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static string PressKey(string keys)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keys))
            {
                return "ERROR: No key was provided.";
            }

            string[] parts =
                keys
                    .ToUpperInvariant()
                    .Split(
                        '+',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries
                    );

            List<ushort> keyCodes = new();

            foreach (string part in parts)
            {
                if (!TryGetVirtualKey(part, out ushort virtualKey))
                {
                    return $"ERROR: Unsupported key '{part}'.";
                }

                keyCodes.Add(virtualKey);
            }

            List<INPUT> inputs = new();

            foreach (ushort keyCode in keyCodes)
            {
                inputs.Add(
                    CreateVirtualKeyInput(
                        keyCode,
                        keyUp: false
                    )
                );
            }

            for (int i = keyCodes.Count - 1; i >= 0; i--)
            {
                inputs.Add(
                    CreateVirtualKeyInput(
                        keyCodes[i],
                        keyUp: true
                    )
                );
            }

            INPUT[] inputArray = inputs.ToArray();

            int inputSize =
                Marshal.SizeOf<INPUT>();

            uint sent =
                SendInput(
                    (uint)inputArray.Length,
                    inputArray,
                    inputSize
                );

            if (sent != inputArray.Length)
            {
                int error =
                    Marshal.GetLastWin32Error();

                return
                    $"ERROR: SendInput failed. " +
                    $"Sent {sent} of {inputArray.Length}. " +
                    $"Win32 error: {error}. " +
                    $"INPUT size: {inputSize}.";
            }

            Thread.Sleep(150);

            return $"SUCCESS: Pressed {keys}.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static bool TryGetVirtualKey(
        string key,
        out ushort virtualKey)
    {
        Dictionary<string, ushort> keys =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["CTRL"] = 0x11,
                ["CONTROL"] = 0x11,
                ["SHIFT"] = 0x10,
                ["ALT"] = 0x12,

                ["ENTER"] = 0x0D,
                ["RETURN"] = 0x0D,
                ["TAB"] = 0x09,
                ["ESC"] = 0x1B,
                ["ESCAPE"] = 0x1B,
                ["SPACE"] = 0x20,
                ["BACKSPACE"] = 0x08,
                ["DELETE"] = 0x2E,

                ["HOME"] = 0x24,
                ["END"] = 0x23,
                ["PAGEUP"] = 0x21,
                ["PAGEDOWN"] = 0x22,

                ["LEFT"] = 0x25,
                ["UP"] = 0x26,
                ["RIGHT"] = 0x27,
                ["DOWN"] = 0x28,

                ["F1"] = 0x70,
                ["F2"] = 0x71,
                ["F3"] = 0x72,
                ["F4"] = 0x73,
                ["F5"] = 0x74,
                ["F6"] = 0x75,
                ["F7"] = 0x76,
                ["F8"] = 0x77,
                ["F9"] = 0x78,
                ["F10"] = 0x79,
                ["F11"] = 0x7A,
                ["F12"] = 0x7B
            };

        if (keys.TryGetValue(
            key,
            out virtualKey))
        {
            return true;
        }

        if (key.Length == 1)
        {
            char character = key[0];

            if (character >= 'A' &&
                character <= 'Z')
            {
                virtualKey = character;
                return true;
            }

            if (character >= '0' &&
                character <= '9')
            {
                virtualKey = character;
                return true;
            }
        }

        virtualKey = 0;

        return false;
    }

    private static INPUT CreateVirtualKeyInput(
        ushort virtualKey,
        bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,

            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags =
                        keyUp
                            ? KEYEVENTF_KEYUP
                            : 0,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

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