using System;
using System.Threading.Tasks;
using System.Windows;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7B-1
    // MENU + MODAL DIALOG + RETURN WORKFLOW
    // =========================================================

    private async void DialogWorkflow07BTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogWorkflowTestWindow?
            workflowWindow = null;

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7B DIALOG WORKFLOW TEST"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // OPEN CONTROLLED PARENT WINDOW
            // =================================================

            workflowWindow =
                new DialogWorkflowTestWindow
                {
                    Owner = this
                };

            workflowWindow.Show();

            workflowWindow.Activate();

            workflowWindow.Focus();

            await Task.Delay(
                650
            );

            // =================================================
            // 1. VERIFY PARENT WINDOW
            // =================================================

            Log(
                "[1/12] Verifying workflow parent window..."
            );

            string parentControls =
                await Dialog07B_WaitForForegroundControlsAsync(
                    new[]
                    {
                        "Open Review Dialog",
                        "Workflow Status"
                    },
                    10
                );

            Log(
                parentControls
            );

            if (Dialog07B_IsFailure(
                    parentControls))
            {
                Dialog07B_Fail(
                    "Parent window detection"
                );

                return;
            }

            Log(
                "PASS: Parent workflow window detected"
            );

            // =================================================
            // 2. INSPECT MENU COMMAND
            // =================================================

            Log(
                "[2/12] Inspecting native menu command..."
            );

            string menuInfo =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            "__FOREGROUND__",
                            "menuitem",
                            "Open Review Dialog",
                            true
                        )
                );

            Log(
                menuInfo
            );

            if (Dialog07B_IsFailure(
                    menuInfo))
            {
                Dialog07B_Fail(
                    "Menu item discovery"
                );

                return;
            }

            if (!menuInfo.Contains(
                    "Invoke: True",
                    StringComparison.OrdinalIgnoreCase))
            {
                Dialog07B_Fail(
                    "Menu item InvokePattern"
                );

                return;
            }

            Log(
                "PASS: Menu command exposes InvokePattern"
            );

            // =================================================
            // 3. INVOKE MENU COMMAND
            // =================================================

            Log(
                "[3/12] Invoking Open Review Dialog menu command..."
            );

            string openDialog =
                await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            "__FOREGROUND__",
                            "menuitem",
                            "Open Review Dialog",
                            true
                        )
                );

            Log(
                openDialog
            );

            if (Dialog07B_IsFailure(
                    openDialog))
            {
                Dialog07B_Fail(
                    "Menu command invocation"
                );

                return;
            }

            // =================================================
            // 4. DETECT MODAL DIALOG
            //
            // We deliberately verify the active window by its
            // control structure instead of relying only on title
            // discovery.
            // =================================================

            Log(
                "[4/12] Waiting for modal review dialog..."
            );

            string dialogControls =
                await Dialog07B_WaitForForegroundControlsAsync(
                    new[]
                    {
                        "Reference Code",
                        "Confirm review",
                        "Apply Review",
                        "Cancel"
                    },
                    10
                );

            Log(
                dialogControls
            );

            if (Dialog07B_IsFailure(
                    dialogControls))
            {
                Dialog07B_Fail(
                    "Modal dialog detection"
                );

                return;
            }

            Log(
                "PASS: Modal dialog detected"
            );

            // =================================================
            // 5. SET DIALOG TEXT
            // =================================================

            Log(
                "[5/12] Setting Reference Code..."
            );

            string setReference =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Reference Code",
                            true,
                            "REVIEW-07B-001"
                        )
                );

            Log(
                setReference
            );

            if (Dialog07B_IsFailure(
                    setReference))
            {
                Dialog07B_Fail(
                    "Dialog textbox ValuePattern"
                );

                return;
            }

            string verifyReference =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Reference Code",
                            true
                        )
                );

            Log(
                verifyReference
            );

            if (!verifyReference.Contains(
                    "REVIEW-07B-001",
                    StringComparison.Ordinal))
            {
                Dialog07B_Fail(
                    "Dialog textbox verification"
                );

                return;
            }

            Log(
                "PASS: Dialog textbox set and verified"
            );

            // =================================================
            // 6. SET CONFIRMATION
            // =================================================

            Log(
                "[6/12] Setting confirmation checkbox..."
            );

            string confirm =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetToggleState(
                            "__FOREGROUND__",
                            "checkbox",
                            "Confirm review",
                            true,
                            true
                        )
                );

            Log(
                confirm
            );

            if (Dialog07B_IsFailure(
                    confirm))
            {
                Dialog07B_Fail(
                    "Dialog confirmation toggle"
                );

                return;
            }

            string confirmState =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetToggleState(
                            "__FOREGROUND__",
                            "checkbox",
                            "Confirm review",
                            true
                        )
                );

            Log(
                confirmState
            );

            if (!confirmState.Contains(
                    "State: On",
                    StringComparison.OrdinalIgnoreCase))
            {
                Dialog07B_Fail(
                    "Dialog confirmation verification"
                );

                return;
            }

            Log(
                "PASS: Dialog confirmation set and verified"
            );

            // =================================================
            // 7. INSPECT + INVOKE APPLY
            // =================================================

            Log(
                "[7/12] Inspecting and invoking Apply Review..."
            );

            string applyInfo =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            "__FOREGROUND__",
                            "button",
                            "Apply Review",
                            true
                        )
                );

            Log(
                applyInfo
            );

            if (Dialog07B_IsFailure(
                    applyInfo))
            {
                Dialog07B_Fail(
                    "Apply button inspection"
                );

                return;
            }

            if (!applyInfo.Contains(
                    "Invoke: True",
                    StringComparison.OrdinalIgnoreCase))
            {
                Dialog07B_Fail(
                    "Apply button InvokePattern"
                );

                return;
            }

            string apply =
                await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            "__FOREGROUND__",
                            "button",
                            "Apply Review",
                            true
                        )
                );

            Log(
                apply
            );

            if (Dialog07B_IsFailure(
                    apply))
            {
                Dialog07B_Fail(
                    "Apply button invocation"
                );

                return;
            }

            // =================================================
            // 8. RETURN TO PARENT
            // =================================================

            Log(
                "[8/12] Waiting for return to parent workflow..."
            );

            string returnedParent =
                await Dialog07B_WaitForForegroundControlsAsync(
                    new[]
                    {
                        "Open Review Dialog",
                        "Workflow Status"
                    },
                    10
                );

            Log(
                returnedParent
            );

            if (Dialog07B_IsFailure(
                    returnedParent))
            {
                Dialog07B_Fail(
                    "Return-to-parent detection"
                );

                return;
            }

            string appliedStatus =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Workflow Status",
                            true
                        )
                );

            Log(
                appliedStatus
            );

            bool applied =
                appliedStatus.Contains(
                    "Result: applied",
                    StringComparison.OrdinalIgnoreCase
                );

            bool correctReference =
                appliedStatus.Contains(
                    "Reference=REVIEW-07B-001",
                    StringComparison.Ordinal
                );

            bool confirmed =
                appliedStatus.Contains(
                    "Confirmed=True",
                    StringComparison.OrdinalIgnoreCase
                );

            if (
                !applied
                ||
                !correctReference
                ||
                !confirmed
            )
            {
                Dialog07B_Fail(
                    "Applied workflow result verification"
                );

                return;
            }

            Log(
                "PASS: Modal apply result returned to parent"
            );

            // =================================================
            // 9. OPEN SECOND DIALOG FOR CANCEL FLOW
            // =================================================

            Log(
                "[9/12] Opening second dialog for cancel-flow test..."
            );

            string openCancelDialog =
                await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            "__FOREGROUND__",
                            "menuitem",
                            "Open Review Dialog",
                            true
                        )
                );

            Log(
                openCancelDialog
            );

            if (Dialog07B_IsFailure(
                    openCancelDialog))
            {
                Dialog07B_Fail(
                    "Second menu invocation"
                );

                return;
            }

            string secondDialog =
                await Dialog07B_WaitForForegroundControlsAsync(
                    new[]
                    {
                        "Reference Code",
                        "Confirm review",
                        "Apply Review",
                        "Cancel"
                    },
                    10
                );

            Log(
                secondDialog
            );

            if (Dialog07B_IsFailure(
                    secondDialog))
            {
                Dialog07B_Fail(
                    "Second dialog detection"
                );

                return;
            }

            Log(
                "PASS: Second modal dialog detected"
            );

            // =================================================
            // 10. CANCEL
            // =================================================

            Log(
                "[10/12] Invoking Cancel..."
            );

            string cancelInfo =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            "__FOREGROUND__",
                            "button",
                            "Cancel",
                            true
                        )
                );

            Log(
                cancelInfo
            );

            if (Dialog07B_IsFailure(
                    cancelInfo))
            {
                Dialog07B_Fail(
                    "Cancel button inspection"
                );

                return;
            }

            string cancel =
                await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            "__FOREGROUND__",
                            "button",
                            "Cancel",
                            true
                        )
                );

            Log(
                cancel
            );

            if (Dialog07B_IsFailure(
                    cancel))
            {
                Dialog07B_Fail(
                    "Cancel button invocation"
                );

                return;
            }

            // =================================================
            // 11. VERIFY CANCEL RESULT
            // =================================================

            Log(
                "[11/12] Verifying cancel result..."
            );

            string cancelParent =
                await Dialog07B_WaitForForegroundControlsAsync(
                    new[]
                    {
                        "Open Review Dialog",
                        "Workflow Status"
                    },
                    10
                );

            Log(
                cancelParent
            );

            if (Dialog07B_IsFailure(
                    cancelParent))
            {
                Dialog07B_Fail(
                    "Cancel return-to-parent"
                );

                return;
            }

            string cancelledStatus =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Workflow Status",
                            true
                        )
                );

            Log(
                cancelledStatus
            );

            if (!cancelledStatus.Contains(
                    "Result: cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                Dialog07B_Fail(
                    "Cancel workflow verification"
                );

                return;
            }

            Log(
                "PASS: Cancel result returned to parent"
            );

            // =================================================
            // 12. FINAL VERIFICATION
            // =================================================

            Log(
                "[12/12] Final workflow state verification..."
            );

            string finalControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            120
                        )
                );

            Log(
                finalControls
            );

            if (
                Dialog07B_IsFailure(
                    finalControls)
                ||
                !finalControls.Contains(
                    "Open Review Dialog",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !finalControls.Contains(
                    "Workflow Status",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                Dialog07B_Fail(
                    "Final parent-window verification"
                );

                return;
            }

            Log(
                "PASS: Final workflow state"
            );

            // =================================================
            // SUCCESS
            // =================================================

            Log(
                "============================================================"
            );

            Log(
                "SUCCESS: VERSION 0.7B DIALOG WORKFLOW TEST PASSED."
            );

            Log(
                "Parent window targeting: PASS"
            );

            Log(
                "Native menu item discovery: PASS"
            );

            Log(
                "Menu InvokePattern: PASS"
            );

            Log(
                "Modal dialog detection: PASS"
            );

            Log(
                "Dialog TextBox ValuePattern: PASS"
            );

            Log(
                "Dialog TextBox verification: PASS"
            );

            Log(
                "Dialog CheckBox TogglePattern: PASS"
            );

            Log(
                "Dialog CheckBox verification: PASS"
            );

            Log(
                "Apply button InvokePattern: PASS"
            );

            Log(
                "Modal result return: PASS"
            );

            Log(
                "Return-to-parent targeting: PASS"
            );

            Log(
                "Cancel button workflow: PASS"
            );

            Log(
                "Multi-window state verification: PASS"
            );

            Log(
                "============================================================"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7B DIALOG WORKFLOW TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // WAIT FOR EXPECTED FOREGROUND WINDOW CONTENT
    // =========================================================

    private static async Task<string>
        Dialog07B_WaitForForegroundControlsAsync(
            string[] requiredText,
            int timeoutSeconds)
    {
        int safeTimeout =
            Math.Clamp(
                timeoutSeconds,
                1,
                60
            );

        DateTime deadline =
            DateTime.UtcNow.AddSeconds(
                safeTimeout
            );

        string lastResult =
            "";

        while (DateTime.UtcNow <
               deadline)
        {
            lastResult =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            140
                        )
                );

            if (!Dialog07B_IsFailure(
                    lastResult))
            {
                bool allFound =
                    true;

                foreach (
                    string required
                    in requiredText)
                {
                    if (!lastResult.Contains(
                            required,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        allFound =
                            false;

                        break;
                    }
                }

                if (allFound)
                {
                    return
                        lastResult;
                }
            }

            await Task.Delay(
                150
            );
        }

        return
            "NOT_FOUND: Expected foreground window did not become available.\n" +
            $"Last inspection:\n{lastResult}";
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void Dialog07B_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7B DIALOG WORKFLOW TEST - {testName}"
        );

        Log(
            "Test stopped at first failed requirement."
        );

        Log(
            "============================================================"
        );
    }

    // =========================================================
    // FAILURE CHECK
    // =========================================================

    private static bool Dialog07B_IsFailure(
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
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "TIMEOUT",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase);
    }
}