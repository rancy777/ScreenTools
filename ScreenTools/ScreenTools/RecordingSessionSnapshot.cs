namespace ScreenTools;

/// <summary>
/// Captures the immutable recording options used for one recording session.
/// </summary>
public sealed record RecordingSessionSnapshot(
    string ScreenshotFormat,
    bool IncludeMicrophone,
    bool IncludeSystemAudio,
    string QualityPreset,
    int ReplaySeconds,
    string OutputDirectory,
    bool LaunchAtStartup);
