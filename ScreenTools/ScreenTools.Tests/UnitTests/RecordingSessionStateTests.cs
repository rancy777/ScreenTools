using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class RecordingSessionStateTests
{
    [Fact]
    public void Default_IsIdle()
    {
        var state = new RecordingSessionState();
        Assert.Equal(RecordingStatus.Idle, state.Status);
    }

    [Fact]
    public void ChangeTo_UpdatesStatus()
    {
        var state = new RecordingSessionState();
        state.ChangeTo(RecordingStatus.Recording);
        Assert.Equal(RecordingStatus.Recording, state.Status);
    }

    [Fact]
    public void ChangeTo_RaisesStateChanged()
    {
        var state = new RecordingSessionState();
        var raised = false;
        state.StateChanged += (_, _) => raised = true;
        state.ChangeTo(RecordingStatus.Recording);
        Assert.True(raised);
    }
}
