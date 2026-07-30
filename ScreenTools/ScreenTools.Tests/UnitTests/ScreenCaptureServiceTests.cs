using System;
using System.IO;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class ScreenCaptureServiceTests
{
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
        File.Delete(artifact.OutputPath);
    }
}
