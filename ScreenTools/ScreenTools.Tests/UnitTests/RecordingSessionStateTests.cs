using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class RecordingSessionStateTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var state = new RecordingSessionState();
        Assert.True(state.ReplayBufferEnabled);
        Assert.Equal(30, state.ReplaySeconds);
        Assert.Equal("1080P 60FPS", state.QualityPreset);
    }

    [Fact]
    public void SetField_RaisesStateChanged()
    {
        var state = new RecordingSessionState();
        var raised = false;
        state.StateChanged += (_, _) => raised = true;
        state.QualityPreset = "720P 30FPS";
        Assert.True(raised);
        Assert.Equal("720P 30FPS", state.QualityPreset);
    }
}
