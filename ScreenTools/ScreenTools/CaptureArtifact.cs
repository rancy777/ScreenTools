using System.Windows;

namespace ScreenTools;

public sealed record CaptureArtifact(
    string Kind,
    string OutputPath,
    int FrameCount,
    bool IsVideo = false,
    string? Detail = null,
    Rect? CaptureBounds = null);
