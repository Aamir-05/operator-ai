using Operator.AI;
using QRCoder;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Operator.Desktop;

public partial class PairingWindow : Window
{
    private readonly RemoteSettings _settings;
    private CancellationTokenSource? _cancellation;

    public PairingWindow()
    {
        InitializeComponent();
        _settings = RemoteSettings.Load();
        Loaded += PairingWindow_Loaded;
        Closed += PairingWindow_Closed;
    }

    private async void PairingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_settings.ProjectUrl))
        {
            PairingStatusText.Text = "Configure the Operator Cloud URL in Setup first.";
            return;
        }

        _cancellation = new CancellationTokenSource();

        try
        {
            RemoteApiClient api = new(_settings);
            PairStartResponse session = await api.StartPairingAsync(_cancellation.Token);

            PairCodeText.Text = session.Code;
            PairUriText.Text = session.PairUri;
            QrImage.Source = CreateQrImage(session.PairUri);
            PairingStatusText.Text = "Waiting for Operator AI Mobile to claim this PC...";

            while (!_cancellation.IsCancellationRequested)
            {
                PairPollResponse poll = await api.PollPairingAsync(
                    session.SessionId,
                    session.PollToken,
                    _cancellation.Token);

                if (poll.Status.Equals("paired", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(poll.DeviceId) || string.IsNullOrWhiteSpace(poll.DeviceSecret))
                        throw new InvalidOperationException("Pairing completed without a device credential.");

                    _settings.DeviceId = poll.DeviceId;
                    _settings.Enabled = true;
                    _settings.Save();
                    OperatorSecrets.SaveDeviceSecret(poll.DeviceId, poll.DeviceSecret);

                    PairingStatusText.Text = "Paired successfully. Operator AI Mobile can now send tasks to this PC.";
                    PairCodeText.Text = "PAIRED";
                    return;
                }

                if (poll.Status.Equals("expired", StringComparison.OrdinalIgnoreCase))
                {
                    PairingStatusText.Text = "Pairing session expired. Close this window and try again.";
                    return;
                }

                await Task.Delay(1500, _cancellation.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            PairingStatusText.Text = "Pairing error: " + ex.Message;
        }
    }

    private void PairingWindow_Closed(object? sender, EventArgs e)
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private static BitmapImage CreateQrImage(string content)
    {
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        PngByteQRCode qr = new(data);
        byte[] png = qr.GetGraphic(10);

        BitmapImage image = new();
        using MemoryStream stream = new(png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
