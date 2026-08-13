using Operator.AI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Operator.Desktop;

public partial class MainWindow
{
    private RemoteAgentService? _remoteService;
    private Forms.NotifyIcon? _trayIcon;
    private bool _allowExit;

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ConfigureTrayIcon();

        if (!OperatorSecrets.HasOpenAiApiKey())
        {
            SetupWindow setup = new() { Owner = this };
            setup.ShowDialog();
        }

        StartRemoteServiceIfConfigured();

        bool backgroundStart = Environment.GetCommandLineArgs().Any(
            argument => argument.Equals("--background", StringComparison.OrdinalIgnoreCase));

        if (backgroundStart)
        {
            WindowState = WindowState.Minimized;
            Hide();
        }

        await RefreshRemoteStatusAsync();
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowExit)
        {
            e.Cancel = true;
            Hide();

            _trayIcon?.ShowBalloonTip(
                2500,
                "Operator AI",
                "Operator AI is still running in the background for mobile commands.",
                Forms.ToolTipIcon.Info);

            return;
        }

        if (_remoteService != null)
            await _remoteService.StopAsync();

        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void SetupButton_Click(object sender, RoutedEventArgs e)
    {
        SetupWindow setup = new() { Owner = this };

        if (setup.ShowDialog() == true)
        {
            StartRemoteServiceIfConfigured();
            _ = RefreshRemoteStatusAsync();
        }
    }

    private void PairMobileButton_Click(object sender, RoutedEventArgs e)
    {
        RemoteSettings settings = RemoteSettings.Load();

        if (string.IsNullOrWhiteSpace(settings.ProjectUrl))
        {
            MessageBox.Show(
                this,
                "Configure Operator Cloud in Setup first.",
                "Operator AI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        PairingWindow pairing = new() { Owner = this };
        pairing.ShowDialog();
        StartRemoteServiceIfConfigured();
        _ = RefreshRemoteStatusAsync();
    }

    private async void RemoteToggleButton_Click(object sender, RoutedEventArgs e)
    {
        RemoteSettings settings = RemoteSettings.Load();
        settings.Enabled = !settings.Enabled;
        settings.Save();

        if (settings.Enabled)
            StartRemoteServiceIfConfigured();
        else if (_remoteService != null)
            await _remoteService.StopAsync();

        await RefreshRemoteStatusAsync();
    }

    private void ConfigureTrayIcon()
    {
        if (_trayIcon != null) return;

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Operator AI 1.0",
            Icon = SystemIcons.Application,
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);

        Forms.ContextMenuStrip menu = new();
        Forms.ToolStripMenuItem open = new("Open Operator AI");
        open.Click += (_, _) => Dispatcher.Invoke(ShowFromTray);

        Forms.ToolStripMenuItem exit = new("Exit");
        exit.Click += (_, _) => Dispatcher.Invoke(() =>
        {
            _allowExit = true;
            Close();
        });

        menu.Items.Add(open);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exit);
        _trayIcon.ContextMenuStrip = menu;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void StartRemoteServiceIfConfigured()
    {
        RemoteSettings settings = RemoteSettings.Load();

        if (!settings.Enabled
            || string.IsNullOrWhiteSpace(settings.ProjectUrl)
            || string.IsNullOrWhiteSpace(settings.DeviceId)
            || string.IsNullOrWhiteSpace(OperatorSecrets.GetDeviceSecret(settings.DeviceId)))
        {
            _ = RefreshRemoteStatusAsync();
            return;
        }

        if (_remoteService != null && _remoteService.IsRunning)
            return;

        _remoteService = new RemoteAgentService(async message =>
        {
            await Dispatcher.InvokeAsync(() => Log(message));
        });

        _remoteService.StatusChanged += status =>
        {
            Dispatcher.BeginInvoke(() => RemoteStatusText.Text = status);
        };

        _remoteService.Start();
    }

    private Task RefreshRemoteStatusAsync()
    {
        RemoteSettings settings = RemoteSettings.Load();

        string status = !settings.Enabled
            ? "Remote disabled"
            : string.IsNullOrWhiteSpace(settings.ProjectUrl)
                ? "Cloud not configured"
                : string.IsNullOrWhiteSpace(settings.DeviceId)
                    ? "Not paired"
                    : string.IsNullOrWhiteSpace(OperatorSecrets.GetDeviceSecret(settings.DeviceId))
                        ? "Pairing credential missing"
                        : _remoteService?.IsRunning == true
                            ? "Remote online"
                            : "Remote ready";

        RemoteStatusText.Text = status;
        RemoteToggleButton.Content = settings.Enabled ? "Disable Remote" : "Enable Remote";
        PairedDeviceText.Text = string.IsNullOrWhiteSpace(settings.DeviceId)
            ? "No mobile device paired"
            : $"Device ID: {settings.DeviceId}";

        return Task.CompletedTask;
    }
}
