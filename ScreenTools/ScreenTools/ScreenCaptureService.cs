using System;
using System.IO;
using System.Linq;
using System.Windows;
using Drawing = System.Drawing;
using DrawingImaging = System.Drawing.Imaging;
using Forms = System.Windows.Forms;

namespace ScreenTools;

public sealed class ScreenCaptureService
{
    public CaptureArtifact CaptureScreenshot(RecordingSessionState session)
    {
        var extension = NormalizeImageExtension(session.ScreenshotFormat);
        var outputPath = CaptureOutputPathHelper.CreateScreenshotPath(session.OutputDirectory, extension);

        using var bitmap = CaptureVirtualScreen();
        SaveBitmap(bitmap, outputPath, extension);
        return new CaptureArtifact("截图", outputPath, 1, CaptureBounds: new Rect(0, 0, bitmap.Width, bitmap.Height));
    }

    public CaptureArtifact CaptureScreenshotRegion(RecordingSessionState session, Rect region)
    {
        var extension = NormalizeImageExtension(session.ScreenshotFormat);
        var outputPath = CaptureOutputPathHelper.CreateScreenshotPath(session.OutputDirectory, extension);
        using var bitmap = CaptureRegion(region);
        SaveBitmap(bitmap, outputPath, extension);
        return new CaptureArtifact("截图", outputPath, 1, CaptureBounds: region);
    }

    public void CaptureFrameJpeg(string outputPath, string qualityPreset)
    {
        File.WriteAllBytes(outputPath, CaptureFrameJpegBytes(qualityPreset));
    }

    public byte[] CaptureFrameJpegBytes(string qualityPreset)
    {
        var profile = CaptureQualityProfile.FromPreset(qualityPreset);
        using var bitmap = CaptureVirtualScreen();
        using var scaledBitmap = ResizeBitmap(bitmap, profile.MaxWidth);
        using var stream = new MemoryStream();
        SaveJpeg(scaledBitmap, stream, profile.JpegQuality);
        return stream.ToArray();
    }

    private static Drawing.Bitmap CaptureVirtualScreen()
    {
        var screenBounds = Forms.SystemInformation.VirtualScreen;
        return CaptureScreenArea(screenBounds.Left, screenBounds.Top, screenBounds.Width, screenBounds.Height);
    }

    private static Drawing.Bitmap CaptureRegion(Rect region)
    {
        return CaptureScreenArea(
            (int)Math.Round(region.X),
            (int)Math.Round(region.Y),
            Math.Max(1, (int)Math.Round(region.Width)),
            Math.Max(1, (int)Math.Round(region.Height)));
    }

    private static Drawing.Bitmap CaptureScreenArea(int x, int y, int width, int height)
    {
        var bitmap = new Drawing.Bitmap(width, height, DrawingImaging.PixelFormat.Format32bppArgb);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Drawing.Size(width, height), Drawing.CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Drawing.Bitmap ResizeBitmap(Drawing.Bitmap source, int maxWidth)
    {
        if (source.Width <= maxWidth)
        {
            return (Drawing.Bitmap)source.Clone();
        }

        var ratio = (double)maxWidth / source.Width;
        var targetSize = new Drawing.Size(maxWidth, (int)Math.Round(source.Height * ratio));
        var resized = new Drawing.Bitmap(targetSize.Width, targetSize.Height);

        using var graphics = Drawing.Graphics.FromImage(resized);
        graphics.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Drawing.Rectangle(Drawing.Point.Empty, targetSize));

        return resized;
    }

    private static void SaveBitmap(Drawing.Bitmap bitmap, string outputPath, string format)
    {
        switch (format)
        {
            case "BMP":
                bitmap.Save(outputPath, DrawingImaging.ImageFormat.Bmp);
                break;
            case "JPG":
                using (var stream = File.Create(outputPath))
                {
                    SaveJpeg(bitmap, stream, 92L);
                }
                break;
            default:
                bitmap.Save(outputPath, DrawingImaging.ImageFormat.Png);
                break;
        }
    }

    private static void SaveJpeg(Drawing.Image image, Stream outputStream, long quality)
    {
        var jpegEncoder = DrawingImaging.ImageCodecInfo.GetImageDecoders()
            .First(codec => codec.FormatID == DrawingImaging.ImageFormat.Jpeg.Guid);
        using var encoderParameters = new DrawingImaging.EncoderParameters(1);
        encoderParameters.Param[0] = new DrawingImaging.EncoderParameter(DrawingImaging.Encoder.Quality, quality);
        image.Save(outputStream, jpegEncoder, encoderParameters);
    }

    private static string NormalizeImageExtension(string format) =>
        format.Trim().ToUpperInvariant() switch
        {
            "JPG" or "JPEG" => "JPG",
            "BMP" => "BMP",
            _ => "PNG"
        };

}
