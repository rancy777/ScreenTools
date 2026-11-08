using System;

namespace ScreenTools;

/// <summary>
/// Stores user-configurable capture, recording, replay, shortcut, and localization settings.
/// </summary>
public sealed class RecordingSessionState
{
    private string _screenshotFormat = "PNG";
    private ScreenshotCaptureMode _screenshotCaptureMode = ScreenshotCaptureMode.FullScreen;
    private bool _copyScreenshotToClipboard = true;
    private bool _includeMicrophone = true;
    private bool _includeSystemAudio = true;
    private string _qualityPreset = "1080P 60FPS";
    private bool _replayBufferEnabled = true;
    private int _replaySeconds = 30;
    private string _outputDirectory = CaptureOutputPathHelper.GetDefaultOutputDirectory();
    private bool _launchAtStartup;
    private string _language = LocalizationManager.DetectSystemLanguage();
    private ScreenshotAfterCaptureBehavior _screenshotAfterCapture = ScreenshotAfterCaptureBehavior.ShowQuickActions;
    private ShortcutGesture _screenshotShortcut = ShortcutGesture.CreateDefault(ShortcutAction.Screenshot);
    private ShortcutGesture _recordingShortcut = ShortcutGesture.CreateDefault(ShortcutAction.Recording);
    private ShortcutGesture _replayShortcut = ShortcutGesture.CreateDefault(ShortcutAction.Replay);

    /// <summary>
    /// Raised after a user-configurable session setting changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public string ScreenshotFormat
    {
        get => _screenshotFormat;
        set => SetField(ref _screenshotFormat, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public ScreenshotCaptureMode ScreenshotCaptureMode
    {
        get => _screenshotCaptureMode;
        set => SetField(ref _screenshotCaptureMode, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public bool CopyScreenshotToClipboard
    {
        get => _copyScreenshotToClipboard;
        set => SetField(ref _copyScreenshotToClipboard, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public bool IncludeMicrophone
    {
        get => _includeMicrophone;
        set => SetField(ref _includeMicrophone, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public bool IncludeSystemAudio
    {
        get => _includeSystemAudio;
        set => SetField(ref _includeSystemAudio, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public string QualityPreset
    {
        get => _qualityPreset;
        set => SetField(ref _qualityPreset, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public bool ReplayBufferEnabled
    {
        get => _replayBufferEnabled;
        set => SetField(ref _replayBufferEnabled, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public int ReplaySeconds
    {
        get => _replaySeconds;
        set => SetField(ref _replaySeconds, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetField(ref _outputDirectory, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => SetField(ref _launchAtStartup, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public string Language
    {
        get => _language;
        set => SetField(ref _language, LocalizationManager.NormalizeLanguage(value));
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public ScreenshotAfterCaptureBehavior ScreenshotAfterCapture
    {
        get => _screenshotAfterCapture;
        set => SetField(ref _screenshotAfterCapture, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public ShortcutGesture ScreenshotShortcut
    {
        get => _screenshotShortcut;
        set => SetField(ref _screenshotShortcut, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public ShortcutGesture RecordingShortcut
    {
        get => _recordingShortcut;
        set => SetField(ref _recordingShortcut, value);
    }

    /// <summary>
    /// Initializes the RecordingSessionState component and prepares its required resources.
    /// </summary>
    public ShortcutGesture ReplayShortcut
    {
        get => _replayShortcut;
        set => SetField(ref _replayShortcut, value);
    }

    private void SetField<T>(ref T field, T value)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
