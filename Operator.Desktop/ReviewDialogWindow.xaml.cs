using System.Windows;

namespace Operator.Desktop;

public partial class ReviewDialogWindow : Window
{
    public string ReferenceCode
    {
        get;
        private set;
    } = "";

    public bool IsConfirmed
    {
        get;
        private set;
    }

    public ReviewDialogWindow()
    {
        InitializeComponent();
    }

    // =========================================================
    // APPLY
    // =========================================================

    private void ApplyReview_Click(
        object sender,
        RoutedEventArgs e)
    {
        string reference =
            ReferenceCodeBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(
                reference))
        {
            DialogStatusBox.Text =
                "Validation: reference code is required";

            return;
        }

        if (ConfirmReviewCheckBox.IsChecked != true)
        {
            DialogStatusBox.Text =
                "Validation: confirmation is required";

            return;
        }

        ReferenceCode =
            reference;

        IsConfirmed =
            true;

        DialogStatusBox.Text =
            "Validation: accepted";

        DialogResult =
            true;
    }

    // =========================================================
    // CANCEL
    // =========================================================

    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        ReferenceCode =
            "";

        IsConfirmed =
            false;

        DialogResult =
            false;
    }
}