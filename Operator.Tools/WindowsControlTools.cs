using System;
using System.Runtime.InteropServices;
using System.Text;
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
    // VERSION 0.7A
    //
    // This file preserves the original Operator AI Windows
    // control APIs while adding stronger native UI Automation
    // capabilities.
    // =========================================================

    // =========================================================
    // LEGACY API
    // SET CONTROL VALUE
    //
    // PRESERVED FOR EXISTING OPERATOR AI CODE
    // =========================================================

    public static string SetControlValue(
        string windowTitle,
        string controlQuery,
        string value)
    {
        try
        {
            AutomationElement? window =
                FindWindow(
                    windowTitle
                );

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
                    $"NOT_FOUND: Control '{controlQuery}' was not found inside '{SafeName(window)}'.";
            }

            AutomationElement target =
                FindValueControl(
                    control
                )
                ?? control;

            if (!target.Current.IsEnabled)
            {
                return
                    $"BLOCKED: Control '{controlQuery}' is disabled.";
            }

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
                    $"BLOCKED: Control '{controlQuery}' is read-only.";
            }

            try
            {
                if (target.Current.IsKeyboardFocusable)
                {
                    target.SetFocus();
                }
            }
            catch
            {
            }

            valuePattern.SetValue(
                value ?? ""
            );

            Thread.Sleep(
                250
            );

            string actual =
                valuePattern.Current.Value;

            return
                "SUCCESS: Set control value.\n" +
                $"Control: {controlQuery}\n" +
                $"Value: {actual}";
        }
        catch (ElementNotAvailableException)
        {
            return
                "ERROR: Control disappeared before its value could be set.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // LEGACY API
    // INVOKE / CLICK CONTROL
    //
    // PRESERVED FOR EXISTING OPERATOR AI CODE
    // =========================================================

    public static string InvokeControl(
        string windowTitle,
        string controlQuery)
    {
        try
        {
            AutomationElement? window =
                FindWindow(
                    windowTitle
                );

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
                    $"NOT_FOUND: Control '{controlQuery}' was not found inside '{SafeName(window)}'.";
            }

            return
                ActivateControl(
                    control,
                    controlQuery
                );
        }
        catch (Exception ex)
        {
            return
                $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // LEGACY API
    // FIND CONTROL INFORMATION
    //
    // PRESERVED FOR EXISTING OPERATOR AI CODE
    // =========================================================

    public static string FindControlInfo(
        string windowTitle,
        string controlQuery)
    {
        try
        {
            AutomationElement? window =
                FindWindow(
                    windowTitle
                );

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
                "FOUND CONTROL\n" +
                $"Window: {SafeName(window)}\n" +
                DescribeControl(
                    control
                );
        }
        catch (Exception ex)
        {
            return
                $"ERROR: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // LIST CONTROLS
    // =========================================================

    public static string ListControls(
        string windowTitle,
        int maximumControls = 120)
    {
        try
        {
            AutomationElement? window =
                FindWindow(
                    windowTitle
                );

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            int safeMaximum =
                Math.Clamp(
                    maximumControls,
                    1,
                    500
                );

            AutomationElementCollection controls =
                window.FindAll(
                    TreeScope.Descendants,
                    System.Windows.Automation.Condition.TrueCondition
                );

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "WINDOW CONTROLS"
            );

            result.AppendLine(
                $"Window: {SafeName(window)}"
            );

            result.AppendLine(
                $"Total discovered: {controls.Count}"
            );

            int displayed =
                Math.Min(
                    controls.Count,
                    safeMaximum
                );

            for (int i = 0;
                 i < displayed;
                 i++)
            {
                AutomationElement control =
                    controls[i];

                result.AppendLine(
                    DescribeControlLine(
                        control,
                        i + 1
                    )
                );
            }

            if (controls.Count >
                displayed)
            {
                result.AppendLine(
                    $"... limited to {displayed} of {controls.Count} controls."
                );
            }

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not list Windows controls: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // FIND CONTROL BY TYPE + NAME
    // =========================================================

    public static string FindControl(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? window =
                FindWindow(
                    windowTitle
                );

            if (window == null)
            {
                return
                    $"NOT_FOUND: Window containing '{windowTitle}' was not found.";
            }

            AutomationElement? control =
                FindControlElement(
                    window,
                    controlType,
                    controlName,
                    exactName
                );

            if (control == null)
            {
                return
                    "NOT_FOUND: Windows control was not found.\n" +
                    $"Window: {windowTitle}\n" +
                    $"Type: {controlType}\n" +
                    $"Name: {controlName}\n" +
                    $"Exact name: {exactName}";
            }

            return
                "SUCCESS: Windows control found.\n" +
                DescribeControl(
                    control
                );
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Windows control search failed: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
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
                AutomationElement? window =
                    FindWindow(
                        windowTitle
                    );

                if (window != null)
                {
                    return
                        "SUCCESS: Window became available.\n" +
                        $"Window: {SafeName(window)}";
                }

                Thread.Sleep(
                    150
                );
            }

            return
                $"NOT_FOUND: Window containing '{windowTitle}' did not appear within {timeout} seconds.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for Windows window failed: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // WAIT FOR CONTROL
    // =========================================================

    public static string WaitForControl(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName,
        int timeoutSeconds)
    {
        try
        {
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
                AutomationElement? window =
                    FindWindow(
                        windowTitle
                    );

                if (window != null)
                {
                    AutomationElement? control =
                        FindControlElement(
                            window,
                            controlType,
                            controlName,
                            exactName
                        );

                    if (control != null)
                    {
                        return
                            "SUCCESS: Windows control became available.\n" +
                            DescribeControl(
                                control
                            );
                    }
                }

                Thread.Sleep(
                    150
                );
            }

            return
                "NOT_FOUND: Windows control did not appear within timeout.\n" +
                $"Window: {windowTitle}\n" +
                $"Type: {controlType}\n" +
                $"Name: {controlName}\n" +
                $"Timeout: {timeout} seconds";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Wait for Windows control failed: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // CLICK / ACTIVATE CONTROL
    // =========================================================

    public static string ClickControl(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            return
                ActivateControl(
                    control,
                    controlName
                );
        }
        catch (ElementNotAvailableException)
        {
            return
                "ERROR: Windows control disappeared before it could be activated.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Windows control activation failed: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // SET CONTROL VALUE BY TYPE + NAME
    // =========================================================

    public static string SetControlValue(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName,
        string value)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            AutomationElement target =
                FindValueControl(
                    control
                )
                ?? control;

            if (!target.Current.IsEnabled)
            {
                return
                    $"BLOCKED: Windows control '{SafeName(control)}' is disabled.";
            }

            if (!target.TryGetCurrentPattern(
                    ValuePattern.Pattern,
                    out object? valueObject))
            {
                return
                    "ERROR: Windows control does not support ValuePattern.\n" +
                    DescribeControl(
                        control
                    );
            }

            ValuePattern valuePattern =
                (ValuePattern)valueObject;

            if (valuePattern.Current.IsReadOnly)
            {
                return
                    $"BLOCKED: Windows control '{SafeName(control)}' is read-only.";
            }

            try
            {
                if (target.Current.IsKeyboardFocusable)
                {
                    target.SetFocus();
                }
            }
            catch
            {
            }

            valuePattern.SetValue(
                value ?? ""
            );

            Thread.Sleep(
                150
            );

            string actualValue =
                valuePattern.Current.Value;

            return
                "SUCCESS: Windows control value set.\n" +
                $"Name: {SafeName(control)}\n" +
                $"Value: {actualValue}";
        }
        catch (ElementNotAvailableException)
        {
            return
                "ERROR: Windows control disappeared before its value could be set.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not set Windows control value: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // GET CONTROL VALUE / TEXT
    // =========================================================

    public static string GetControlValue(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            AutomationElement target =
                FindValueControl(
                    control
                )
                ?? control;

            if (target.TryGetCurrentPattern(
                    ValuePattern.Pattern,
                    out object? valueObject))
            {
                ValuePattern valuePattern =
                    (ValuePattern)valueObject;

                return
                    "SUCCESS: Windows control value read.\n" +
                    $"Name: {SafeName(control)}\n" +
                    $"Value: {valuePattern.Current.Value}";
            }

            if (control.TryGetCurrentPattern(
                    TextPattern.Pattern,
                    out object? textObject))
            {
                TextPattern textPattern =
                    (TextPattern)textObject;

                string text =
                    textPattern
                        .DocumentRange
                        .GetText(
                            -1
                        );

                return
                    "SUCCESS: Windows control text read.\n" +
                    $"Name: {SafeName(control)}\n" +
                    $"Text: {text}";
            }

            return
                "SUCCESS: Windows control does not expose ValuePattern or TextPattern.\n" +
                $"Name: {SafeName(control)}";
        }
        catch (ElementNotAvailableException)
        {
            return
                "ERROR: Windows control disappeared before its value could be read.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read Windows control value: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // SET TOGGLE STATE
    // =========================================================

    public static string SetToggleState(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName,
        bool desiredState)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            if (!control.Current.IsEnabled)
            {
                return
                    $"BLOCKED: Windows control '{SafeName(control)}' is disabled.";
            }

            if (!control.TryGetCurrentPattern(
                    TogglePattern.Pattern,
                    out object? toggleObject))
            {
                return
                    "ERROR: Windows control does not support TogglePattern.\n" +
                    DescribeControl(
                        control
                    );
            }

            TogglePattern togglePattern =
                (TogglePattern)toggleObject;

            for (int attempt = 0;
                 attempt < 3;
                 attempt++)
            {
                ToggleState state =
                    togglePattern.Current
                        .ToggleState;

                bool currentlyOn =
                    state ==
                    ToggleState.On;

                if (currentlyOn ==
                    desiredState)
                {
                    return
                        "SUCCESS: Windows toggle state verified.\n" +
                        $"Name: {SafeName(control)}\n" +
                        $"Checked: {desiredState}";
                }

                togglePattern.Toggle();

                Thread.Sleep(
                    100
                );
            }

            ToggleState finalState =
                togglePattern.Current
                    .ToggleState;

            bool finalOn =
                finalState ==
                ToggleState.On;

            if (finalOn !=
                desiredState)
            {
                return
                    "ERROR: Windows toggle did not reach requested state.\n" +
                    $"Requested: {desiredState}\n" +
                    $"Actual: {finalState}";
            }

            return
                "SUCCESS: Windows toggle state changed.\n" +
                $"Name: {SafeName(control)}\n" +
                $"Checked: {finalOn}";
        }
        catch (ElementNotAvailableException)
        {
            return
                "ERROR: Windows toggle control disappeared.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not change Windows toggle state: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // GET TOGGLE STATE
    // =========================================================

    public static string GetToggleState(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            if (!control.TryGetCurrentPattern(
                    TogglePattern.Pattern,
                    out object? toggleObject))
            {
                return
                    "ERROR: Windows control does not support TogglePattern.\n" +
                    DescribeControl(
                        control
                    );
            }

            TogglePattern togglePattern =
                (TogglePattern)toggleObject;

            ToggleState state =
                togglePattern.Current
                    .ToggleState;

            return
                "SUCCESS: Windows toggle state read.\n" +
                $"Name: {SafeName(control)}\n" +
                $"State: {state}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not read Windows toggle state: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // SELECT CONTROL
    // =========================================================

    public static string SelectControl(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            if (!control.Current.IsEnabled)
            {
                return
                    $"BLOCKED: Windows control '{SafeName(control)}' is disabled.";
            }

            if (!control.TryGetCurrentPattern(
                    SelectionItemPattern.Pattern,
                    out object? selectionObject))
            {
                return
                    "ERROR: Windows control does not support SelectionItemPattern.\n" +
                    DescribeControl(
                        control
                    );
            }

            SelectionItemPattern selectionPattern =
                (SelectionItemPattern)selectionObject;

            selectionPattern.Select();

            Thread.Sleep(
                100
            );

            bool selected =
                selectionPattern.Current
                    .IsSelected;

            if (!selected)
            {
                return
                    $"ERROR: Windows control '{SafeName(control)}' did not become selected.";
            }

            return
                "SUCCESS: Windows control selected.\n" +
                $"Name: {SafeName(control)}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not select Windows control: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // EXPAND / COLLAPSE
    // =========================================================

    public static string SetExpandedState(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName,
        bool expanded)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            if (!control.Current.IsEnabled)
            {
                return
                    $"BLOCKED: Windows control '{SafeName(control)}' is disabled.";
            }

            if (!control.TryGetCurrentPattern(
                    ExpandCollapsePattern.Pattern,
                    out object? expandObject))
            {
                return
                    "ERROR: Windows control does not support ExpandCollapsePattern.\n" +
                    DescribeControl(
                        control
                    );
            }

            ExpandCollapsePattern pattern =
                (ExpandCollapsePattern)expandObject;

            ExpandCollapseState current =
                pattern.Current
                    .ExpandCollapseState;

            if (expanded)
            {
                if (current !=
                    ExpandCollapseState.Expanded)
                {
                    pattern.Expand();
                }
            }
            else
            {
                if (current ==
                    ExpandCollapseState.Expanded)
                {
                    pattern.Collapse();
                }
            }

            Thread.Sleep(
                100
            );

            ExpandCollapseState finalState =
                pattern.Current
                    .ExpandCollapseState;

            bool success =
                expanded
                    ? finalState ==
                      ExpandCollapseState.Expanded
                    : finalState ==
                      ExpandCollapseState.Collapsed;

            if (!success)
            {
                return
                    "ERROR: Windows control did not reach requested expanded state.\n" +
                    $"Actual state: {finalState}";
            }

            return
                "SUCCESS: Windows expanded state changed.\n" +
                $"Name: {SafeName(control)}\n" +
                $"Expanded: {expanded}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not change Windows expanded state: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // FOCUS CONTROL
    // =========================================================

    public static string FocusControl(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            if (!control.Current.IsEnabled)
            {
                return
                    $"BLOCKED: Windows control '{SafeName(control)}' is disabled.";
            }

            if (!control.Current.IsKeyboardFocusable)
            {
                return
                    "ERROR: Windows control is not keyboard-focusable.\n" +
                    DescribeControl(
                        control
                    );
            }

            control.SetFocus();

            Thread.Sleep(
                100
            );

            return
                "SUCCESS: Windows control focused.\n" +
                DescribeControl(
                    control
                );
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not focus Windows control: {ex.Message}";
        }
    }

    // =========================================================
    // 0.7A
    // DETAILED CONTROL INFORMATION
    // =========================================================

    public static string GetControlInfo(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName = true)
    {
        try
        {
            AutomationElement? control =
                ResolveControl(
                    windowTitle,
                    controlType,
                    controlName,
                    exactName,
                    out string failure
                );

            if (control == null)
            {
                return
                    failure;
            }

            StringBuilder result =
                new StringBuilder();

            result.AppendLine(
                "WINDOW CONTROL INFORMATION"
            );

            result.AppendLine(
                DescribeControl(
                    control
                )
            );

            result.AppendLine();

            result.AppendLine(
                "Supported interaction patterns:"
            );

            result.AppendLine(
                $"Invoke: {SupportsPattern(control, InvokePattern.Pattern)}"
            );

            result.AppendLine(
                $"Value: {SupportsPattern(control, ValuePattern.Pattern)}"
            );

            result.AppendLine(
                $"Text: {SupportsPattern(control, TextPattern.Pattern)}"
            );

            result.AppendLine(
                $"Toggle: {SupportsPattern(control, TogglePattern.Pattern)}"
            );

            result.AppendLine(
                $"SelectionItem: {SupportsPattern(control, SelectionItemPattern.Pattern)}"
            );

            result.AppendLine(
                $"ExpandCollapse: {SupportsPattern(control, ExpandCollapsePattern.Pattern)}"
            );

            return
                result.ToString();
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not inspect Windows control: {ex.Message}";
        }
    }

    // =========================================================
    // INTERNAL
    // ACTIVATE A CONTROL USING ITS SUPPORTED PATTERN
    // =========================================================

    private static string ActivateControl(
        AutomationElement control,
        string controlDescription)
    {
        if (!control.Current.IsEnabled)
        {
            return
                $"BLOCKED: Windows control '{SafeName(control)}' is disabled.";
        }

        // -----------------------------------------------------
        // INVOKE
        // Buttons, links, many menu commands
        // -----------------------------------------------------

        if (control.TryGetCurrentPattern(
                InvokePattern.Pattern,
                out object? invokeObject))
        {
            InvokePattern invokePattern =
                (InvokePattern)invokeObject;

            invokePattern.Invoke();

            Thread.Sleep(
                250
            );

            return
                "SUCCESS: Invoked Windows control.\n" +
                DescribeControl(
                    control
                );
        }

        // -----------------------------------------------------
        // SELECTION ITEM
        // Tabs, list items, selectable menu items
        // -----------------------------------------------------

        if (control.TryGetCurrentPattern(
                SelectionItemPattern.Pattern,
                out object? selectionObject))
        {
            SelectionItemPattern selectionPattern =
                (SelectionItemPattern)selectionObject;

            selectionPattern.Select();

            Thread.Sleep(
                150
            );

            return
                "SUCCESS: Selected Windows control.\n" +
                DescribeControl(
                    control
                );
        }

        // -----------------------------------------------------
        // TOGGLE
        // Checkbox / toggle control
        // -----------------------------------------------------

        if (control.TryGetCurrentPattern(
                TogglePattern.Pattern,
                out object? toggleObject))
        {
            TogglePattern togglePattern =
                (TogglePattern)toggleObject;

            togglePattern.Toggle();

            Thread.Sleep(
                150
            );

            return
                "SUCCESS: Toggled Windows control.\n" +
                DescribeControl(
                    control
                );
        }

        // -----------------------------------------------------
        // EXPAND / COLLAPSE
        // Combo boxes / expandable menu nodes
        // -----------------------------------------------------

        if (control.TryGetCurrentPattern(
                ExpandCollapsePattern.Pattern,
                out object? expandObject))
        {
            ExpandCollapsePattern expandPattern =
                (ExpandCollapsePattern)expandObject;

            ExpandCollapseState state =
                expandPattern.Current
                    .ExpandCollapseState;

            if (state ==
                ExpandCollapseState.Collapsed)
            {
                expandPattern.Expand();

                Thread.Sleep(
                    150
                );

                return
                    "SUCCESS: Expanded Windows control.\n" +
                    DescribeControl(
                        control
                    );
            }

            if (state ==
                ExpandCollapseState.Expanded)
            {
                expandPattern.Collapse();

                Thread.Sleep(
                    150
                );

                return
                    "SUCCESS: Collapsed Windows control.\n" +
                    DescribeControl(
                        control
                    );
            }
        }

        return
            "ERROR: Windows control does not expose a supported activation pattern.\n" +
            $"Requested control: {controlDescription}\n" +
            DescribeControl(
                control
            );
    }

    // =========================================================
    // INTERNAL
    // RESOLVE CONTROL
    // =========================================================

    private static AutomationElement? ResolveControl(
        string windowTitle,
        string controlType,
        string controlName,
        bool exactName,
        out string failure)
    {
        failure = "";

        AutomationElement? window =
            FindWindow(
                windowTitle
            );

        if (window == null)
        {
            failure =
                $"NOT_FOUND: Window containing '{windowTitle}' was not found.";

            return null;
        }

        AutomationElement? control =
            FindControlElement(
                window,
                controlType,
                controlName,
                exactName
            );

        if (control == null)
        {
            failure =
                "NOT_FOUND: Windows control was not found.\n" +
                $"Window: {windowTitle}\n" +
                $"Type: {controlType}\n" +
                $"Name: {controlName}\n" +
                $"Exact name: {exactName}";

            return null;
        }

        return
            control;
    }

    // =========================================================
    // FIND WINDOW
    //
    // "__FOREGROUND__" grabs the current active window/dialog.
    // =========================================================

    private static AutomationElement? FindWindow(
        string partialTitle)
    {
        if (string.IsNullOrWhiteSpace(
                partialTitle))
        {
            return null;
        }

        if (string.Equals(
                partialTitle,
                "__FOREGROUND__",
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                IntPtr hwnd =
                    GetForegroundWindow();

                if (hwnd ==
                    IntPtr.Zero)
                {
                    return null;
                }

                return
                    AutomationElement.FromHandle(
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

        // -----------------------------------------------------
        // EXACT WINDOW TITLE FIRST
        // -----------------------------------------------------

        foreach (
            AutomationElement element
            in elements)
        {
            try
            {
                string name =
                    element.Current.Name
                    ?? "";

                if (string.Equals(
                        name,
                        partialTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                        element;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PARTIAL WINDOW TITLE SECOND
        // -----------------------------------------------------

        foreach (
            AutomationElement element
            in elements)
        {
            try
            {
                string name =
                    element.Current.Name
                    ?? "";

                if (string.IsNullOrWhiteSpace(
                        name))
                {
                    continue;
                }

                if (name.Contains(
                        partialTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                        element;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    // =========================================================
    // LEGACY FIND CONTROL
    //
    // Search:
    // 1. Exact Name
    // 2. Exact AutomationId
    // 3. Partial Name
    // 4. Partial AutomationId
    // =========================================================

    private static AutomationElement? FindControl(
        AutomationElement window,
        string query)
    {
        if (string.IsNullOrWhiteSpace(
                query))
        {
            return null;
        }

        AutomationElementCollection controls =
            window.FindAll(
                TreeScope.Descendants,
                System.Windows.Automation.Condition.TrueCondition
            );

        // -----------------------------------------------------
        // PASS 1 - EXACT NAME
        // -----------------------------------------------------

        foreach (
            AutomationElement control
            in controls)
        {
            try
            {
                string name =
                    control.Current.Name
                    ?? "";

                if (string.Equals(
                        name,
                        query,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                        control;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PASS 2 - EXACT AUTOMATION ID
        // -----------------------------------------------------

        foreach (
            AutomationElement control
            in controls)
        {
            try
            {
                string automationId =
                    control.Current.AutomationId
                    ?? "";

                if (string.Equals(
                        automationId,
                        query,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                        control;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PASS 3 - PARTIAL NAME
        // -----------------------------------------------------

        foreach (
            AutomationElement control
            in controls)
        {
            try
            {
                string name =
                    control.Current.Name
                    ?? "";

                if (
                    !string.IsNullOrWhiteSpace(
                        name)
                    &&
                    name.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    return
                        control;
                }
            }
            catch
            {
            }
        }

        // -----------------------------------------------------
        // PASS 4 - PARTIAL AUTOMATION ID
        // -----------------------------------------------------

        foreach (
            AutomationElement control
            in controls)
        {
            try
            {
                string automationId =
                    control.Current.AutomationId
                    ?? "";

                if (
                    !string.IsNullOrWhiteSpace(
                        automationId)
                    &&
                    automationId.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    return
                        control;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    // =========================================================
    // 0.7A FIND CONTROL BY TYPE + NAME
    // =========================================================

    private static AutomationElement? FindControlElement(
        AutomationElement window,
        string controlType,
        string controlName,
        bool exactName)
    {
        ControlType? requestedType =
            ParseControlType(
                controlType
            );

        AutomationElementCollection controls =
            window.FindAll(
                TreeScope.Descendants,
                System.Windows.Automation.Condition.TrueCondition
            );

        AutomationElement? partialNameMatch =
            null;

        AutomationElement? automationIdMatch =
            null;

        for (int i = 0;
             i < controls.Count;
             i++)
        {
            AutomationElement control =
                controls[i];

            try
            {
                if (
                    requestedType != null
                    &&
                    !control.Current
                        .ControlType
                        .Equals(
                            requestedType
                        )
                )
                {
                    continue;
                }

                string name =
                    control.Current.Name
                    ?? "";

                string automationId =
                    control.Current.AutomationId
                    ?? "";

                // Empty name means first matching type.

                if (string.IsNullOrWhiteSpace(
                        controlName))
                {
                    return
                        control;
                }

                // Exact visible/accessibility name.

                if (name.Equals(
                        controlName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                        control;
                }

                // Exact AutomationId is also deterministic.

                if (automationId.Equals(
                        controlName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    automationIdMatch =
                        control;
                }

                // Partial matching only when requested.

                if (!exactName)
                {
                    if (
                        partialNameMatch == null
                        &&
                        !string.IsNullOrWhiteSpace(
                            name)
                        &&
                        name.Contains(
                            controlName,
                            StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        partialNameMatch =
                            control;
                    }

                    if (
                        automationIdMatch == null
                        &&
                        !string.IsNullOrWhiteSpace(
                            automationId)
                        &&
                        automationId.Contains(
                            controlName,
                            StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        automationIdMatch =
                            control;
                    }
                }
            }
            catch
            {
            }
        }

        return
            partialNameMatch
            ??
            automationIdMatch;
    }

    // =========================================================
    // FIND VALUE-PATTERN CONTROL
    //
    // Preserves existing behavior for composite controls.
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
                return
                    parent;
            }

            AutomationElementCollection children =
                parent.FindAll(
                    TreeScope.Descendants,
                    System.Windows.Automation.Condition.TrueCondition
                );

            foreach (
                AutomationElement child
                in children)
            {
                try
                {
                    if (child.TryGetCurrentPattern(
                            ValuePattern.Pattern,
                            out _))
                    {
                        return
                            child;
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
    // CONTROL TYPE PARSER
    // =========================================================

    private static ControlType? ParseControlType(
        string controlType)
    {
        if (string.IsNullOrWhiteSpace(
                controlType))
        {
            return null;
        }

        string type =
            controlType
                .Trim()
                .Replace(
                    " ",
                    ""
                )
                .Replace(
                    "_",
                    ""
                )
                .Replace(
                    "-",
                    ""
                )
                .ToLowerInvariant();

        return type switch
        {
            "button" =>
                ControlType.Button,

            "calendar" =>
                ControlType.Calendar,

            "checkbox" =>
                ControlType.CheckBox,

            "combobox" =>
                ControlType.ComboBox,

            "custom" =>
                ControlType.Custom,

            "dataitem" =>
                ControlType.DataItem,

            "datagrid" =>
                ControlType.DataGrid,

            "document" =>
                ControlType.Document,

            "edit" =>
                ControlType.Edit,

            "group" =>
                ControlType.Group,

            "header" =>
                ControlType.Header,

            "headeritem" =>
                ControlType.HeaderItem,

            "hyperlink" =>
                ControlType.Hyperlink,

            "image" =>
                ControlType.Image,

            "list" =>
                ControlType.List,

            "listitem" =>
                ControlType.ListItem,

            "menu" =>
                ControlType.Menu,

            "menubar" =>
                ControlType.MenuBar,

            "menuitem" =>
                ControlType.MenuItem,

            "pane" =>
                ControlType.Pane,

            "progressbar" =>
                ControlType.ProgressBar,

            "radiobutton" =>
                ControlType.RadioButton,

            "scrollbar" =>
                ControlType.ScrollBar,

            "separator" =>
                ControlType.Separator,

            "slider" =>
                ControlType.Slider,

            "spinner" =>
                ControlType.Spinner,

            "splitbutton" =>
                ControlType.SplitButton,

            "statusbar" =>
                ControlType.StatusBar,

            "tab" =>
                ControlType.Tab,

            "tabitem" =>
                ControlType.TabItem,

            "table" =>
                ControlType.Table,

            "text" =>
                ControlType.Text,

            "thumb" =>
                ControlType.Thumb,

            "titlebar" =>
                ControlType.TitleBar,

            "toolbar" =>
                ControlType.ToolBar,

            "tooltip" =>
                ControlType.ToolTip,

            "tree" =>
                ControlType.Tree,

            "treeitem" =>
                ControlType.TreeItem,

            "window" =>
                ControlType.Window,

            _ =>
                throw new ArgumentException(
                    $"Unsupported Windows control type '{controlType}'."
                )
        };
    }

    // =========================================================
    // PATTERN SUPPORT
    // =========================================================

    private static bool SupportsPattern(
        AutomationElement control,
        AutomationPattern pattern)
    {
        try
        {
            return
                control.TryGetCurrentPattern(
                    pattern,
                    out _
                );
        }
        catch
        {
            return false;
        }
    }

    // =========================================================
    // CONTROL DESCRIPTION
    // =========================================================

    private static string DescribeControl(
        AutomationElement control)
    {
        try
        {
            System.Windows.Rect rectangle =
                control.Current
                    .BoundingRectangle;

            return
                $"Name: {SafeName(control)}\n" +
                $"Type: {SafeControlType(control)}\n" +
                $"AutomationId: {SafeAutomationId(control)}\n" +
                $"Enabled: {SafeEnabled(control)}\n" +
                $"Focusable: {SafeFocusable(control)}\n" +
                $"X: {rectangle.X:0.##}\n" +
                $"Y: {rectangle.Y:0.##}\n" +
                $"Width: {rectangle.Width:0.##}\n" +
                $"Height: {rectangle.Height:0.##}";
        }
        catch
        {
            return
                $"Name: {SafeName(control)}\n" +
                $"Type: {SafeControlType(control)}\n" +
                $"AutomationId: {SafeAutomationId(control)}";
        }
    }

    // =========================================================
    // ONE-LINE CONTROL DESCRIPTION
    // =========================================================

    private static string DescribeControlLine(
        AutomationElement control,
        int index)
    {
        return
            $"[{index}] " +
            $"Type={SafeControlType(control)}, " +
            $"Name=\"{CleanValue(SafeName(control))}\", " +
            $"AutomationId=\"{CleanValue(SafeAutomationId(control))}\", " +
            $"Enabled={SafeEnabled(control)}, " +
            $"Focusable={SafeFocusable(control)}";
    }

    // =========================================================
    // SAFE PROPERTIES
    // =========================================================

    private static string SafeName(
        AutomationElement element)
    {
        try
        {
            return
                element.Current.Name
                ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static string SafeAutomationId(
        AutomationElement element)
    {
        try
        {
            return
                element.Current.AutomationId
                ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static string SafeControlType(
        AutomationElement element)
    {
        try
        {
            return
                element.Current
                    .ControlType
                    .ProgrammaticName
                    .Replace(
                        "ControlType.",
                        ""
                    );
        }
        catch
        {
            return
                "Unknown";
        }
    }

    private static bool SafeEnabled(
        AutomationElement element)
    {
        try
        {
            return
                element.Current.IsEnabled;
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeFocusable(
        AutomationElement element)
    {
        try
        {
            return
                element.Current.IsKeyboardFocusable;
        }
        catch
        {
            return false;
        }
    }

    private static string CleanValue(
        string value)
    {
        return
            value
                .Replace(
                    "\r",
                    " "
                )
                .Replace(
                    "\n",
                    " "
                )
                .Trim();
    }
}