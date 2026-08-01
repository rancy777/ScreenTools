using System;
using System.Windows;

namespace ScreenTools;

public interface IScreenCaptureService
{
    CaptureArtifact CaptureScreenshot(RecordingSessionState session);
    CaptureArtifact CaptureScreenshotRegion(RecordingSessionState session, Rect region);
    void CaptureFrameJpeg(string outputPath, string qualityPreset);
    byte[] CaptureFrameJpegBytes(string qualityPreset);
}
