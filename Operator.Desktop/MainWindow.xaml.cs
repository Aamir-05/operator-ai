using System.Windows;
using Operator.Tools;
using Operator.AI;

namespace Operator.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TaskBox.Text =
            "Open Notepad, type exactly: Operator AI keyboard test, then press CTRL+S.";

        Log("Operator AI started.");
        Log("Status: Ready.");
    }
    private async void UiTypeTest_Click(
    object sender,
    RoutedEventArgs e)
    {
        Log("Starting Windows UI test...");

        string openResult =
            WindowsTools.OpenApplication(
                "notepad"
            );

        Log(openResult);

        await Task.Delay(1200);

        string windows =
            WindowsUiTools.ListWindows();

        Log(windows);

        string focusResult =
            WindowsUiTools.FocusWindow(
                "Notepad"
            );

        Log(focusResult);

        string typeResult =
            WindowsUiTools.TypeText(
                "Notepad",
                "Operator AI can control Windows UI."
            );

        Log(typeResult);
    }
    private async void SaveKeyTest_Click(
    object sender,
    RoutedEventArgs e)
    {
        Log("Testing keyboard control...");

        string openResult =
            WindowsTools.OpenApplication(
                "notepad"
            );

        Log(openResult);

        await Task.Delay(1200);

        string focusResult =
            WindowsUiTools.FocusWindow(
                "Notepad"
            );

        Log(focusResult);

        string typeResult =
            WindowsUiTools.TypeText(
                "Notepad",
                "Keyboard automation test"
            );

        Log(typeResult);

        await Task.Delay(500);

        string keyResult =
            WindowsInputTools.PressKey(
                "CTRL+S"
            );

        Log(keyResult);
    }

    private async void AskAI_Click(
        object sender,
        RoutedEventArgs e)
    {
        string task =
            TaskBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(task))
        {
            Log("Please enter a task.");
            return;
        }

        try
        {
            AskAIButton.IsEnabled = false;

            Log("--------------------------------");
            Log($"TASK: {task}");
            Log("Starting autonomous agent...");

            OperatorAgent agent =
                new OperatorAgent();

            string result =
                await agent.RunAsync(
                    task,
                    message =>
                        Dispatcher.Invoke(
                            () => Log(message)
                        )
                );

            Log($"AI: {result}");
            Log("Task finished.");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
        finally
        {
            AskAIButton.IsEnabled = true;
        }
    }

    private void OpenNotepad_Click(
        object sender,
        RoutedEventArgs e)
    {
        Log("Opening Notepad...");

        string result =
            WindowsTools.OpenApplication("notepad");

        Log(result);
    }

    private void CreateFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        Log("Creating test.txt...");

        string result =
            WindowsTools.CreateDesktopFile(
                "test.txt",
                "Hello Aamir"
            );

        Log(result);
    }

    private void VerifyFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        Log("Verifying test.txt...");

        string result =
            WindowsTools.DesktopFileExists(
                "test.txt"
            );

        Log(result);
    }

    private void Log(string message)
    {
        LogBox.AppendText(
            $"[{DateTime.Now:HH:mm:ss}] {message}\n"
        );

        LogBox.ScrollToEnd();
    }
}