using System;
using System.Windows;

namespace Operator.Desktop;

public partial class DialogWorkflowTestWindow : Window
{
    public DialogWorkflowTestWindow()
    {
        InitializeComponent();
    }

    // =========================================================
    // MENU COMMAND
    //
    // We schedule the modal dialog through Dispatcher so the
    // UI Automation InvokePattern operation can return before
    // ShowDialog enters its modal message loop.
    // =========================================================

    private void OpenReviewDialogMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(
            new Action(
                OpenReviewDialog
            )
        );
    }

    // =========================================================
    // OPEN MODAL REVIEW DIALOG
    // =========================================================

    private void OpenReviewDialog()
    {
        ReviewDialogWindow dialog =
            new ReviewDialogWindow
            {
                Owner = this
            };

        bool? result =
            dialog.ShowDialog();

        if (result == true)
        {
            WorkflowStatusBox.Text =
                "Result: applied" +
                $" | Reference={dialog.ReferenceCode}" +
                $" | Confirmed={dialog.IsConfirmed}";
        }
        else
        {
            WorkflowStatusBox.Text =
                "Result: cancelled";
        }
    }
}