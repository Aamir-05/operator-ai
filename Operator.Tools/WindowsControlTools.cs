using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

namespace Operator.Tools;

public static class WindowsControlTools
{
    // =========================================================
    // WINDOWS NATIVE API
    // =========================================================

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // =========================================================
    // SET CONTROL VALUE
    // =========================================================

    public static string SetControlValue(
        string windowTitle,
        string controlQuery,
        string value)
    {
        try
        {
            AutomationElement? window =
                FindWindow(windowTitle);

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window '{windowTitle}' was not found.";
            }

            AutomationElement? control =
                FindControl(
                    window,
                    controlQuery
                );

            if (control == null)
            {
                return
                    $"NOT_FOUND: Control '{controlQuery}' was not found inside '{window.Current.Name}'.";
            }

            AutomationElement target =
                FindValueControl(control)
                ?? control;

            if (!target.TryGetCurrentPattern(
                    ValuePattern.Pattern,
                    out object? patternObject))
            {
                return
                    $"ERROR: Control '{controlQuery}' does not support ValuePattern.";
            }

            ValuePattern valuePattern =
                (ValuePattern)patternObject;

            if (valuePattern.Current.IsReadOnly)
            {
                return
                    $"ERROR: Control '{controlQuery}' is read-only.";
            }

            valuePattern.SetValue(value);

            Thread.Sleep(400);

            return
                $"SUCCESS: Set control '{controlQuery}' to '{value}'.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // INVOKE / CLICK CONTROL
    // =========================================================

    public static string InvokeControl(
        string windowTitle,
        string controlQuery)
    {
        try
        {
            AutomationElement? window =
                FindWindow(windowTitle);

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window '{windowTitle}' was not found.";
            }

            AutomationElement? control =
                FindControl(
                    window,
                    controlQuery
                );

            if (control == null)
            {
                return
                    $"NOT_FOUND: Control '{controlQuery}' was not found inside '{window.Current.Name}'.";
            }

            if (control.TryGetCurrentPattern(
                    InvokePattern.Pattern,
                    out object? invokeObject))
            {
                InvokePattern invokePattern =
                    (InvokePattern)invokeObject;

                invokePattern.Invoke();

                Thread.Sleep(500);

                return
                    $"SUCCESS: Invoked control '{control.Current.Name}'.";
            }

            if (control.TryGetCurrentPattern(
                    SelectionItemPattern.Pattern,
                    out object? selectionObject))
            {
                SelectionItemPattern selectionPattern =
                    (SelectionItemPattern)selectionObject;

                selectionPattern.Select();

                Thread.Sleep(500);

                return
                    $"SUCCESS: Selected control '{control.Current.Name}'.";
            }

            return
                $"ERROR: Control '{controlQuery}' does not support InvokePattern or SelectionItemPattern.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // GET CONTROL INFORMATION
    // =========================================================

    public static string FindControlInfo(
        string windowTitle,
        string controlQuery)
    {
        try
        {
            AutomationElement? window =
                FindWindow(windowTitle);

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window '{windowTitle}' was not found.";
            }

            AutomationElement? control =
                FindControl(
                    window,
                    controlQuery
                );

            if (control == null)
            {
                return
                    $"NOT_FOUND: Control '{controlQuery}' was not found.";
            }

            return
                $"FOUND CONTROL\n" +
                $"Window: {window.Current.Name}\n" +
                $"Name: {control.Current.Name}\n" +
                $"AutomationId: {control.Current.AutomationId}\n" +
                $"Type: {control.Current.ControlType.ProgrammaticName}\n" +
                $"Enabled: {control.Current.IsEnabled}\n" +
                $"Focusable: {control.Current.IsKeyboardFocusable}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // FIND WINDOW
    //
    // "__FOREGROUND__" grabs the currently active window/dialog.
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

        // Partial match.
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
    // FIND CONTROL
    // =========================================================

    private static AutomationElement? FindControl(
        AutomationElement window,
        string query)
    {
        AutomationElementCollection controls =
            window.FindAll(
                TreeScope.Descendants,
                System.Windows.Automation.Condition.TrueCondition
            );

        // -----------------------------------------------------
        // PASS 1
        // Exact control Name
        // -----------------------------------------------------

        foreach (AutomationElement control in controls)
        {
            try
            {
                string name =
                    control.Current.Name ?? "";

                if (string.Equals(
                    name,
                    query,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return control;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PASS 2
        // Exact AutomationId
        // -----------------------------------------------------

        foreach (AutomationElement control in controls)
        {
            try
            {
                string automationId =
                    control.Current.AutomationId ?? "";

                if (string.Equals(
                    automationId,
                    query,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return control;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PASS 3
        // Partial Name
        // -----------------------------------------------------

        foreach (AutomationElement control in controls)
        {
            try
            {
                string name =
                    control.Current.Name ?? "";

                if (!string.IsNullOrWhiteSpace(name) &&
                    name.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return control;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PASS 4
        // Partial AutomationId
        // -----------------------------------------------------

        foreach (AutomationElement control in controls)
        {
            try
            {
                string automationId =
                    control.Current.AutomationId ?? "";

                if (!string.IsNullOrWhiteSpace(automationId) &&
                    automationId.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return control;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    // =========================================================
    // FIND VALUE-PATTERN CHILD
    // =========================================================

    private static AutomationElement? FindValueControl(
        AutomationElement parent)
    {
        try
        {
            if (parent.TryGetCurrentPattern(
                    ValuePattern.Pattern,
                    out _))
            {
                return parent;
            }

            AutomationElementCollection children =
                parent.FindAll(
                    TreeScope.Descendants,
                    System.Windows.Automation.Condition.TrueCondition
                );

            foreach (AutomationElement child in children)
            {
                try
                {
                    if (child.TryGetCurrentPattern(
                            ValuePattern.Pattern,
                            out _))
                    {
                        return child;
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
}