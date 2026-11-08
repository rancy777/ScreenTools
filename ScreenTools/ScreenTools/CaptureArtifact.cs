using System.Windows;

namespace ScreenTools;

/// <summary>
/// Describes a screenshot, recording, or replay output produced by the application.
/// </summary>
public sealed record CaptureArtifact(
    string Kind,
    string OutputPath,
    int FrameCount,
    bool IsVideo = false,
    string? Detail = null,
    Rect? CaptureBounds = null);
