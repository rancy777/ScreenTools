using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class CaptureQualityProfileTests
{
    [Theory]
    [InlineData("720P 30FPS", 1280, 12, 4, 76L)]
    [InlineData("1440P 30FPS", 2560, 15, 5, 84L)]
    [InlineData("1080P 60FPS", 1920, 15, 5, 82L)]
    [InlineData("unknown", 1920, 15, 5, 82L)]
    [InlineData(null, 1920, 15, 5, 82L)]
    [InlineData("", 1920, 15, 5, 82L)]
    public void FromPreset_ReturnsExpectedProfile(string? preset, int expectedMaxWidth, int expectedRecordFps, int expectedReplayFps, long expectedQuality)
    {
        var profile = CaptureQualityProfile.FromPreset(preset);

        Assert.Equal(expectedMaxWidth, profile.MaxWidth);
        Assert.Equal(expectedRecordFps, profile.RecordingFrameRate);
        Assert.Equal(expectedReplayFps, profile.ReplayFrameRate);
        Assert.Equal(expectedQuality, profile.JpegQuality);
    }
}
