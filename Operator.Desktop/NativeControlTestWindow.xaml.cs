using System.Windows;
using System.Windows.Controls;

namespace Operator.Desktop;

public partial class NativeControlTestWindow : Window
{
    public NativeControlTestWindow()
    {
        InitializeComponent();
    }

    // =========================================================
    // APPLY CHANGES
    // =========================================================

    private void ApplyChanges_Click(
        object sender,
        RoutedEventArgs e)
    {
        string operatorName =
            OperatorNameBox.Text;

        string automationState =
            EnableAutomationCheckBox.IsChecked == true
                ? "Enabled"
                : "Disabled";

        string department =
            GetSelectedDepartment();

        string selectedTab =
            GetSelectedTab();

        StatusBox.Text =
            "Result: applied" +
            $" | Name={operatorName}" +
            $" | Automation={automationState}" +
            $" | Department={department}" +
            $" | Tab={selectedTab}";
    }

    // =========================================================
    // DEPARTMENT
    // =========================================================

    private string GetSelectedDepartment()
    {
        if (
            DepartmentComboBox.SelectedItem
            is ComboBoxItem item
        )
        {
            return
                item.Content?.ToString()
                ?? "";
        }

        return "";
    }

    // =========================================================
    // TAB
    // =========================================================

    private string GetSelectedTab()
    {
        if (
            WorkspaceTabs.SelectedItem
            is TabItem item
        )
        {
            return
                item.Header?.ToString()
                ?? "";
        }

        return "";
    }
}