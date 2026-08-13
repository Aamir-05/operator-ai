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
    // VERSION 0.7C-2
    // AUTONOMOUS MULTI-WINDOW ORCHESTRATION
    // =========================================================

    private async void MultiWindowAgent07CTest_Click(
        object sender,
        RoutedEventArgs e)
    {
        MultiWindowSourceTestWindow?
            sourceWindow = null;

        MultiWindowDestinationTestWindow?
            destinationWindow = null;

        const string sourceTitle =
            "Operator AI Source Application";

        const string destinationTitle =
            "Operator AI Destination Application";

        const string expectedValue =
            "OPS-TRANSFER-07C-001";

        object actionLock =
            new object();

        HashSet<string> actions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7C AUTONOMOUS MULTI-WINDOW TEST"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // OPEN CONTROLLED SOURCE + DESTINATION
            // =================================================

            sourceWindow =
                new MultiWindowSourceTestWindow
                {
                    Owner = this,
                    Left = 80,
                    Top = 100
                };

            destinationWindow =
                new MultiWindowDestinationTestWindow
                {
                    Owner = this,
                    Left = 800,
                    Top = 100
                };

            sourceWindow.Show();

            destinationWindow.Show();

            await Task.Delay(
                800
            );

            // =================================================
            // PRE-TEST DISCOVERY
            // =================================================

            Log(
                "Verifying both test applications before agent start..."
            );

            string initialWindows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                initialWindows
            );

            if (
                MultiWindowAgent07C_IsFailure(
                    initialWindows)
                ||
                !initialWindows.Contains(
                    sourceTitle,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !initialWindows.Contains(
                    destinationTitle,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                MultiWindowAgent07C_Fail(
                    "Initial multi-window discovery"
                );

                return;
            }

            Log(
                "PASS: Both autonomous-test windows are available."
            );

            // =================================================
            // RUN AUTONOMOUS AGENT
            // =================================================

            MultiWindowAgent07C agent =
                new MultiWindowAgent07C();

            using CancellationTokenSource timeout =
                new CancellationTokenSource(
                    TimeSpan.FromMinutes(4)
                );

            string result =
                await agent.RunAsync(
                    $"""
                    There are two controlled Windows applications open.

                    SOURCE WINDOW TITLE:
                    {sourceTitle}

                    DESTINATION WINDOW TITLE:
                    {destinationTitle}

                    Perform this exact multi-application workflow.

                    1. List the available top-level Windows and verify both
                       source and destination windows exist.

                    2. Focus:

                       {sourceTitle}

                    3. Verify the source window is actually foreground.

                    4. Inspect its native controls.

                    5. Read the native Edit control named:

                       Source Value

                    Remember its exact value. Do not invent it.

                    6. Focus:

                       {destinationTitle}

                    7. Verify the destination window is foreground.

                    8. Inspect its native controls.

                    9. Put the exact value read from Source Value into the
                       native Edit control named:

                       Destination Value

                    10. Read Destination Value back and verify it exactly
                        matches the source value.

                    11. Set the native checkbox:

                        Verify transfer

                        to ON.

                    12. Verify Verify transfer is ON.

                    13. Inspect the native button:

                        Apply Transfer

                    14. Activate Apply Transfer using its native Windows UI
                        Automation pattern.

                    15. Read:

                        Destination Status

                    Verify it contains:

                        Result: accepted
                        Value={expectedValue}
                        Verified=True

                    16. Switch back to:

                        {sourceTitle}

                    17. Verify it became foreground.

                    18. Read Source Value again and verify it is still:

                        {expectedValue}

                    19. Switch again to:

                        {destinationTitle}

                    20. Verify it became foreground.

                    21. Read Destination Status again and verify the accepted
                        result still exists.

                    Do not use keyboard automation.
                    Do not use browser automation.
                    Do not use mouse coordinates.

                    Do not claim completion until the final destination state
                    has been read and verified.
                    """,
                    message =>
                    {
                        lock (actionLock)
                        {
                            Track07CAction(
                                actions,
                                message,
                                "windows_list_windows"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_focus_window"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_verify_foreground"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_list_controls"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_get_control_value"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_set_control_value"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_set_toggle"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_get_toggle"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_get_control_info"
                            );

                            Track07CAction(
                                actions,
                                message,
                                "windows_click_control"
                            );
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

            if (MultiWindowAgent07C_IsFailure(
                    result))
            {
                MultiWindowAgent07C_Fail(
                    "Autonomous multi-window agent execution"
                );

                return;
            }

            // =================================================
            // ENSURE IMPORTANT TOOL CATEGORIES WERE USED
            // =================================================

            string[] requiredActions =
            [
                "windows_list_windows",
                "windows_focus_window",
                "windows_verify_foreground",
                "windows_list_controls",
                "windows_get_control_value",
                "windows_set_control_value",
                "windows_set_toggle",
                "windows_get_toggle",
                "windows_get_control_info",
                "windows_click_control"
            ];

            lock (actionLock)
            {
                foreach (
                    string requiredAction
                    in requiredActions)
                {
                    if (!actions.Contains(
                            requiredAction))
                    {
                        MultiWindowAgent07C_Fail(
                            $"Agent did not exercise {requiredAction}"
                        );

                        return;
                    }
                }
            }

            Log(
                "PASS: Required autonomous window/control tools were exercised."
            );

            // =================================================
            // INDEPENDENT VERIFICATION
            //
            // Do not trust the model's final statement.
            // =================================================

            Log(
                "Independently verifying source application..."
            );

            string sourceFocus =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            sourceTitle
                        )
                );

            Log(
                sourceFocus
            );

            if (MultiWindowAgent07C_IsFailure(
                    sourceFocus))
            {
                MultiWindowAgent07C_Fail(
                    "Independent source focus"
                );

                return;
            }

            string sourceForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            sourceTitle
                        )
                );

            Log(
                sourceForeground
            );

            if (MultiWindowAgent07C_IsFailure(
                    sourceForeground))
            {
                MultiWindowAgent07C_Fail(
                    "Independent source foreground verification"
                );

                return;
            }

            string sourceValue =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Source Value",
                            true
                        )
                );

            Log(
                sourceValue
            );

            if (!sourceValue.Contains(
                    expectedValue,
                    StringComparison.Ordinal))
            {
                MultiWindowAgent07C_Fail(
                    "Independent source value verification"
                );

                return;
            }

            Log(
                "PASS: Source application independently verified."
            );

            // =================================================
            // DESTINATION VERIFICATION
            // =================================================

            Log(
                "Independently verifying destination application..."
            );

            string destinationFocus =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            destinationTitle
                        )
                );

            Log(
                destinationFocus
            );

            if (MultiWindowAgent07C_IsFailure(
                    destinationFocus))
            {
                MultiWindowAgent07C_Fail(
                    "Independent destination focus"
                );

                return;
            }

            string destinationForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            destinationTitle
                        )
                );

            Log(
                destinationForeground
            );

            if (MultiWindowAgent07C_IsFailure(
                    destinationForeground))
            {
                MultiWindowAgent07C_Fail(
                    "Independent destination foreground verification"
                );

                return;
            }

            string destinationValue =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Destination Value",
                            true
                        )
                );

            Log(
                destinationValue
            );

            if (!destinationValue.Contains(
                    expectedValue,
                    StringComparison.Ordinal))
            {
                MultiWindowAgent07C_Fail(
                    "Independent destination value verification"
                );

                return;
            }

            string toggleState =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetToggleState(
                            "__FOREGROUND__",
                            "checkbox",
                            "Verify transfer",
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
                MultiWindowAgent07C_Fail(
                    "Independent transfer checkbox verification"
                );

                return;
            }

            string destinationStatus =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Destination Status",
                            true
                        )
                );

            Log(
                destinationStatus
            );

            bool accepted =
                destinationStatus.Contains(
                    "Result: accepted",
                    StringComparison.OrdinalIgnoreCase
                );

            bool correctValue =
                destinationStatus.Contains(
                    $"Value={expectedValue}",
                    StringComparison.Ordinal
                );

            bool verified =
                destinationStatus.Contains(
                    "Verified=True",
                    StringComparison.OrdinalIgnoreCase
                );

            if (
                !accepted
                ||
                !correctValue
                ||
                !verified
            )
            {
                MultiWindowAgent07C_Fail(
                    "Independent destination state verification"
                );

                return;
            }

            Log(
                "PASS: Destination application independently verified."
            );

            // =================================================
            // FINAL WINDOW DISCOVERY
            // =================================================

            string finalWindows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                finalWindows
            );

            if (
                MultiWindowAgent07C_IsFailure(
                    finalWindows)
                ||
                !finalWindows.Contains(
                    sourceTitle,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !finalWindows.Contains(
                    destinationTitle,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                MultiWindowAgent07C_Fail(
                    "Final independent window discovery"
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
                "SUCCESS: VERSION 0.7C AUTONOMOUS MULTI-WINDOW TEST PASSED."
            );

            Log(
                "Agent Win32 window discovery: PASS"
            );

            Log(
                "Agent source-window switching: PASS"
            );

            Log(
                "Agent source foreground verification: PASS"
            );

            Log(
                "Agent source data read: PASS"
            );

            Log(
                "Agent destination-window switching: PASS"
            );

            Log(
                "Agent destination foreground verification: PASS"
            );

            Log(
                "Agent destination control inspection: PASS"
            );

            Log(
                "Agent cross-window data transfer: PASS"
            );

            Log(
                "Agent destination ValuePattern verification: PASS"
            );

            Log(
                "Agent destination TogglePattern: PASS"
            );

            Log(
                "Agent destination InvokePattern: PASS"
            );

            Log(
                "Agent destination result verification: PASS"
            );

            Log(
                "Agent return-to-source workflow: PASS"
            );

            Log(
                "Agent source-state verification: PASS"
            );

            Log(
                "Agent return-to-destination workflow: PASS"
            );

            Log(
                "Agent destination-state persistence: PASS"
            );

            Log(
                "Independent source verification: PASS"
            );

            Log(
                "Independent destination verification: PASS"
            );

            Log(
                "VERSION 0.7C: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (OperationCanceledException)
        {
            MultiWindowAgent07C_Fail(
                "Autonomous multi-window test timed out"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7C AUTONOMOUS MULTI-WINDOW TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // TRACK ACTION
    // =========================================================

    private static void Track07CAction(
        HashSet<string> actions,
        string message,
        string actionName)
    {
        if (message.Contains(
                $"[ACTION] {actionName}",
                StringComparison.OrdinalIgnoreCase))
        {
            actions.Add(
                actionName
            );
        }
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void MultiWindowAgent07C_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7C AUTONOMOUS MULTI-WINDOW TEST - {testName}"
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

    private static bool MultiWindowAgent07C_IsFailure(
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