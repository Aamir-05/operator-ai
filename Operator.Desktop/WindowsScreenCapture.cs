using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Operator.Desktop;

public static class WindowsScreenCapture
{
    public static byte[] CapturePrimaryScreenJpeg(long quality = 65L)
    {
        Screen screen = Screen.PrimaryScreen ?? throw new InvalidOperationException("No primary screen was found.");
        Rectangle bounds = screen.Bounds;

        using Bitmap bitmap = new(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        using MemoryStream stream = new();
        ImageCodecInfo? codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == ImageFormat.Jpeg.Guid);

        if (codec == null)
        {
            bitmap.Save(stream, ImageFormat.Jpeg);
            return stream.ToArray();
        }

        using EncoderParameters parameters = new(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, Math.Clamp(quality, 25L, 90L));
        bitmap.Save(stream, codec, parameters);
        return stream.ToArray();
    }
}
