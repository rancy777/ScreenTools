using System;
using System.IO;
using System.Text.Json;

namespace ScreenTools;

/// <summary>
/// Loads and saves user settings, including recovery from corrupted settings files.
/// </summary>
public sealed class SessionPersistenceService
{
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes settings persistence using the supplied path or the application default.
    /// </summary>
    public SessionPersistenceService(string? settingsPath = null)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LensSnap");
        _settingsPath = Path.GetFullPath(settingsPath ?? Path.Combine(appDataPath, "session-settings.json"));
    }

    /// <summary>
    /// Raised when persisted settings are invalid and have been quarantined for recovery.
    /// </summary>
    public event EventHandler<SessionCorruptedException>? SessionCorrupted;
    /// <summary>
    /// Raised when updated history or settings could not be persisted.
    /// </summary>
    public event EventHandler<Exception>? PersistenceFailed;

    /// <summary>
    /// Loads saved settings, preserving a recoverable copy if the settings file is corrupted.
    /// </summary>
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
                ScreenshotFormat = NormalizeScreenshotFormat(persisted.ScreenshotFormat),
                ScreenshotCaptureMode = Enum.IsDefined(persisted.ScreenshotCaptureMode) ? persisted.ScreenshotCaptureMode : ScreenshotCaptureMode.FullScreen,
                CopyScreenshotToClipboard = persisted.CopyScreenshotToClipboard,
                IncludeMicrophone = persisted.IncludeMicrophone,
                IncludeSystemAudio = persisted.IncludeSystemAudio,
                QualityPreset = NormalizeQualityPreset(persisted.QualityPreset),
                ReplayBufferEnabled = persisted.ReplayBufferEnabled,
                ReplaySeconds = Math.Clamp(persisted.ReplaySeconds <= 0 ? 30 : persisted.ReplaySeconds, 5, 120),
                OutputDirectory = string.IsNullOrWhiteSpace(persisted.OutputDirectory)
                    ? CaptureOutputPathHelper.GetDefaultOutputDirectory()
                    : persisted.OutputDirectory,
                LaunchAtStartup = persisted.LaunchAtStartup,
                Language = LocalizationManager.NormalizeLanguage(persisted.Language ?? LocalizationManager.DetectSystemLanguage()),
                ScreenshotAfterCapture = Enum.IsDefined(persisted.ScreenshotAfterCapture) ? persisted.ScreenshotAfterCapture : ScreenshotAfterCaptureBehavior.ShowQuickActions,
                ScreenshotShortcut = persisted.ScreenshotShortcut?.Clone() ?? ShortcutGesture.CreateDefault(ShortcutAction.Screenshot),
                RecordingShortcut = persisted.RecordingShortcut?.Clone() ?? ShortcutGesture.CreateDefault(ShortcutAction.Recording),
                ReplayShortcut = persisted.ReplayShortcut?.Clone() ?? ShortcutGesture.CreateDefault(ShortcutAction.Replay)
            };
        }
        catch (JsonException ex)
        {
            QuarantineCorruptedSettings();
            SessionCorrupted?.Invoke(this, new SessionCorruptedException(ex));
            return new RecordingSessionState();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            PersistenceFailed?.Invoke(this, ex);
            return new RecordingSessionState();
        }
    }

    /// <summary>
    /// Persists the current recording and capture settings.
    /// </summary>
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
            Language = session.Language,
            ScreenshotAfterCapture = session.ScreenshotAfterCapture,
            ScreenshotShortcut = session.ScreenshotShortcut.Clone(),
            RecordingShortcut = session.RecordingShortcut.Clone(),
            ReplayShortcut = session.ReplayShortcut.Clone()
        };

        try
        {
            AtomicFilePersistence.WriteAllText(
                _settingsPath,
                JsonSerializer.Serialize(persisted, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            PersistenceFailed?.Invoke(this, ex);
        }
    }

    private static string NormalizeScreenshotFormat(string? format) => format?.Trim().ToUpperInvariant() switch
    {
        "JPG" or "JPEG" => "JPG",
        "BMP" => "BMP",
        _ => "PNG"
    };

    private static string NormalizeQualityPreset(string? preset) => preset?.Trim().ToUpperInvariant() switch
    {
        "720P 30FPS" => "720P 30FPS",
        "1440P 30FPS" => "1440P 30FPS",
        _ => "1080P 60FPS"
    };

    private void QuarantineCorruptedSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
                File.Move(_settingsPath, $"{_settingsPath}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}", overwrite: false);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class PersistedSessionState
    {
        /// <summary>
        /// Gets or sets the image format used when saving screenshots.
        /// </summary>
        public string? ScreenshotFormat { get; set; }
        /// <summary>
        /// Gets or sets whether screenshots capture the full desktop or a selected region.
        /// </summary>
        public ScreenshotCaptureMode ScreenshotCaptureMode { get; set; }
        /// <summary>
        /// Gets or sets whether saved screenshots are also copied to the clipboard.
        /// </summary>
        public bool CopyScreenshotToClipboard { get; set; }
        /// <summary>
        /// Gets or sets whether microphone audio is included in new recordings.
        /// </summary>
        public bool IncludeMicrophone { get; set; }
        /// <summary>
        /// Gets or sets whether desktop playback audio is included in new recordings.
        /// </summary>
        public bool IncludeSystemAudio { get; set; }
        /// <summary>
        /// Gets or sets the capture-quality preset used for recording and replay.
        /// </summary>
        public string? QualityPreset { get; set; }
        /// <summary>
        /// Gets or sets whether background instant-replay capture is enabled.
        /// </summary>
        public bool ReplayBufferEnabled { get; set; } = true;
        /// <summary>
        /// Gets or sets the target duration, in seconds, retained for instant replay.
        /// </summary>
        public int ReplaySeconds { get; set; }
        /// <summary>
        /// Gets or sets the directory where screenshots, recordings, and replays are saved.
        /// </summary>
        public string? OutputDirectory { get; set; }
        /// <summary>
        /// Gets or sets whether LensSnap launches when the user signs in.
        /// </summary>
        public bool LaunchAtStartup { get; set; }
        /// <summary>
        /// Gets or sets the application language identifier.
        /// </summary>
        public string? Language { get; set; }
        /// <summary>
        /// Gets or sets the action performed after a screenshot is saved.
        /// </summary>
        public ScreenshotAfterCaptureBehavior ScreenshotAfterCapture { get; set; }
        /// <summary>
        /// Gets or sets the keyboard gesture assigned to screenshot capture.
        /// </summary>
        public ShortcutGesture? ScreenshotShortcut { get; set; }
        /// <summary>
        /// Gets or sets the keyboard gesture assigned to recording toggle.
        /// </summary>
        public ShortcutGesture? RecordingShortcut { get; set; }
        /// <summary>
        /// Gets or sets the keyboard gesture assigned to instant-replay export.
        /// </summary>
        public ShortcutGesture? ReplayShortcut { get; set; }
    }

    /// <summary>
    /// Reports that persisted session settings could not be read and were quarantined.
    /// </summary>
    public sealed record SessionCorruptedException(Exception Exception);
}
