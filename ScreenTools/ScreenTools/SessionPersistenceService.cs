using System;
using System.IO;
using System.Text.Json;

namespace ScreenTools;

public sealed class SessionPersistenceService
{
    private readonly string _settingsPath;

    public SessionPersistenceService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LensSnap");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "session-settings.json");
    }

    public event EventHandler<SessionCorruptedException>? SessionCorrupted;

    public RecordingSessionState Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new RecordingSessionState();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var persisted = JsonSerializer.Deserialize<PersistedSessionState>(json);
            if (persisted is null)
            {
                return new RecordingSessionState();
            }

            return new RecordingSessionState
            {
                ScreenshotFormat = persisted.ScreenshotFormat ?? "PNG",
                ScreenshotCaptureMode = persisted.ScreenshotCaptureMode,
                CopyScreenshotToClipboard = persisted.CopyScreenshotToClipboard,
                IncludeMicrophone = persisted.IncludeMicrophone,
                IncludeSystemAudio = persisted.IncludeSystemAudio,
                QualityPreset = persisted.QualityPreset ?? "1080P 60FPS",
                ReplayBufferEnabled = persisted.ReplayBufferEnabled,
                ReplaySeconds = persisted.ReplaySeconds <= 0 ? 30 : persisted.ReplaySeconds,
                OutputDirectory = string.IsNullOrWhiteSpace(persisted.OutputDirectory)
                    ? CaptureOutputPathHelper.GetDefaultOutputDirectory()
                    : persisted.OutputDirectory,
                LaunchAtStartup = persisted.LaunchAtStartup,
                ScreenshotAfterCapture = persisted.ScreenshotAfterCapture,
                ScreenshotShortcut = persisted.ScreenshotShortcut?.Clone() ?? ShortcutGesture.CreateDefault(ShortcutAction.Screenshot),
                RecordingShortcut = persisted.RecordingShortcut?.Clone() ?? ShortcutGesture.CreateDefault(ShortcutAction.Recording),
                ReplayShortcut = persisted.ReplayShortcut?.Clone() ?? ShortcutGesture.CreateDefault(ShortcutAction.Replay)
            };
        }
        catch (JsonException ex)
        {
            SessionCorrupted?.Invoke(this, new SessionCorruptedException(ex));
            return new RecordingSessionState();
        }
    }

    public void Save(RecordingSessionState session)
    {
        var persisted = new PersistedSessionState
        {
            ScreenshotFormat = session.ScreenshotFormat,
            ScreenshotCaptureMode = session.ScreenshotCaptureMode,
            CopyScreenshotToClipboard = session.CopyScreenshotToClipboard,
            IncludeMicrophone = session.IncludeMicrophone,
            IncludeSystemAudio = session.IncludeSystemAudio,
            QualityPreset = session.QualityPreset,
            ReplayBufferEnabled = session.ReplayBufferEnabled,
            ReplaySeconds = session.ReplaySeconds,
            OutputDirectory = session.OutputDirectory,
            LaunchAtStartup = session.LaunchAtStartup,
            ScreenshotAfterCapture = session.ScreenshotAfterCapture,
            ScreenshotShortcut = session.ScreenshotShortcut.Clone(),
            RecordingShortcut = session.RecordingShortcut.Clone(),
            ReplayShortcut = session.ReplayShortcut.Clone()
        };

        try
        {
            File.WriteAllText(
                _settingsPath,
                JsonSerializer.Serialize(persisted, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Persisting settings is best-effort; do not throw from a side-effect-only save path.
        }
    }

    private sealed class PersistedSessionState
    {
        public string? ScreenshotFormat { get; set; }
        public ScreenshotCaptureMode ScreenshotCaptureMode { get; set; }
        public bool CopyScreenshotToClipboard { get; set; }
        public bool IncludeMicrophone { get; set; }
        public bool IncludeSystemAudio { get; set; }
        public string? QualityPreset { get; set; }
        public bool ReplayBufferEnabled { get; set; } = true;
        public int ReplaySeconds { get; set; }
        public string? OutputDirectory { get; set; }
        public bool LaunchAtStartup { get; set; }
        public ScreenshotAfterCaptureBehavior ScreenshotAfterCapture { get; set; }
        public ShortcutGesture? ScreenshotShortcut { get; set; }
        public ShortcutGesture? RecordingShortcut { get; set; }
        public ShortcutGesture? ReplayShortcut { get; set; }
    }

    public sealed record SessionCorruptedException(Exception Exception);
}
