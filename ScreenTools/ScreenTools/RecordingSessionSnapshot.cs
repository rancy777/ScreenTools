namespace ScreenTools;

public sealed record RecordingSessionSnapshot(
    string ScreenshotFormat,
    bool IncludeMicrophone,
    bool IncludeSystemAudio,
    string QualityPreset,
    int ReplaySeconds,
    string OutputDirectory,
    bool LaunchAtStartup);
