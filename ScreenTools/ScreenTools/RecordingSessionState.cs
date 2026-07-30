using System;

namespace ScreenTools;

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
    private ScreenshotAfterCaptureBehavior _screenshotAfterCapture = ScreenshotAfterCaptureBehavior.ShowQuickActions;
    private ShortcutGesture _screenshotShortcut = ShortcutGesture.CreateDefault(ShortcutAction.Screenshot);
    private ShortcutGesture _recordingShortcut = ShortcutGesture.CreateDefault(ShortcutAction.Recording);
    private ShortcutGesture _replayShortcut = ShortcutGesture.CreateDefault(ShortcutAction.Replay);

    public event EventHandler? StateChanged;

    public string ScreenshotFormat
    {
        get => _screenshotFormat;
        set => SetField(ref _screenshotFormat, value);
    }

    public ScreenshotCaptureMode ScreenshotCaptureMode
    {
        get => _screenshotCaptureMode;
        set => SetField(ref _screenshotCaptureMode, value);
    }

    public bool CopyScreenshotToClipboard
    {
        get => _copyScreenshotToClipboard;
        set => SetField(ref _copyScreenshotToClipboard, value);
    }

    public bool IncludeMicrophone
    {
        get => _includeMicrophone;
        set => SetField(ref _includeMicrophone, value);
    }

    public bool IncludeSystemAudio
    {
        get => _includeSystemAudio;
        set => SetField(ref _includeSystemAudio, value);
    }

    public string QualityPreset
    {
        get => _qualityPreset;
        set => SetField(ref _qualityPreset, value);
    }

    public bool ReplayBufferEnabled
    {
        get => _replayBufferEnabled;
        set => SetField(ref _replayBufferEnabled, value);
    }

    public int ReplaySeconds
    {
        get => _replaySeconds;
        set => SetField(ref _replaySeconds, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetField(ref _outputDirectory, value);
    }

    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => SetField(ref _launchAtStartup, value);
    }

    public ScreenshotAfterCaptureBehavior ScreenshotAfterCapture
    {
        get => _screenshotAfterCapture;
        set => SetField(ref _screenshotAfterCapture, value);
    }

    public ShortcutGesture ScreenshotShortcut
    {
        get => _screenshotShortcut;
        set => SetField(ref _screenshotShortcut, value);
    }

    public ShortcutGesture RecordingShortcut
    {
        get => _recordingShortcut;
        set => SetField(ref _recordingShortcut, value);
    }

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
