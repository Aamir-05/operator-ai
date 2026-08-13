using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Operator.AI;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7A-4
    // AUTONOMOUS NATIVE WINDOWS CONTROL TEST
    // =========================================================

    private async void NativeAgent07ATest_Click(
        object sender,
        RoutedEventArgs e)
    {
        NativeControlTestWindow?
            testWindow = null;

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7A AUTONOMOUS NATIVE CONTROL TEST"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // OPEN CONTROLLED TEST WINDOW
            // =================================================

            Log(
                "Opening deterministic native control test window..."
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
                700
            );

            // =================================================
            // VERIFY WINDOW IS ACTIVE
            // =================================================

            testWindow.Activate();

            await Task.Delay(
                250
            );

            string initialControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            120
                        )
                );

            Log(
                initialControls
            );

            if (NativeAgent07A_IsFailure(
                    initialControls))
            {
                NativeAgent07A_Fail(
                    "Initial native window inspection"
                );

                return;
            }

            bool correctWindow =
                initialControls.Contains(
                    "Operator Name",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                initialControls.Contains(
                    "Enable automation",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                initialControls.Contains(
                    "Apply Changes",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!correctWindow)
            {
                NativeAgent07A_Fail(
                    "Native test window identity verification"
                );

                return;
            }

            Log(
                "PASS: Controlled native test window is active."
            );

            // =================================================
            // RUN AUTONOMOUS AGENT
            // =================================================

            Log(
                "Starting Operator AI autonomous native-control task..."
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
                    Work only in the currently active native Windows test window.

                    The active window is the Operator AI Native Control Test.

                    Use native Windows UI Automation tools, not browser tools,
                    mouse coordinates, or keyboard guessing.

                    Complete these actions:

                    1. Inspect the controls in the active window.

                    2. Set the native textbox named "Operator Name" to exactly:

                    Aamir Autonomous 0.7A

                    3. Verify the textbox value.

                    4. Set the native checkbox named "Enable automation" to ON.

                    5. Verify that the checkbox is ON.

                    6. Select the native tab item named "Operations".

                    7. Inspect the native button named "Apply Changes".

                    8. Activate "Apply Changes" using its native UI Automation
                       pattern.

                    9. Read the native control named "Test Status".

                    10. Verify that Test Status contains all of these:

                    Name=Aamir Autonomous 0.7A
                    Automation=Enabled
                    Tab=Operations

                    Do not change the Department field.

                    Do not use keyboard shortcuts.

                    Do not use screen coordinates.

                    Do not use browser tools.

                    Do not claim success unless the final Test Status value has
                    been read and verified.
                    """,
                    message =>
                        Dispatcher.Invoke(
                            () =>
                                Log(
                                    $"[AGENT] {message}"
                                )
                        ),
                    timeout.Token
                );

            Log(
                $"Agent result: {result}"
            );

            if (NativeAgent07A_IsFailure(
                    result))
            {
                NativeAgent07A_Fail(
                    "Autonomous agent execution"
                );

                return;
            }

            // =================================================
            // INDEPENDENT VERIFICATION
            //
            // Do not trust the agent's final text alone.
            // Read actual Windows UI state directly.
            // =================================================

            Log(
                "Independently verifying native UI state..."
            );

            testWindow.Activate();

            await Task.Delay(
                200
            );

            string nameValue =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Operator Name",
                            true
                        )
                );

            Log(
                nameValue
            );

            if (!nameValue.Contains(
                    "Aamir Autonomous 0.7A",
                    StringComparison.Ordinal))
            {
                NativeAgent07A_Fail(
                    "Operator Name verification"
                );

                return;
            }

            Log(
                "PASS: Operator Name verified."
            );

            // =================================================
            // VERIFY CHECKBOX
            // =================================================

            string toggleState =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetToggleState(
                            "__FOREGROUND__",
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
                NativeAgent07A_Fail(
                    "Enable automation verification"
                );

                return;
            }

            Log(
                "PASS: Enable automation verified."
            );

            // =================================================
            // VERIFY FINAL STATUS
            // =================================================

            string finalStatus =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Test Status",
                            true
                        )
                );

            Log(
                finalStatus
            );

            if (NativeAgent07A_IsFailure(
                    finalStatus))
            {
                NativeAgent07A_Fail(
                    "Final Test Status read"
                );

                return;
            }

            bool namePassed =
                finalStatus.Contains(
                    "Name=Aamir Autonomous 0.7A",
                    StringComparison.Ordinal
                );

            bool automationPassed =
                finalStatus.Contains(
                    "Automation=Enabled",
                    StringComparison.Ordinal
                );

            bool departmentPassed =
                finalStatus.Contains(
                    "Department=Finance",
                    StringComparison.Ordinal
                );

            bool tabPassed =
                finalStatus.Contains(
                    "Tab=Operations",
                    StringComparison.Ordinal
                );

            if (!namePassed)
            {
                Log(
                    "Expected final status: Name=Aamir Autonomous 0.7A"
                );
            }

            if (!automationPassed)
            {
                Log(
                    "Expected final status: Automation=Enabled"
                );
            }

            if (!departmentPassed)
            {
                Log(
                    "Expected Department to remain Finance."
                );
            }

            if (!tabPassed)
            {
                Log(
                    "Expected final status: Tab=Operations"
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
                NativeAgent07A_Fail(
                    "Final autonomous native state verification"
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
                "SUCCESS: VERSION 0.7A AUTONOMOUS NATIVE CONTROL TEST PASSED."
            );

            Log(
                "Agent native control inspection: PASS"
            );

            Log(
                "Agent TextBox targeting: PASS"
            );

            Log(
                "Agent TextBox ValuePattern: PASS"
            );

            Log(
                "Agent TextBox verification: PASS"
            );

            Log(
                "Agent CheckBox TogglePattern: PASS"
            );

            Log(
                "Agent CheckBox verification: PASS"
            );

            Log(
                "Agent Tab SelectionItemPattern: PASS"
            );

            Log(
                "Agent Button inspection: PASS"
            );

            Log(
                "Agent Button InvokePattern: PASS"
            );

            Log(
                "Agent final Test Status read: PASS"
            );

            Log(
                "Independent native state verification: PASS"
            );

            Log(
                "VERSION 0.7A: COMPLETE"
            );

            Log(
                "============================================================"
            );
        }
        catch (OperationCanceledException)
        {
            NativeAgent07A_Fail(
                "Autonomous test timed out or was cancelled"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7A AUTONOMOUS NATIVE TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void NativeAgent07A_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7A AUTONOMOUS NATIVE CONTROL TEST - {testName}"
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

    private static bool NativeAgent07A_IsFailure(
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