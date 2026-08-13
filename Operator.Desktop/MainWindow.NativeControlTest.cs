using System;
using System.Threading.Tasks;
using System.Windows;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7A
    // NATIVE WINDOWS CONTROL TEST
    // =========================================================

    private async void NativeControls07ATest_Click(
        object sender,
        RoutedEventArgs e)
    {
        NativeControlTestWindow?
            testWindow = null;

        const string targetWindow =
            "__FOREGROUND__";

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7A NATIVE WINDOWS CONTROL TEST"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // OPEN CONTROLLED TEST WINDOW
            // =================================================

            Log(
                "Opening controlled native test window..."
            );

            testWindow =
                new NativeControlTestWindow
                {
                    Owner = this
                };

            testWindow.Show();

            testWindow.Activate();

            testWindow.Focus();

            await Task.Delay(
                800
            );

            // =================================================
            // IMPORTANT
            //
            // The controlled test window belongs to the same
            // Operator.Desktop process.
            //
            // For this deterministic test we target the active
            // native HWND using the existing __FOREGROUND__
            // WindowsControlTools feature.
            // =================================================

            testWindow.Activate();

            await Task.Delay(
                250
            );

            // =================================================
            // 1. VERIFY FOREGROUND WINDOW
            // =================================================

            Log(
                "[1/10] Verifying active native test window..."
            );

            string controls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            targetWindow,
                            120
                        )
                );

            Log(
                controls
            );

            if (Native07A_IsFailure(
                    controls))
            {
                Native07A_Fail(
                    "Foreground window detection"
                );

                return;
            }

            // Verify that we did not accidentally target
            // the main Operator AI window or another app.

            bool hasOperatorName =
                controls.Contains(
                    "Operator Name",
                    StringComparison.OrdinalIgnoreCase
                );

            bool hasEnableAutomation =
                controls.Contains(
                    "Enable automation",
                    StringComparison.OrdinalIgnoreCase
                );

            bool hasApplyChanges =
                controls.Contains(
                    "Apply Changes",
                    StringComparison.OrdinalIgnoreCase
                );

            if (
                !hasOperatorName
                ||
                !hasEnableAutomation
                ||
                !hasApplyChanges
            )
            {
                Native07A_Fail(
                    "Foreground window identity verification"
                );

                return;
            }

            Log(
                "PASS: Native test window detected"
            );

            // =================================================
            // 2. CONTROL ENUMERATION
            // =================================================

            Log(
                "[2/10] Verifying native control enumeration..."
            );

            if (!controls.Contains(
                    "ControlType.Edit",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !controls.Contains(
                    "Type=Edit",
                    StringComparison.OrdinalIgnoreCase))
            {
                Native07A_Fail(
                    "Edit control enumeration"
                );

                return;
            }

            if (!controls.Contains(
                    "ControlType.Button",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !controls.Contains(
                    "Type=Button",
                    StringComparison.OrdinalIgnoreCase))
            {
                Native07A_Fail(
                    "Button control enumeration"
                );

                return;
            }

            Log(
                "PASS: Native control enumeration"
            );

            // =================================================
            // 3. FIND TEXTBOX
            // =================================================

            Log(
                "[3/10] Finding textbox by control type and name..."
            );

            string findTextbox =
                await Task.Run(
                    () =>
                        WindowsControlTools.FindControl(
                            targetWindow,
                            "edit",
                            "Operator Name",
                            true
                        )
                );

            Log(
                findTextbox
            );

            if (Native07A_IsFailure(
                    findTextbox))
            {
                Native07A_Fail(
                    "Textbox discovery"
                );

                return;
            }

            Log(
                "PASS: Textbox discovered"
            );

            // =================================================
            // 4. SET + VERIFY TEXTBOX
            // =================================================

            Log(
                "[4/10] Setting textbox through ValuePattern..."
            );

            string setValue =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetControlValue(
                            targetWindow,
                            "edit",
                            "Operator Name",
                            true,
                            "Operator AI 0.7A"
                        )
                );

            Log(
                setValue
            );

            if (Native07A_IsFailure(
                    setValue))
            {
                Native07A_Fail(
                    "Textbox ValuePattern"
                );

                return;
            }

            string getValue =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            targetWindow,
                            "edit",
                            "Operator Name",
                            true
                        )
                );

            Log(
                getValue
            );

            if (!getValue.Contains(
                    "Operator AI 0.7A",
                    StringComparison.Ordinal))
            {
                Native07A_Fail(
                    "Textbox verification"
                );

                return;
            }

            Log(
                "PASS: Native textbox set + verification"
            );

            // =================================================
            // 5. CHECKBOX
            // =================================================

            Log(
                "[5/10] Setting checkbox through TogglePattern..."
            );

            string setToggle =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetToggleState(
                            targetWindow,
                            "checkbox",
                            "Enable automation",
                            true,
                            true
                        )
                );

            Log(
                setToggle
            );

            if (Native07A_IsFailure(
                    setToggle))
            {
                Native07A_Fail(
                    "Checkbox TogglePattern"
                );

                return;
            }

            string toggleState =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetToggleState(
                            targetWindow,
                            "checkbox",
                            "Enable automation",
                            true
                        )
                );

            Log(
                toggleState
            );

            if (!toggleState.Contains(
                    "State: On",
                    StringComparison.OrdinalIgnoreCase))
            {
                Native07A_Fail(
                    "Checkbox state verification"
                );

                return;
            }

            Log(
                "PASS: Native checkbox toggle + verification"
            );

            // =================================================
            // 6. COMBOBOX
            // =================================================

            Log(
                "[6/10] Testing ComboBox ExpandCollapsePattern..."
            );

            string comboInfo =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            targetWindow,
                            "combobox",
                            "Department",
                            true
                        )
                );

            Log(
                comboInfo
            );

            if (Native07A_IsFailure(
                    comboInfo))
            {
                Native07A_Fail(
                    "ComboBox inspection"
                );

                return;
            }

            if (!comboInfo.Contains(
                    "ExpandCollapse: True",
                    StringComparison.OrdinalIgnoreCase))
            {
                Native07A_Fail(
                    "ComboBox ExpandCollapse support"
                );

                return;
            }

            string expand =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetExpandedState(
                            targetWindow,
                            "combobox",
                            "Department",
                            true,
                            true
                        )
                );

            Log(
                expand
            );

            if (Native07A_IsFailure(
                    expand))
            {
                Native07A_Fail(
                    "ComboBox expand"
                );

                return;
            }

            await Task.Delay(
                300
            );

            string collapse =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetExpandedState(
                            targetWindow,
                            "combobox",
                            "Department",
                            true,
                            false
                        )
                );

            Log(
                collapse
            );

            if (Native07A_IsFailure(
                    collapse))
            {
                Native07A_Fail(
                    "ComboBox collapse"
                );

                return;
            }

            Log(
                "PASS: Native ComboBox expand/collapse"
            );

            // =================================================
            // 7. TAB SELECTION
            // =================================================

            Log(
                "[7/10] Selecting Operations tab..."
            );

            string tabSelection =
                await Task.Run(
                    () =>
                        WindowsControlTools.SelectControl(
                            targetWindow,
                            "tabitem",
                            "Operations",
                            true
                        )
                );

            Log(
                tabSelection
            );

            if (Native07A_IsFailure(
                    tabSelection))
            {
                Native07A_Fail(
                    "Tab SelectionItemPattern"
                );

                return;
            }

            Log(
                "PASS: Native tab selection"
            );

            // =================================================
            // 8. BUTTON PATTERN
            // =================================================

            Log(
                "[8/10] Inspecting Apply Changes button..."
            );

            string buttonInfo =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            targetWindow,
                            "button",
                            "Apply Changes",
                            true
                        )
                );

            Log(
                buttonInfo
            );

            if (Native07A_IsFailure(
                    buttonInfo))
            {
                Native07A_Fail(
                    "Button inspection"
                );

                return;
            }

            if (!buttonInfo.Contains(
                    "Invoke: True",
                    StringComparison.OrdinalIgnoreCase))
            {
                Native07A_Fail(
                    "Button InvokePattern support"
                );

                return;
            }

            Log(
                "PASS: Native button exposes InvokePattern"
            );

            // =================================================
            // 9. INVOKE BUTTON
            // =================================================

            Log(
                "[9/10] Invoking Apply Changes button..."
            );

            string clickButton =
                await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            targetWindow,
                            "button",
                            "Apply Changes",
                            true
                        )
                );

            Log(
                clickButton
            );

            if (Native07A_IsFailure(
                    clickButton))
            {
                Native07A_Fail(
                    "Button InvokePattern"
                );

                return;
            }

            await Task.Delay(
                350
            );

            Log(
                "PASS: Native button invocation"
            );

            // =================================================
            // 10. READ FINAL RESULT
            // =================================================

            Log(
                "[10/10] Reading final result through UI Automation..."
            );

            string status =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            targetWindow,
                            "edit",
                            "Test Status",
                            true
                        )
                );

            Log(
                status
            );

            if (Native07A_IsFailure(
                    status))
            {
                Native07A_Fail(
                    "Final status read"
                );

                return;
            }

            bool namePassed =
                status.Contains(
                    "Name=Operator AI 0.7A",
                    StringComparison.Ordinal
                );

            bool automationPassed =
                status.Contains(
                    "Automation=Enabled",
                    StringComparison.Ordinal
                );

            bool departmentPassed =
                status.Contains(
                    "Department=Finance",
                    StringComparison.Ordinal
                );

            bool tabPassed =
                status.Contains(
                    "Tab=Operations",
                    StringComparison.Ordinal
                );

            if (!namePassed)
            {
                Log(
                    "Expected Name=Operator AI 0.7A"
                );
            }

            if (!automationPassed)
            {
                Log(
                    "Expected Automation=Enabled"
                );
            }

            if (!departmentPassed)
            {
                Log(
                    "Expected Department=Finance"
                );
            }

            if (!tabPassed)
            {
                Log(
                    "Expected Tab=Operations"
                );
            }

            if (
                !namePassed
                ||
                !automationPassed
                ||
                !departmentPassed
                ||
                !tabPassed
            )
            {
                Native07A_Fail(
                    "Final native state verification"
                );

                return;
            }

            // =================================================
            // SUCCESS
            // =================================================

            Log(
                "============================================================"
            );

            Log(
                "SUCCESS: VERSION 0.7A NATIVE CONTROL TEST PASSED."
            );

            Log(
                "Native window targeting: PASS"
            );

            Log(
                "Control enumeration: PASS"
            );

            Log(
                "Control type + name targeting: PASS"
            );

            Log(
                "TextBox ValuePattern: PASS"
            );

            Log(
                "TextBox verification: PASS"
            );

            Log(
                "CheckBox TogglePattern: PASS"
            );

            Log(
                "CheckBox verification: PASS"
            );

            Log(
                "ComboBox ExpandCollapsePattern: PASS"
            );

            Log(
                "Tab SelectionItemPattern: PASS"
            );

            Log(
                "Button InvokePattern: PASS"
            );

            Log(
                "Final UI state verification: PASS"
            );

            Log(
                "============================================================"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7A NATIVE CONTROL TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void Native07A_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7A NATIVE CONTROL TEST - {testName}"
        );

        Log(
            "Test stopped at first failed requirement."
        );

        Log(
            "============================================================"
        );
    }

    // =========================================================
    // RESULT CHECK
    // =========================================================

    private static bool Native07A_IsFailure(
        string result)
    {
        if (string.IsNullOrWhiteSpace(
                result))
        {
            return true;
        }

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