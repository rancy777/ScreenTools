using System;
using System.IO;
using System.Drawing;
using System.Linq;
using Forms = System.Windows.Forms;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class ScreenCaptureServiceTests
{
    [Fact]
    public void CaptureEveryConnectedMonitor_CapturesCenterAndAllFourPhysicalCorners()
    {
        var root = Path.Combine(Path.GetTempPath(), "LensSnap-MonitorAcceptance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var service = new ScreenCaptureService();
            var session = new RecordingSessionState { ScreenshotFormat = "PNG", OutputDirectory = root };
            var monitors = Forms.Screen.AllScreens;
            Assert.NotEmpty(monitors);

            foreach (var monitor in monitors)
            {
                var bounds = monitor.Bounds;
                Assert.True(bounds.Width >= 16 && bounds.Height >= 16, $"Monitor {monitor.DeviceName} is too small for edge probes: {bounds}.");
                var probes = new[]
                {
                    ("center", bounds.Left + bounds.Width / 2 - 8, bounds.Top + bounds.Height / 2 - 8),
                    ("top-left", bounds.Left, bounds.Top),
                    ("top-right", bounds.Right - 16, bounds.Top),
                    ("bottom-left", bounds.Left, bounds.Bottom - 16),
                    ("bottom-right", bounds.Right - 16, bounds.Bottom - 16)
                };

                foreach (var (name, x, y) in probes)
                {
                    var artifact = service.CaptureScreenshotRegion(session, new System.Windows.Rect(x, y, 16, 16));
                    try
                    {
                        Assert.True(File.Exists(artifact.OutputPath), $"{monitor.DeviceName}/{name}: output file missing.");
                        Assert.Equal(new System.Windows.Rect(x, y, 16, 16), artifact.CaptureBounds);
                        using var image = Image.FromFile(artifact.OutputPath);
                        Assert.Equal(16, image.Width);
                        Assert.Equal(16, image.Height);
                    }
                    finally
                    {
                        if (File.Exists(artifact.OutputPath)) File.Delete(artifact.OutputPath);
                    }
                }
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CaptureAdjacentMonitorSeams_ProducesExactCrossMonitorPixelRectangle()
    {
        var monitors = Forms.Screen.AllScreens.Select(screen => screen.Bounds).ToArray();
        var pairs = from i in Enumerable.Range(0, monitors.Length)
                    from j in Enumerable.Range(i + 1, monitors.Length - i - 1)
                    select (A: monitors[i], B: monitors[j]);
        var seamRegions = new System.Collections.Generic.List<System.Windows.Rect>();
        foreach (var (a, b) in pairs)
        {
            var overlapTop = Math.Max(a.Top, b.Top);
            var overlapBottom = Math.Min(a.Bottom, b.Bottom);
            var overlapLeft = Math.Max(a.Left, b.Left);
            var overlapRight = Math.Min(a.Right, b.Right);
            if (a.Right == b.Left || b.Right == a.Left)
            {
                if (overlapBottom - overlapTop >= 16)
                    seamRegions.Add(new System.Windows.Rect(Math.Min(a.Right, b.Right) - 8, overlapTop + (overlapBottom - overlapTop) / 2 - 8, 16, 16));
            }
            else if (a.Bottom == b.Top || b.Bottom == a.Top)
            {
                if (overlapRight - overlapLeft >= 16)
                    seamRegions.Add(new System.Windows.Rect(overlapLeft + (overlapRight - overlapLeft) / 2 - 8, Math.Min(a.Bottom, b.Bottom) - 8, 16, 16));
            }
        }

        if (seamRegions.Count == 0)
            return; // This physical layout has no edge-adjacent monitor pair.

        var root = Path.Combine(Path.GetTempPath(), "LensSnap-SeamAcceptance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var service = new ScreenCaptureService();
            var session = new RecordingSessionState { ScreenshotFormat = "PNG", OutputDirectory = root };
            foreach (var region in seamRegions)
            {
                var artifact = service.CaptureScreenshotRegion(session, region);
                try
                {
                    var expected = DesktopCaptureGeometry.ToPixelRectangle(region);
                    Assert.Equal(new System.Windows.Rect(expected.X, expected.Y, expected.Width, expected.Height), artifact.CaptureBounds);
                    using var image = Image.FromFile(artifact.OutputPath);
                    Assert.Equal(expected.Width, image.Width);
                    Assert.Equal(expected.Height, image.Height);
                }
                finally
                {
                    if (File.Exists(artifact.OutputPath)) File.Delete(artifact.OutputPath);
                }
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("PNG", ".png")]
    [InlineData("JPG", ".jpg")]
    [InlineData("BMP", ".bmp")]
    public void CaptureVirtualScreen_EachSupportedImageFormatPreservesPhysicalDimensions(string format, string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "LensSnap-FormatAcceptance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var expected = DesktopCaptureGeometry.GetVirtualBounds(Forms.Screen.AllScreens.Select(screen => screen.Bounds));
            var artifact = new ScreenCaptureService().CaptureScreenshot(new RecordingSessionState { ScreenshotFormat = format, OutputDirectory = root });
            Assert.Equal(extension, Path.GetExtension(artifact.OutputPath), ignoreCase: true);
            Assert.Equal(new System.Windows.Rect(expected.X, expected.Y, expected.Width, expected.Height), artifact.CaptureBounds);
            using var image = Image.FromFile(artifact.OutputPath);
            Assert.Equal(expected.Width, image.Width);
            Assert.Equal(expected.Height, image.Height);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("720P 30FPS")]
    [InlineData("1080P 60FPS")]
    [InlineData("1440P 30FPS")]
    public void CaptureRecordingFrameJpeg_UsesExpectedPhysicalDesktopAspectRatioAndProfileWidth(string preset)
    {
        var virtualBounds = DesktopCaptureGeometry.GetVirtualBounds(Forms.Screen.AllScreens.Select(screen => screen.Bounds));
        var profile = CaptureQualityProfile.FromPreset(preset);
        var bytes = new ScreenCaptureService().CaptureFrameJpegBytes(preset);
        using var stream = new MemoryStream(bytes);
        using var image = Image.FromStream(stream);

        var expectedWidth = Math.Min(virtualBounds.Width, profile.MaxWidth);
        var expectedHeight = (int)Math.Round(virtualBounds.Height * (expectedWidth / (double)virtualBounds.Width));
        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
        Assert.Equal(virtualBounds.Width / (double)virtualBounds.Height,
            image.Width / (double)image.Height, precision: 2);
    }

    [Fact]
    public void CaptureVirtualScreen_CurrentScreen_DoesNotExceedMemoryBudget()
    {
        var service = new ScreenCaptureService();
        var session = new RecordingSessionState
        {
            ScreenshotFormat = "PNG",
            OutputDirectory = Path.GetTempPath()
        };

        // This should not throw if the current screen is within budget.
        var artifact = service.CaptureScreenshot(session);
        Assert.NotNull(artifact);
        Assert.True(File.Exists(artifact.OutputPath));
        var expectedBounds = DesktopCaptureGeometry.GetVirtualBounds(
            System.Linq.Enumerable.Select(Forms.Screen.AllScreens, screen => screen.Bounds));
        Assert.Equal(new System.Windows.Rect(expectedBounds.X, expectedBounds.Y, expectedBounds.Width, expectedBounds.Height), artifact.CaptureBounds);
        using (var image = Image.FromFile(artifact.OutputPath))
        {
            Assert.Equal(expectedBounds.Width, image.Width);
            Assert.Equal(expectedBounds.Height, image.Height);
        }
        File.Delete(artifact.OutputPath);
    }

    [Fact]
    public void CaptureScreenshotRegion_RoundsOutwardAndReportsActualPhysicalPixelDimensions()
    {
        var service = new ScreenCaptureService();
        var session = new RecordingSessionState
        {
            ScreenshotFormat = "PNG",
            OutputDirectory = Path.GetTempPath()
        };
        var primary = Forms.Screen.PrimaryScreen!.Bounds;
        var region = new System.Windows.Rect(primary.Left + 100.25, primary.Top + 80.5, 80.25, 60.25);

        var artifact = service.CaptureScreenshotRegion(session, region);
        try
        {
            Assert.True(File.Exists(artifact.OutputPath));
            Assert.Equal(new System.Windows.Rect(primary.Left + 100, primary.Top + 80, 81, 61), artifact.CaptureBounds);
            using var image = Image.FromFile(artifact.OutputPath);
            Assert.Equal(81, image.Width);
            Assert.Equal(61, image.Height);
        }
        finally
        {
            if (File.Exists(artifact.OutputPath))
                File.Delete(artifact.OutputPath);
        }
    }
}
