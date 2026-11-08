using System;
using System.IO;
using System.Linq;
using System.Windows;
using Drawing = System.Drawing;
using DrawingImaging = System.Drawing.Imaging;
using Forms = System.Windows.Forms;

namespace ScreenTools;

/// <summary>
/// Captures full-screen or selected-region screenshots and JPEG recording frames.
/// </summary>
public sealed class ScreenCaptureService : IScreenCaptureService
{
    /// <summary>
    /// Captures the configured screenshot target and writes the resulting artifact.
    /// </summary>
    public CaptureArtifact CaptureScreenshot(RecordingSessionState session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var extension = NormalizeImageExtension(session.ScreenshotFormat);
        var outputPath = CaptureOutputPathHelper.CreateScreenshotPath(session.OutputDirectory, extension);

        try
        {
            using var bitmap = CaptureVirtualScreen(out var screenBounds);
            SaveBitmap(bitmap, outputPath, extension);
            return new CaptureArtifact("截图", outputPath, 1, CaptureBounds: new Rect(screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"截图失败：无法捕获屏幕画面。{ex.Message}", ex);
        }
    }

    /// <summary>
    /// Captures and saves the selected screen rectangle.
    /// </summary>
    public CaptureArtifact CaptureScreenshotRegion(RecordingSessionState session, Rect region)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var extension = NormalizeImageExtension(session.ScreenshotFormat);
        var outputPath = CaptureOutputPathHelper.CreateScreenshotPath(session.OutputDirectory, extension);

        try
        {
            using var bitmap = CaptureRegion(region, out var capturedBounds);
            SaveBitmap(bitmap, outputPath, extension);
            return new CaptureArtifact("截图", outputPath, 1, CaptureBounds: new Rect(capturedBounds.X, capturedBounds.Y, capturedBounds.Width, capturedBounds.Height));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"区域截图失败：无法捕获屏幕区域。{ex.Message}", ex);
        }
    }

    /// <summary>
    /// Captures the current desktop as a JPEG file using the selected quality preset.
    /// </summary>
    public void CaptureFrameJpeg(string outputPath, string qualityPreset)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("输出路径不能为空。", nameof(outputPath));
        }

        try
        {
            File.WriteAllBytes(outputPath, CaptureFrameJpegBytes(qualityPreset));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"录制帧采集失败：无法写入 {outputPath}。{ex.Message}", ex);
        }
    }

    /// <summary>
    /// Captures the current desktop and returns its JPEG-encoded bytes.
    /// </summary>
    public byte[] CaptureFrameJpegBytes(string qualityPreset)
    {
        if (string.IsNullOrWhiteSpace(qualityPreset))
        {
            throw new ArgumentException("质量档位不能为空。", nameof(qualityPreset));
        }

        try
        {
            var profile = CaptureQualityProfile.FromPreset(qualityPreset);
            using var bitmap = CaptureVirtualScreen(out _);
            using var stream = new MemoryStream();
            Drawing.Bitmap? resizedBitmap = null;
            try
            {
                var encodingBitmap = bitmap;
                if (bitmap.Width > profile.MaxWidth)
                {
                    resizedBitmap = ResizeBitmap(bitmap, profile.MaxWidth);
                    encodingBitmap = resizedBitmap;
                }

                SaveJpeg(encodingBitmap, stream, profile.JpegQuality);
            }
            finally
            {
                resizedBitmap?.Dispose();
            }

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"录制帧采集失败：无法捕获或编码画面。{ex.Message}", ex);
        }
    }

    private const long MaxCaptureMemoryBytes = 500L * 1024 * 1024; // 500 MB

    private static Drawing.Bitmap CaptureVirtualScreen(out Drawing.Rectangle screenBounds)
    {
        screenBounds = GetVirtualScreenBounds();
        var estimatedMemory = (long)screenBounds.Width * screenBounds.Height * 3; // 24-bit RGB
        if (estimatedMemory > MaxCaptureMemoryBytes)
        {
            throw new InvalidOperationException($"屏幕分辨率过高（{screenBounds.Width}x{screenBounds.Height}，预计占用 {estimatedMemory / (1024 * 1024)} MB），超过内存预算 {MaxCaptureMemoryBytes / (1024 * 1024)} MB。请降低显示缩放或选择区域截图。");
        }

        return CaptureScreenArea(screenBounds.Left, screenBounds.Top, screenBounds.Width, screenBounds.Height);
    }

    private static Drawing.Bitmap CaptureRegion(Rect region, out Drawing.Rectangle capturedBounds)
    {
        var requestedBounds = DesktopCaptureGeometry.ToPixelRectangle(region);
        capturedBounds = DesktopCaptureGeometry.ClipToVirtualBounds(requestedBounds, GetVirtualScreenBounds());
        return CaptureScreenArea(capturedBounds.Left, capturedBounds.Top, capturedBounds.Width, capturedBounds.Height);
    }

    private static Drawing.Rectangle GetVirtualScreenBounds() =>
        DesktopCaptureGeometry.GetVirtualBounds(Forms.Screen.AllScreens.Select(screen => screen.Bounds));

    private static Drawing.Bitmap CaptureScreenArea(int x, int y, int width, int height)
    {
        var bitmap = new Drawing.Bitmap(width, height, DrawingImaging.PixelFormat.Format24bppRgb);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Drawing.Size(width, height), Drawing.CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Drawing.Bitmap ResizeBitmap(Drawing.Bitmap source, int maxWidth)
    {
        var ratio = (double)maxWidth / source.Width;
        var targetSize = new Drawing.Size(maxWidth, (int)Math.Round(source.Height * ratio));
        var resized = new Drawing.Bitmap(targetSize.Width, targetSize.Height, DrawingImaging.PixelFormat.Format24bppRgb);

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
        using var encoderParameters = new DrawingImaging.EncoderParameters(1);
        encoderParameters.Param[0] = new DrawingImaging.EncoderParameter(DrawingImaging.Encoder.Quality, quality);
        image.Save(outputStream, JpegEncoder, encoderParameters);
    }

    private static readonly DrawingImaging.ImageCodecInfo JpegEncoder = DrawingImaging.ImageCodecInfo.GetImageEncoders()
        .First(codec => codec.FormatID == DrawingImaging.ImageFormat.Jpeg.Guid);

    private static string NormalizeImageExtension(string format) =>
        format.Trim().ToUpperInvariant() switch
        {
            "JPG" or "JPEG" => "JPG",
            "BMP" => "BMP",
            _ => "PNG"
        };

}
