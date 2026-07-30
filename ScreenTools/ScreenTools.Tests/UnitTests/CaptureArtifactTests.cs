using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class CaptureArtifactTests
{
    [Fact]
    public void New_SetsDefaults()
    {
        var artifact = new CaptureArtifact("截图", "C:\\tmp\\1.png", 1);
        Assert.Equal("截图", artifact.Kind);
        Assert.Equal("C:\\tmp\\1.png", artifact.OutputPath);
        Assert.Equal(1, artifact.FrameCount);
        Assert.False(artifact.IsVideo);
    }

    [Fact]
    public void WithVideo_MarksIsVideo()
    {
        var artifact = new CaptureArtifact("回录", "C:\\tmp\\replay.mp4", 10, IsVideo: true);
        Assert.True(artifact.IsVideo);
        Assert.Equal(10, artifact.FrameCount);
    }
}
