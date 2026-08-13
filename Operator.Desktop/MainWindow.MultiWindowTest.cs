using System;
using System.Threading.Tasks;
using System.Windows;
using Operator.Tools;

namespace Operator.Desktop;

public partial class MainWindow
{
    // =========================================================
    // VERSION 0.7C-1
    // ROBUST MULTI-WINDOW ORCHESTRATION FOUNDATION
    // =========================================================

    private async void MultiWindow07CTest_Click(
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

        const string expectedSourceValue =
            "OPS-TRANSFER-07C-001";

        try
        {
            Log(
                "============================================================"
            );

            Log(
                "STARTING VERSION 0.7C MULTI-WINDOW ORCHESTRATION TEST"
            );

            Log(
                "============================================================"
            );

            // =================================================
            // OPEN TWO TOP-LEVEL WINDOWS
            // =================================================

            Log(
                "Opening deterministic source and destination windows..."
            );

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
            // 1. ROBUST WIN32 WINDOW DISCOVERY
            // =================================================

            Log(
                "[1/12] Discovering top-level Windows through Win32..."
            );

            string windows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                windows
            );

            if (MultiWindow07C_IsFailure(
                    windows))
            {
                MultiWindow07C_Fail(
                    "Top-level window enumeration"
                );

                return;
            }

            bool sourceDiscovered =
                windows.Contains(
                    sourceTitle,
                    StringComparison.OrdinalIgnoreCase
                );

            bool destinationDiscovered =
                windows.Contains(
                    destinationTitle,
                    StringComparison.OrdinalIgnoreCase
                );

            if (!sourceDiscovered)
            {
                Log(
                    $"Missing window: {sourceTitle}"
                );
            }

            if (!destinationDiscovered)
            {
                Log(
                    $"Missing window: {destinationTitle}"
                );
            }

            if (
                !sourceDiscovered
                ||
                !destinationDiscovered
            )
            {
                MultiWindow07C_Fail(
                    "Top-level window discovery"
                );

                return;
            }

            Log(
                "PASS: Both top-level workflow windows discovered"
            );

            // =================================================
            // 2. SWITCH TO SOURCE BY TITLE
            // =================================================

            Log(
                "[2/12] Switching explicitly to source application..."
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

            if (MultiWindow07C_IsFailure(
                    sourceFocus))
            {
                MultiWindow07C_Fail(
                    "Source window focus"
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

            if (MultiWindow07C_IsFailure(
                    sourceForeground))
            {
                MultiWindow07C_Fail(
                    "Source foreground verification"
                );

                return;
            }

            string sourceControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            100
                        )
                );

            Log(
                sourceControls
            );

            if (
                MultiWindow07C_IsFailure(
                    sourceControls)
                ||
                !sourceControls.Contains(
                    "Source Value",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !sourceControls.Contains(
                    "Source Status",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                MultiWindow07C_Fail(
                    "Source control inspection"
                );

                return;
            }

            Log(
                "PASS: Explicit source targeting"
            );

            // =================================================
            // 3. READ SOURCE DATA
            // =================================================

            Log(
                "[3/12] Reading value from source application..."
            );

            string sourceValueResult =
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
                sourceValueResult
            );

            if (MultiWindow07C_IsFailure(
                    sourceValueResult))
            {
                MultiWindow07C_Fail(
                    "Source value read"
                );

                return;
            }

            if (!sourceValueResult.Contains(
                    expectedSourceValue,
                    StringComparison.Ordinal))
            {
                MultiWindow07C_Fail(
                    "Source value verification"
                );

                return;
            }

            Log(
                $"PASS: Source value read: {expectedSourceValue}"
            );

            // =================================================
            // 4. SWITCH TO DESTINATION BY TITLE
            // =================================================

            Log(
                "[4/12] Switching explicitly to destination application..."
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

            if (MultiWindow07C_IsFailure(
                    destinationFocus))
            {
                MultiWindow07C_Fail(
                    "Destination window focus"
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

            if (MultiWindow07C_IsFailure(
                    destinationForeground))
            {
                MultiWindow07C_Fail(
                    "Destination foreground verification"
                );

                return;
            }

            Log(
                "PASS: Destination application focused"
            );

            // =================================================
            // 5. INSPECT DESTINATION
            // =================================================

            Log(
                "[5/12] Inspecting destination controls..."
            );

            string destinationControls =
                await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            100
                        )
                );

            Log(
                destinationControls
            );

            if (MultiWindow07C_IsFailure(
                    destinationControls))
            {
                MultiWindow07C_Fail(
                    "Destination control enumeration"
                );

                return;
            }

            if (
                !destinationControls.Contains(
                    "Destination Value",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !destinationControls.Contains(
                    "Verify transfer",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !destinationControls.Contains(
                    "Apply Transfer",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                MultiWindow07C_Fail(
                    "Destination control identity verification"
                );

                return;
            }

            Log(
                "PASS: Destination controls targeted"
            );

            // =================================================
            // 6. TRANSFER VALUE
            // =================================================

            Log(
                "[6/12] Transferring source value into destination..."
            );

            string setDestination =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetControlValue(
                            "__FOREGROUND__",
                            "edit",
                            "Destination Value",
                            true,
                            expectedSourceValue
                        )
                );

            Log(
                setDestination
            );

            if (MultiWindow07C_IsFailure(
                    setDestination))
            {
                MultiWindow07C_Fail(
                    "Destination ValuePattern write"
                );

                return;
            }

            string verifyDestinationValue =
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
                verifyDestinationValue
            );

            if (!verifyDestinationValue.Contains(
                    expectedSourceValue,
                    StringComparison.Ordinal))
            {
                MultiWindow07C_Fail(
                    "Destination value verification"
                );

                return;
            }

            Log(
                "PASS: Cross-window value transfer"
            );

            // =================================================
            // 7. SET DESTINATION VERIFICATION
            // =================================================

            Log(
                "[7/12] Enabling destination verification..."
            );

            string setVerification =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetToggleState(
                            "__FOREGROUND__",
                            "checkbox",
                            "Verify transfer",
                            true,
                            true
                        )
                );

            Log(
                setVerification
            );

            if (MultiWindow07C_IsFailure(
                    setVerification))
            {
                MultiWindow07C_Fail(
                    "Destination verification toggle"
                );

                return;
            }

            string verificationState =
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
                verificationState
            );

            if (!verificationState.Contains(
                    "State: On",
                    StringComparison.OrdinalIgnoreCase))
            {
                MultiWindow07C_Fail(
                    "Destination verification state"
                );

                return;
            }

            Log(
                "PASS: Destination verification enabled"
            );

            // =================================================
            // 8. INVOKE DESTINATION ACTION
            // =================================================

            Log(
                "[8/12] Invoking Apply Transfer..."
            );

            string applyInfo =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            "__FOREGROUND__",
                            "button",
                            "Apply Transfer",
                            true
                        )
                );

            Log(
                applyInfo
            );

            if (MultiWindow07C_IsFailure(
                    applyInfo))
            {
                MultiWindow07C_Fail(
                    "Apply Transfer inspection"
                );

                return;
            }

            if (!applyInfo.Contains(
                    "Invoke: True",
                    StringComparison.OrdinalIgnoreCase))
            {
                MultiWindow07C_Fail(
                    "Apply Transfer InvokePattern"
                );

                return;
            }

            string apply =
                await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            "__FOREGROUND__",
                            "button",
                            "Apply Transfer",
                            true
                        )
                );

            Log(
                apply
            );

            if (MultiWindow07C_IsFailure(
                    apply))
            {
                MultiWindow07C_Fail(
                    "Apply Transfer invocation"
                );

                return;
            }

            await Task.Delay(
                300
            );

            Log(
                "PASS: Destination action invoked"
            );

            // =================================================
            // 9. VERIFY DESTINATION RESULT
            // =================================================

            Log(
                "[9/12] Verifying destination result..."
            );

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
                    $"Value={expectedSourceValue}",
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
                MultiWindow07C_Fail(
                    "Destination result verification"
                );

                return;
            }

            Log(
                "PASS: Destination accepted transferred value"
            );

            // =================================================
            // 10. SWITCH BACK TO SOURCE
            // =================================================

            Log(
                "[10/12] Switching back to source application..."
            );

            string returnSource =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            sourceTitle
                        )
                );

            Log(
                returnSource
            );

            if (MultiWindow07C_IsFailure(
                    returnSource))
            {
                MultiWindow07C_Fail(
                    "Return-to-source focus"
                );

                return;
            }

            string verifySourceForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            sourceTitle
                        )
                );

            Log(
                verifySourceForeground
            );

            if (MultiWindow07C_IsFailure(
                    verifySourceForeground))
            {
                MultiWindow07C_Fail(
                    "Return-to-source foreground verification"
                );

                return;
            }

            string sourceAfterTransfer =
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
                sourceAfterTransfer
            );

            if (!sourceAfterTransfer.Contains(
                    expectedSourceValue,
                    StringComparison.Ordinal))
            {
                MultiWindow07C_Fail(
                    "Source-state verification"
                );

                return;
            }

            Log(
                "PASS: Returned to source and verified original data"
            );

            // =================================================
            // 11. RETURN TO DESTINATION
            // =================================================

            Log(
                "[11/12] Returning to destination for persistence check..."
            );

            string returnDestination =
                await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            destinationTitle
                        )
                );

            Log(
                returnDestination
            );

            if (MultiWindow07C_IsFailure(
                    returnDestination))
            {
                MultiWindow07C_Fail(
                    "Return-to-destination focus"
                );

                return;
            }

            string verifyDestinationForeground =
                await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            destinationTitle
                        )
                );

            Log(
                verifyDestinationForeground
            );

            if (MultiWindow07C_IsFailure(
                    verifyDestinationForeground))
            {
                MultiWindow07C_Fail(
                    "Return-to-destination foreground verification"
                );

                return;
            }

            string finalDestinationStatus =
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
                finalDestinationStatus
            );

            if (
                !finalDestinationStatus.Contains(
                    "Result: accepted",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !finalDestinationStatus.Contains(
                    $"Value={expectedSourceValue}",
                    StringComparison.Ordinal)
                ||
                !finalDestinationStatus.Contains(
                    "Verified=True",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                MultiWindow07C_Fail(
                    "Destination-state persistence verification"
                );

                return;
            }

            Log(
                "PASS: Destination state persisted across window switches"
            );

            // =================================================
            // 12. FINAL WINDOW DISCOVERY
            // =================================================

            Log(
                "[12/12] Final independent multi-window verification..."
            );

            string finalWindows =
                await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows()
                );

            Log(
                finalWindows
            );

            if (
                MultiWindow07C_IsFailure(
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
                MultiWindow07C_Fail(
                    "Final top-level window verification"
                );

                return;
            }

            Log(
                "PASS: Both applications remain independently addressable"
            );

            // =================================================
            // SUCCESS
            // =================================================

            Log(
                "============================================================"
            );

            Log(
                "SUCCESS: VERSION 0.7C MULTI-WINDOW ORCHESTRATION TEST PASSED."
            );

            Log(
                "Win32 multi-window discovery: PASS"
            );

            Log(
                "Explicit source window targeting: PASS"
            );

            Log(
                "Source foreground verification: PASS"
            );

            Log(
                "Source data read: PASS"
            );

            Log(
                "Explicit destination window targeting: PASS"
            );

            Log(
                "Destination foreground verification: PASS"
            );

            Log(
                "Destination control inspection: PASS"
            );

            Log(
                "Cross-window data transfer: PASS"
            );

            Log(
                "Destination ValuePattern verification: PASS"
            );

            Log(
                "Destination TogglePattern: PASS"
            );

            Log(
                "Destination InvokePattern: PASS"
            );

            Log(
                "Destination result verification: PASS"
            );

            Log(
                "Return-to-source switching: PASS"
            );

            Log(
                "Source-state verification: PASS"
            );

            Log(
                "Return-to-destination switching: PASS"
            );

            Log(
                "Destination-state persistence: PASS"
            );

            Log(
                "Independent multi-window addressing: PASS"
            );

            Log(
                "============================================================"
            );
        }
        catch (Exception ex)
        {
            Log(
                $"0.7C MULTI-WINDOW TEST ERROR: {ex.Message}"
            );
        }
    }

    // =========================================================
    // FAILURE
    // =========================================================

    private void MultiWindow07C_Fail(
        string testName)
    {
        Log(
            "============================================================"
        );

        Log(
            $"FAIL: VERSION 0.7C MULTI-WINDOW TEST - {testName}"
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

    private static bool MultiWindow07C_IsFailure(
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