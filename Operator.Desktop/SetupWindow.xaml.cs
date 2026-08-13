using Operator.AI;
using System;
using System.Windows;

namespace Operator.Desktop;

public partial class SetupWindow : Window
{
    private readonly RemoteSettings _settings;

    public SetupWindow()
    {
        InitializeComponent();
        _settings = RemoteSettings.Load();

        ProjectUrlBox.Text = _settings.ProjectUrl;
        DeviceNameBox.Text = _settings.DeviceName;
        RemoteEnabledCheckBox.IsChecked = _settings.Enabled;
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
        ScreenshotCheckBox.IsChecked = _settings.CaptureScreenshotAfterRemoteTask;

        if (OperatorSecrets.HasOpenAiApiKey())
            ApiKeyBox.Password = "********";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string enteredKey = ApiKeyBox.Password.Trim();

            if (enteredKey != "********" && !string.IsNullOrWhiteSpace(enteredKey))
                OperatorSecrets.SaveOpenAiApiKey(enteredKey);

            if (!OperatorSecrets.HasOpenAiApiKey())
            {
                MessageBox.Show(this, "Enter your OpenAI API key.", "Operator AI Setup",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _settings.ProjectUrl = ProjectUrlBox.Text.Trim().TrimEnd('/');
            _settings.DeviceName = string.IsNullOrWhiteSpace(DeviceNameBox.Text)
                ? Environment.MachineName
                : DeviceNameBox.Text.Trim();
            _settings.Enabled = RemoteEnabledCheckBox.IsChecked == true;
            _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
            _settings.CaptureScreenshotAfterRemoteTask = ScreenshotCheckBox.IsChecked == true;
            _settings.Save();

            StartupRegistration.Apply(_settings.StartWithWindows);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Operator AI Setup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
