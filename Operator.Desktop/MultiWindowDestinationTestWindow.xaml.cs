using System.Windows;

namespace Operator.Desktop;

public partial class MultiWindowDestinationTestWindow : Window
{
    public MultiWindowDestinationTestWindow()
    {
        InitializeComponent();
    }

    // =========================================================
    // APPLY TRANSFER
    // =========================================================

    private void ApplyTransfer_Click(
        object sender,
        RoutedEventArgs e)
    {
        string value =
            DestinationValueBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(
                value))
        {
            DestinationStatusBox.Text =
                "Result: rejected | Reason=Destination value is empty";

            return;
        }

        if (VerifyTransferCheckBox.IsChecked != true)
        {
            DestinationStatusBox.Text =
                "Result: rejected | Reason=Verification not enabled";

            return;
        }

        DestinationStatusBox.Text =
            "Result: accepted" +
            $" | Value={value}" +
            " | Verified=True";
    }
}