using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7B-2
    // AUTONOMOUS MENU + MODAL DIALOG WORKFLOW
    // =========================================================

    private async void DialogAgent07BTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogWorkflowTestWindow?
            workflowWindow = null;

        object actionLock =
            new object();

        HashSet<string> agentActions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        bool forbiddenToolUsed =
            false;

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7B AUTONOMOUS DIALOG WORKFLOW TEST"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // OPEN DETERMINISTIC PARENT WINDOW
            // =================================================

            Log(
                "Opening deterministic parent workflow window..."
            );

            workflowWindow =
                new DialogWorkflowTestWindow
                {
                    Owner = this
                };

            workflowWindow.Show();

            workflowWindow.Activate();

            workflowWindow.Focus();

            await Task.Delay(
                700
            );

            // =================================================
            // VERIFY CORRECT PARENT WINDOW
            // =================================================

            workflowWindow.Activate();

            await Task.Delay(
                200
            );

            string initialControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            140
                        )
                );

            Log(
                initialControls
            );

            if (DialogAgent07B_IsFailure(
                    initialControls))
            {
                DialogAgent07B_Fail(
                    "Initial parent-window inspection"
                );

                return;
            }

            bool correctParent =
                initialControls.Contains(
                    "Open Review Dialog",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                initialControls.Contains(
                    "Workflow Status",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!correctParent)
            {
                DialogAgent07B_Fail(
                    "Parent-window identity verification"
                );

                return;
            }

            Log(
                "PASS: Controlled parent workflow window detected."
            );

            // =================================================
            // RUN AUTONOMOUS AGENT
            // =================================================

            Log(
                "Starting Operator AI autonomous dialog workflow..."
            );

            OperatorAgent agent =
                new OperatorAgent();

            using CancellationTokenSource timeout =
                new CancellationTokenSource(
                    TimeSpan.FromMinutes(3)
                );

            string result =
                await agent.RunAsync(
                    """
                    Work only with the currently active native Windows
                    application and its modal dialog.

                    The currently active parent window is:

                    Operator AI Dialog Workflow Test

                    Use native Windows UI Automation tools.

                    Do NOT use:
                    - browser tools
                    - browser coordinates
                    - type_text
                    - press_key
                    - keyboard shortcuts
                    - screen-coordinate clicking

                    Complete this exact workflow:

                    1. Inspect the native controls in the currently
                       active parent window.

                    2. Find the native menu item named:

                       Open Review Dialog

                    3. Inspect that menu item if useful.

                    4. Activate Open Review Dialog using native Windows
                       UI Automation.

                    5. The foreground window will change to a modal
                       dialog.

                       Inspect the controls in the new foreground window
                       instead of assuming its controls.

                    6. In the modal dialog, set the native textbox named:

                       Reference Code

                       to exactly:

                       AUTON-07B-002

                    7. Verify that Reference Code contains exactly:

                       AUTON-07B-002

                    8. Set the native checkbox named:

                       Confirm review

                       to ON.

                    9. Verify Confirm review is ON.

                    10. Inspect the native button named:

                        Apply Review

                    11. Activate Apply Review using its native
                        UI Automation pattern.

                    12. The modal dialog should close and the parent
                        workflow window should become active again.

                        Inspect the foreground controls again to verify
                        that you returned to the parent window.

                    13. Read the native control named:

                        Workflow Status

                    14. Verify Workflow Status contains all of these:

                        Result: applied
                        Reference=AUTON-07B-002
                        Confirmed=True

                    Do not perform any other action.

                    Do not claim success unless the final Workflow Status
                    has actually been read through a native Windows
                    inspection tool and verified.
                    """,
                    message =>
                    {
                        // =============================================
                        // TRACK WHICH TOOLS THE AGENT ACTUALLY USED
                        // =============================================

                        lock (actionLock)
                        {
                            if (message.Contains(
                                    "[ACTION] windows_list_controls",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_list_controls"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_find_control",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_find_control"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_get_control_info",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_get_control_info"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_set_control_value",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_set_control_value"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_get_control_value",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_get_control_value"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_set_toggle",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_set_toggle"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_get_toggle",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_get_toggle"
                                );
                            }

                            if (message.Contains(
                                    "[ACTION] windows_click_control",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                agentActions.Add(
                                    "windows_click_control"
                                );
                            }

                            // =============================================
                            // THESE ARE FORBIDDEN IN THIS TEST
                            // =============================================

                            if (
                                message.Contains(
                                    "[ACTION] browser_",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                message.Contains(
                                    "[ACTION] type_text",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                message.Contains(
                                    "[ACTION] press_key",
                                    StringComparison.OrdinalIgnoreCase)
                            )
                            {
                                forbiddenToolUsed =
                                    true;
                            }
                        }

                        Dispatcher.Invoke(
                            () =>
                                Log(
                                    $"[AGENT] {message}"
                                )
                        );
                    },
                    timeout.Token
                );

            Log(
                $"Agent result: {result}"
            );

            if (DialogAgent07B_IsFailure(
                    result))
            {
                DialogAgent07B_Fail(
                    "Autonomous agent execution"
                );

                return;
            }

            // =================================================
            // VERIFY NO FALLBACK / FORBIDDEN TOOLS WERE USED
            // =================================================

            bool usedForbidden;

            lock (actionLock)
            {
                usedForbidden =
                    forbiddenToolUsed;
            }

            if (usedForbidden)
            {
                DialogAgent07B_Fail(
                    "Agent used forbidden browser/keyboard fallback"
                );

                return;
            }

            Log(
                "PASS: Agent stayed within native UI Automation."
            );

            // =================================================
            // VERIFY REQUIRED NATIVE TOOL TYPES WERE USED
            // =================================================

            bool usedListControls;
            bool usedSetValue;
            bool usedGetValue;
            bool usedSetToggle;
            bool usedGetToggle;
            bool usedClick;

            lock (actionLock)
            {
                usedListControls =
                    agentActions.Contains(
                        "windows_list_controls"
                    );

                usedSetValue =
                    agentActions.Contains(
                        "windows_set_control_value"
                    );

                usedGetValue =
                    agentActions.Contains(
                        "windows_get_control_value"
                    );

                usedSetToggle =
                    agentActions.Contains(
                        "windows_set_toggle"
                    );

                usedGetToggle =
                    agentActions.Contains(
                        "windows_get_toggle"
                    );

                usedClick =
                    agentActions.Contains(
                        "windows_click_control"
                    );
            }

            if (!usedListControls)
            {
                DialogAgent07B_Fail(
                    "Agent did not inspect native controls"
                );

                return;
            }

            if (!usedSetValue)
            {
                DialogAgent07B_Fail(
                    "Agent did not use native ValuePattern"
                );

                return;
            }

            if (!usedGetValue)
            {
                DialogAgent07B_Fail(
                    "Agent did not verify native values"
                );

                return;
            }

            if (!usedSetToggle)
            {
                DialogAgent07B_Fail(
                    "Agent did not set the native checkbox"
                );

                return;
            }

            if (!usedGetToggle)
            {
                DialogAgent07B_Fail(
                    "Agent did not verify the native checkbox"
                );

                return;
            }

            if (!usedClick)
            {
                DialogAgent07B_Fail(
                    "Agent did not use native control invocation"
                );

                return;
            }

            Log(
                "PASS: Required native agent tools were exercised."
            );

            // =================================================
            // PARENT WINDOW MUST STILL EXIST
            // =================================================

            if (
                workflowWindow == null
                ||
                !workflowWindow.IsVisible
            )
            {
                DialogAgent07B_Fail(
                    "Parent workflow window unexpectedly closed"
                );

                return;
            }

            // =================================================
            // INDEPENDENTLY RE-ACTIVATE PARENT
            //
            // The agent result alone is not trusted.
            // =================================================

            Log(
                "Independently verifying actual Windows UI state..."
            );

            workflowWindow.Activate();

            workflowWindow.Focus();

            await Task.Delay(
                250
            );

            // =================================================
            // VERIFY RETURNED TO PARENT STRUCTURE
            // =================================================

            string parentControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            140
                        )
                );

            Log(
                parentControls
            );

            if (DialogAgent07B_IsFailure(
                    parentControls))
            {
                DialogAgent07B_Fail(
                    "Independent parent-window inspection"
                );

                return;
            }

            if (
                !parentControls.Contains(
                    "Open Review Dialog",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !parentControls.Contains(
                    "Workflow Status",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                DialogAgent07B_Fail(
                    "Independent return-to-parent verification"
                );

                return;
            }

            Log(
                "PASS: Parent workflow window independently verified."
            );

            // =================================================
            // READ ACTUAL WORKFLOW STATUS
            // =================================================

            string finalStatus =
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
                finalStatus
            );

            if (DialogAgent07B_IsFailure(
                    finalStatus))
            {
                DialogAgent07B_Fail(
                    "Independent Workflow Status read"
                );

                return;
            }

            bool resultPassed =
                finalStatus.Contains(
                    "Result: applied",
                    StringComparison.OrdinalIgnoreCase
                );

            bool referencePassed =
                finalStatus.Contains(
                    "Reference=AUTON-07B-002",
                    StringComparison.Ordinal
                );

            bool confirmationPassed =
                finalStatus.Contains(
                    "Confirmed=True",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!resultPassed)
            {
                Log(
                    "Expected Workflow Status to contain: Result: applied"
                );
            }

            if (!referencePassed)
            {
                Log(
                    "Expected Workflow Status to contain: Reference=AUTON-07B-002"
                );
            }

            if (!confirmationPassed)
            {
                Log(
                    "Expected Workflow Status to contain: Confirmed=True"
                );
            }

            if (
                !resultPassed
                ||
                !referencePassed
                ||
                !confirmationPassed
            )
            {
                DialogAgent07B_Fail(
                    "Final autonomous workflow state verification"
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
                "SUCCESS: VERSION 0.7B AUTONOMOUS DIALOG WORKFLOW TEST PASSED."
            );

            Log(
                "Agent parent-window inspection: PASS"
            );

            Log(
                "Agent native menu discovery: PASS"
            );

            Log(
                "Agent native menu invocation: PASS"
            );

            Log(
                "Agent modal-window transition: PASS"
            );

            Log(
                "Agent dialog control inspection: PASS"
            );

            Log(
                "Agent dialog TextBox ValuePattern: PASS"
            );

            Log(
                "Agent dialog TextBox verification: PASS"
            );

            Log(
                "Agent dialog CheckBox TogglePattern: PASS"
            );

            Log(
                "Agent dialog CheckBox verification: PASS"
            );

            Log(
                "Agent Apply Review InvokePattern: PASS"
            );

            Log(
                "Agent return-to-parent handling: PASS"
            );

            Log(
                "Agent Workflow Status verification: PASS"
            );

            Log(
                "Native-only tool enforcement: PASS"
            );

            Log(
                "Independent Windows UI verification: PASS"
            );

            Log(
                "VERSION 0.7B: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (OperationCanceledException)
        {
            DialogAgent07B_Fail(
                "Autonomous workflow timed out or was cancelled"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7B AUTONOMOUS DIALOG TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void DialogAgent07B_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7B AUTONOMOUS DIALOG WORKFLOW TEST - {testName}"
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

    private static bool DialogAgent07B_IsFailure(
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