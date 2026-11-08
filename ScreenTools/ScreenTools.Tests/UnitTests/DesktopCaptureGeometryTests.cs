using System;
using System.Drawing;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public sealed class DesktopCaptureGeometryTests
{
    [Fact]
    public void GetVirtualBounds_SingleMonitor_ReturnsExactPhysicalBounds()
    {
        var bounds = DesktopCaptureGeometry.GetVirtualBounds([new Rectangle(0, 0, 1920, 1080)]);
        Assert.Equal(new Rectangle(0, 0, 1920, 1080), bounds);
    }

    [Fact]
    public void GetVirtualBounds_LeftAndRightMonitors_IncludesNegativeOrigin()
    {
        var bounds = DesktopCaptureGeometry.GetVirtualBounds([
            new Rectangle(0, 0, 1920, 1080),
            new Rectangle(-2560, 0, 2560, 1440)
        ]);

        Assert.Equal(new Rectangle(-2560, 0, 4480, 1440), bounds);
    }

    [Fact]
    public void GetVirtualBounds_AboveAndBelowMonitors_IncludesVerticalOffsets()
    {
        var bounds = DesktopCaptureGeometry.GetVirtualBounds([
            new Rectangle(0, 0, 1920, 1080),
            new Rectangle(400, -1200, 2560, 1200),
            new Rectangle(-300, 1080, 1600, 900)
        ]);

        Assert.Equal(Rectangle.FromLTRB(-300, -1200, 2960, 1980), bounds);
    }

    [Fact]
    public void GetVirtualBounds_MixedResolutions_UsesOuterEdgesNotPrimaryDimensions()
    {
        var bounds = DesktopCaptureGeometry.GetVirtualBounds([
            new Rectangle(0, 0, 1920, 1080),
            new Rectangle(1920, -180, 2560, 1440)
        ]);

        Assert.Equal(Rectangle.FromLTRB(0, -180, 4480, 1260), bounds);
    }

    [Theory]
    [InlineData(0.25, 0.5, 100.25, 50.5, 0, 0, 101, 51)]
    [InlineData(-1920.5, -1080.5, 100, 50, -1921, -1081, 101, 51)]
    [InlineData(10, 20, 1, 1, 10, 20, 1, 1)]
    [InlineData(100, 100, 0.01, 0.01, 100, 100, 1, 1)]
    public void ToPixelRectangle_RoundsOutwardToCoverSelectedPixels(
        double x, double y, double width, double height,
        int expectedX, int expectedY, int expectedWidth, int expectedHeight)
    {
        var result = DesktopCaptureGeometry.ToPixelRectangle(new System.Windows.Rect(x, y, width, height));
        Assert.Equal(new Rectangle(expectedX, expectedY, expectedWidth, expectedHeight), result);
    }

    [Fact]
    public void ToPixelRectangle_RejectsNonFiniteCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopCaptureGeometry.ToPixelRectangle(new System.Windows.Rect(double.NaN, 0, 10, 10)));
    }

    [Fact]
    public void ClipToVirtualBounds_ClipsNegativeAndOverflowingEdges()
    {
        var result = DesktopCaptureGeometry.ClipToVirtualBounds(
            new Rectangle(-100, -50, 300, 200),
            new Rectangle(-50, -25, 1920, 1080));

        Assert.Equal(new Rectangle(-50, -25, 250, 175), result);
    }

    [Fact]
    public void ClipToVirtualBounds_RejectsSelectionsOutsideDesktop()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopCaptureGeometry.ClipToVirtualBounds(
                new Rectangle(4000, 4000, 100, 100),
                new Rectangle(0, 0, 1920, 1080)));
    }

    [Fact]
    public void GetVirtualBounds_RejectsEmptyMonitorSet()
    {
        Assert.Throws<ArgumentException>(() => DesktopCaptureGeometry.GetVirtualBounds([]));
    }

    [Fact]
    public void MonitorPlacement_CentersOnMonitorWithNegativeOrigin()
    {
        var result = MonitorWindowPlacement.CalculateBounds(
            new Rectangle(-1920, -200, 1920, 1080), new Size(1024, 820), MonitorWindowAlignment.Center);

        Assert.Equal(new Rectangle(-1472, -70, 1024, 820), result);
    }

    [Fact]
    public void MonitorPlacement_TopRightUsesMonitorWorkAreaAndOffset()
    {
        var result = MonitorWindowPlacement.CalculateBounds(
            new Rectangle(1920, 0, 1920, 1040), new Size(460, 500), MonitorWindowAlignment.TopRight, 24, 64);

        Assert.Equal(new Rectangle(3292, 88, 460, 500), result);
    }

    [Fact]
    public void MonitorPlacement_BottomRightUsesMonitorWorkArea()
    {
        var result = MonitorWindowPlacement.CalculateBounds(
            new Rectangle(0, 0, 1920, 1040), new Size(400, 160), MonitorWindowAlignment.BottomRight);

        Assert.Equal(new Rectangle(1496, 856, 400, 160), result);
    }

    [Fact]
    public void MonitorPlacement_ClampsOversizedWindowToAvailableWorkArea()
    {
        var result = MonitorWindowPlacement.CalculateBounds(
            new Rectangle(-1280, 0, 1280, 720), new Size(1600, 900), MonitorWindowAlignment.Center);

        Assert.Equal(new Rectangle(-1280, 0, 1280, 720), result);
    }
}
