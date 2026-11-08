using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScreenTools;

/// <summary>
/// Coordinates application windows, capture workflows, services, and orderly shutdown.
/// </summary>
public sealed class WindowFlowCoordinator
{
    private readonly App _app;
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly FrameSequenceEncoder _frameSequenceEncoder;
    private readonly TempWorkspaceService _tempWorkspaceService;
    private readonly MicrophoneCaptureService _microphoneCaptureService;
    private readonly WasapiLoopbackCaptureService _systemAudioCaptureService;
    private readonly ReplayBufferService _replayBufferService;
    private readonly OutputHistoryService _outputHistoryService;
    private readonly ClipboardManagerService _clipboardManagerService;
    private readonly AppStatusService _statusService;
    private readonly SessionPersistenceService _sessionPersistenceService;
    private readonly StartupLaunchService _startupLaunchService;
    private bool _lastLaunchAtStartupState;
    private MainWindow? _mainWindow;
    private RecordingHudWindow? _recordingHudWindow;
    private RecordingActiveWindow? _recordingActiveWindow;
    private readonly List<ScreenshotStickerWindow> _stickerWindows = [];
    private ScreenshotActionToastWindow? _screenshotActionToastWindow;
    private GlobalHotKeyManager? _hotKeyManager;
    private bool _isSavingReplay;
    private bool _isShuttingDown;
    /// <summary>
    /// Gets the shared session settings used by the application workflows.
    /// </summary>
    public RecordingSessionState Session { get; }
    /// <summary>
    /// Gets the service responsible for recording capture and finalization.
    /// </summary>
    public RecordingService RecordingService { get; }

    /// <summary>
    /// Creates application services, loads persisted settings, and prepares the window workflow.
    /// </summary>
    public WindowFlowCoordinator(App app)
    {
        _app = app;
        _tempWorkspaceService = new TempWorkspaceService();
        _tempWorkspaceService.CleanupStaleRecordingWorkspaces(TimeSpan.FromHours(12));
        _screenCaptureService = new ScreenCaptureService();
        _frameSequenceEncoder = new FrameSequenceEncoder();
        _microphoneCaptureService = new MicrophoneCaptureService();
        _systemAudioCaptureService = new WasapiLoopbackCaptureService();
        _systemAudioCaptureService.RefreshAvailability();
        _outputHistoryService = new OutputHistoryService();
        _clipboardManagerService = new ClipboardManagerService();
        _statusService = new AppStatusService();
        _sessionPersistenceService = new SessionPersistenceService();
        _startupLaunchService = new StartupLaunchService();
        _sessionPersistenceService.PersistenceFailed += (_, exception) =>
            _statusService.SetStatus(LocalizationManager.Translate($"设置保存失败：{exception.Message}"), AppStatusLevel.Warning);
        _sessionPersistenceService.SessionCorrupted += (_, _) =>
            _statusService.SetStatus(LocalizationManager.Translate("设置文件已损坏，已隔离损坏文件并恢复默认设置。"), AppStatusLevel.Warning);
        _outputHistoryService.PersistenceFailed += (_, exception) =>
            _statusService.SetStatus(LocalizationManager.Translate($"输出历史记录保存失败：{exception.Message}"), AppStatusLevel.Warning);
        _clipboardManagerService.PersistenceFailed += (_, exception) =>
            _statusService.SetStatus(LocalizationManager.Translate($"剪贴板历史记录保存失败：{exception.Message}"), AppStatusLevel.Warning);
        Session = _sessionPersistenceService.Load();
        LocalizationManager.SetLanguage(Session.Language);
        _replayBufferService = new ReplayBufferService(_screenCaptureService, _frameSequenceEncoder, Session);
        _lastLaunchAtStartupState = _startupLaunchService.IsEnabled();
        Session.LaunchAtStartup = _lastLaunchAtStartupState;
        RecordingService = new RecordingService(
            _screenCaptureService,
            _frameSequenceEncoder,
            _microphoneCaptureService,
            _systemAudioCaptureService,
            _tempWorkspaceService);
        _replayBufferService.Start();
        Session.StateChanged += OnSessionStateChanged;
    }

    /// <summary>
    /// Shows and activates the main application workspace.
    /// </summary>
    public void ShowMainWindow()
    {
        EnsureMainWindow();

        _recordingHudWindow?.Hide();
        _recordingActiveWindow?.Hide();

        _mainWindow!.Show();
        _mainWindow.Activate();
    }

    /// <summary>
    /// Shows and activates the compact recording heads-up display.
    /// </summary>
    public void ShowRecordingHud()
    {
        EnsureRecordingHudWindow();

        _mainWindow?.Hide();
        _recordingActiveWindow?.Hide();

        _recordingHudWindow!.Show();
        MonitorWindowPlacement.PlaceOnCursorMonitor(_recordingHudWindow, MonitorWindowAlignment.Center);
        _recordingHudWindow.Activate();
    }

    /// <summary>
    /// Starts a recording and switches to the active-recording window.
    /// </summary>
    public void StartRecording()
    {
        if (RecordingService.Status != RecordingStatus.Idle)
        {
            return;
        }

        EnsureRecordingActiveWindow();

        RecordingService.Start(Session);
        PublishRecordingEnvironmentStatus();
        _recordingHudWindow?.Hide();
        _mainWindow?.Hide();
        _recordingActiveWindow!.Show();
        _recordingActiveWindow.Activate();
    }

    /// <summary>
    /// Finalizes the active recording, reports its output, and returns to the recording HUD.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        try
        {
            await HandleRecordingStopAsync(saveOutput: true);
        }
        catch (Exception ex)
        {
            var message = LocalizationManager.Translate(ex.Message);
            _statusService.SetStatus(message, AppStatusLevel.Error);
            MessageBox.Show(message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        ShowRecordingHud();
    }

    /// <summary>
    /// Stops an active recording before returning to the main settings window.
    /// </summary>
    public async Task ExitToSettingsAsync()
    {
        if (RecordingService.Status != RecordingStatus.Idle)
        {
            try
            {
                await HandleRecordingStopAsync(saveOutput: true);
            }
            catch (Exception ex)
            {
                var message = LocalizationManager.Translate(ex.Message);
                _statusService.SetStatus(message, AppStatusLevel.Error);
                MessageBox.Show(message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        ShowMainWindow();
    }

    /// <summary>
    /// Starts recording when idle or stops and finalizes the current recording.
    /// </summary>
    public async Task ToggleRecordingAsync()
    {
        if (RecordingService.Status == RecordingStatus.Idle)
        {
            StartRecording();
            return;
        }

        await StopRecordingAsync();
    }

    /// <summary>
    /// Captures the configured screenshot target and writes the resulting artifact.
    /// </summary>
    public async Task CaptureScreenshotAsync()
    {
        try
        {
            var artifact = await CaptureScreenshotArtifactAsync();
            if (artifact is null)
            {
                _statusService.SetStatus("已取消截图。", AppStatusLevel.Info);
                return;
            }

            _outputHistoryService.Add(artifact);
            HandleScreenshotPresentation(artifact);
            // Clipboard APIs require the WPF UI thread; the expensive capture and image
            // encoding have already completed off-thread.
            var clipboardResult = TryCopyScreenshotToClipboard(artifact);
            PublishArtifactStatus(artifact, clipboardResult);
        }
        catch (Exception ex)
        {
            var message = LocalizationManager.Translate(ex.Message);
            _statusService.SetStatus(message, AppStatusLevel.Error);
            MessageBox.Show(message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Exports the prepared replay frames and returns the resulting capture artifact.
    /// </summary>
    public async Task SaveReplayAsync(CancellationToken cancellationToken = default)
    {
        if (_isSavingReplay)
        {
            _statusService.SetStatus("回录仍在导出，请稍候。", AppStatusLevel.Info);
            return;
        }

        if (!_replayBufferService.IsEnabled)
        {
            _statusService.SetStatus("后台回录缓存已关闭，开启后才会保留可回录片段。", AppStatusLevel.Warning);
            return;
        }

        _isSavingReplay = true;

        try
        {
            var export = await Task.Run(
                () => _replayBufferService.PrepareReplayExport(Session),
                cancellationToken);
            if (export is null)
            {
                _statusService.SetStatus("当前还没有足够的回录缓存。", AppStatusLevel.Warning);
                return;
            }

            _statusService.SetStatus("正在后台导出回录，仅保存画面。", AppStatusLevel.Info);
            var artifact = await _replayBufferService.SaveReplayAsync(export, cancellationToken);
            _outputHistoryService.Add(artifact);
            PublishArtifactStatus(artifact);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _statusService.SetStatus(LocalizationManager.Translate("回放导出已取消，回放缓存仍然可用。"), AppStatusLevel.Info);
        }
        catch (Exception ex)
        {
            var message = LocalizationManager.Translate(ex.Message);
            _statusService.SetStatus(message, AppStatusLevel.Error);
            MessageBox.Show(message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isSavingReplay = false;
        }
    }

    private void EnsureMainWindow()
    {
        if (_mainWindow is not null)
        {
            return;
        }

        _mainWindow = new MainWindow();
        _mainWindow.BindSession(Session);
        _mainWindow.BindOutputHistory(_outputHistoryService);
        _mainWindow.BindClipboardHistory(_clipboardManagerService);
        _mainWindow.BindStatus(_statusService);
        _mainWindow.BindSystemAudioCapture(_systemAudioCaptureService);
        _hotKeyManager = new GlobalHotKeyManager(_mainWindow, Session);
        _hotKeyManager.HotKeyPressed += OnHotKeyPressed;
        _mainWindow.Closed += async (_, _) =>
        {
            _isShuttingDown = true;
            try
            {
                await RecordingService.AbortAsync();
                await _replayBufferService.StopAsync(clearFrames: true);
            }
            catch (Exception ex)
            {
                _statusService.SetStatus(LocalizationManager.Translate($"关闭应用时清理失败：{ex.Message}"), AppStatusLevel.Warning);
            }
            finally
            {
                _recordingHudWindow?.Close();
                _recordingActiveWindow?.Close();
                _screenshotActionToastWindow?.Close();
                CloseStickerWindows();
                _hotKeyManager?.Dispose();
                _app.Shutdown();
            }
        };
    }

    private void EnsureRecordingHudWindow()
    {
        if (_recordingHudWindow is not null)
        {
            return;
        }

        _recordingHudWindow = new RecordingHudWindow();
        _recordingHudWindow.BindSession(Session);
        _recordingHudWindow.BindStatus(_statusService);
        _recordingHudWindow.Closed += (_, _) =>
        {
            _recordingHudWindow = null;
        };
        _recordingHudWindow.Closing += OnRecordingHudClosing;
    }

    private void EnsureRecordingActiveWindow()
    {
        if (_recordingActiveWindow is not null)
        {
            return;
        }

        _recordingActiveWindow = new RecordingActiveWindow();
        _recordingActiveWindow.BindSession(Session);
        _recordingActiveWindow.BindRecordingService(RecordingService);
        _recordingActiveWindow.Closed += (_, _) =>
        {
            _recordingActiveWindow = null;
        };
        _recordingActiveWindow.Closing += OnRecordingActiveWindowClosing;
    }

    private void OnRecordingActiveWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isShuttingDown || RecordingService.Status == RecordingStatus.Idle)
            return;

        // Closing the compact control window must not leave an active recording
        // running invisibly. Route the user to the HUD instead.
        e.Cancel = true;
        _recordingActiveWindow?.Hide();
        ShowRecordingHud();
    }

    private void OnRecordingHudClosing(object? sender, CancelEventArgs e)
    {
        if (_isShuttingDown)
            return;

        // Keep a visible control surface available while recording. When idle,
        // closing the HUD simply returns to the main workspace.
        e.Cancel = true;
        _recordingHudWindow?.Hide();
        if (RecordingService.Status is RecordingStatus.Recording or RecordingStatus.Paused or RecordingStatus.Finalizing)
        {
            EnsureRecordingActiveWindow();
            _recordingActiveWindow!.Show();
            _recordingActiveWindow.Activate();
        }
        else
        {
            ShowMainWindow();
        }
    }

    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        if (!_app.Dispatcher.CheckAccess())
        {
            if (!_app.Dispatcher.HasShutdownStarted && !_app.Dispatcher.HasShutdownFinished)
                _app.Dispatcher.BeginInvoke(new Action(() => OnSessionStateChanged(sender, e)));
            return;
        }

        ExecuteSafely(() => _sessionPersistenceService.Save(Session));
        LocalizationManager.SetLanguage(Session.Language);

        if (Session.LaunchAtStartup == _lastLaunchAtStartupState)
        {
            return;
        }

        ExecuteSafely(() =>
        {
            _startupLaunchService.SetEnabled(Session.LaunchAtStartup);
            _lastLaunchAtStartupState = Session.LaunchAtStartup;
        });
    }

    /// <summary>
    /// Temporarily suspends or resumes global shortcut handling.
    /// </summary>
    public void SetShortcutCaptureSuspended(bool suspended)
    {
        if (_hotKeyManager is not null)
        {
            _hotKeyManager.IsSuspended = suspended;
        }
    }

    /// <summary>
    /// Closes every screenshot sticker window currently displayed.
    /// </summary>
    public void CloseAllScreenshotStickers()
    {
        CloseStickerWindows();
    }

    private async void OnHotKeyPressed(object? sender, ShortcutAction action)
    {
        try
        {
            switch (action)
            {
                case ShortcutAction.Screenshot:
                    await CaptureScreenshotAsync();
                    break;
                case ShortcutAction.Recording:
                    await ToggleRecordingAsync();
                    break;
                case ShortcutAction.Replay:
                    await SaveReplayAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _statusService.SetStatus(LocalizationManager.Translate(ex.Message), AppStatusLevel.Error);
        }
    }

    private void ExecuteSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var message = LocalizationManager.Translate(ex.Message);
            _statusService.SetStatus(message, AppStatusLevel.Error);
            MessageBox.Show(message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Copies the specified image to the clipboard and publishes the result as application status.
    /// </summary>
    public ClipboardCopyResult CopyImageToClipboard(string imagePath, string kind = "截图")
    {
        var result = _clipboardManagerService.CopyImageFromFile(imagePath, kind);
        var fileName = string.IsNullOrWhiteSpace(imagePath) ? "当前内容" : Path.GetFileName(imagePath);
        var message = result.Success
            ? $"{kind}已复制到剪贴板：{fileName}"
            : $"{kind}复制失败：{fileName} · {result.Detail}";
        _statusService.SetStatus(message, result.Success ? AppStatusLevel.Success : AppStatusLevel.Warning);
        return result;
    }

    private void PublishArtifactStatus(CaptureArtifact artifact, ClipboardCopyResult? clipboardResult = null)
    {
        var level = string.IsNullOrWhiteSpace(artifact.Detail) && clipboardResult is not { Success: false }
            ? AppStatusLevel.Success
            : AppStatusLevel.Warning;
        var suffix = artifact.IsVideo ? "视频已输出" : "结果已输出";
        var message = $"{artifact.Kind}{suffix}：{artifact.OutputPath}";

        if (!artifact.IsVideo && string.Equals(artifact.Kind, "截图", StringComparison.Ordinal) && artifact.CaptureBounds is Rect bounds)
        {
            message = $"{message} · {Math.Round(bounds.Width):0} × {Math.Round(bounds.Height):0}";
        }

        if (!string.IsNullOrWhiteSpace(artifact.Detail))
        {
            message = $"{message} · {artifact.Detail.Replace(Environment.NewLine, " | ")}";
        }

        if (clipboardResult is { Success: true })
        {
            message = $"{message} · {clipboardResult.Detail}";
        }
        else if (clipboardResult is { Success: false })
        {
            message = $"{message} · 剪贴板失败：{clipboardResult.Detail}";
        }

        _statusService.SetStatus(message, level);
    }

    private async Task<CaptureArtifact?> HandleRecordingStopAsync(bool saveOutput)
    {
        if (!saveOutput)
        {
            await RecordingService.AbortAsync();
            return null;
        }

        var artifact = await RecordingService.StopAsync();
        if (artifact is null)
        {
            return null;
        }

        _outputHistoryService.Add(artifact);
        PublishArtifactStatus(artifact);
        return artifact;
    }

    private async Task<CaptureArtifact?> CaptureScreenshotArtifactAsync()
    {
        if (Session.ScreenshotCaptureMode == ScreenshotCaptureMode.Region)
        {
            return await CaptureRegionScreenshotAsync();
        }

        var session = CreateScreenshotSessionSnapshot();
        return await Task.Run(() => _screenCaptureService.CaptureScreenshot(session));
    }

    private async Task<CaptureArtifact?> CaptureRegionScreenshotAsync()
    {
        var hiddenWindows = HideAppWindowsForCapture();
        try
        {
            await Task.Delay(120);
            var selectedRegion = await RegionSelectionWindow.SelectRegionAsync();
            if (selectedRegion is null)
            {
                return null;
            }

            var session = CreateScreenshotSessionSnapshot();
            var region = selectedRegion.Value;
            return await Task.Run(() => _screenCaptureService.CaptureScreenshotRegion(session, region));
        }
        finally
        {
            RestoreHiddenWindows(hiddenWindows);
        }
    }

    private RecordingSessionState CreateScreenshotSessionSnapshot() => new()
    {
        ScreenshotFormat = Session.ScreenshotFormat,
        OutputDirectory = Session.OutputDirectory
    };

    private void HandleScreenshotPresentation(CaptureArtifact artifact)
    {
        switch (Session.ScreenshotAfterCapture)
        {
            case ScreenshotAfterCaptureBehavior.SaveAndPin:
                ShowScreenshotSticker(artifact);
                break;
            case ScreenshotAfterCaptureBehavior.ShowQuickActions:
                ShowScreenshotActionToast(artifact);
                break;
        }
    }

    private ClipboardCopyResult? TryCopyScreenshotToClipboard(CaptureArtifact artifact)
    {
        if (!Session.CopyScreenshotToClipboard || !File.Exists(artifact.OutputPath))
        {
            return null;
        }

        return _clipboardManagerService.CopyImageFromFile(artifact.OutputPath, artifact.Kind);
    }

    private void ShowScreenshotSticker(CaptureArtifact artifact)
    {
        if (!string.Equals(artifact.Kind, "截图", StringComparison.Ordinal))
        {
            return;
        }

        var offset = 32 * _stickerWindows.Count;
        var window = new ScreenshotStickerWindow(artifact.OutputPath, new Point(0, 0));
        window.CloseAllRequested += (_, _) => CloseStickerWindows();
        window.Closed += (_, _) => _stickerWindows.Remove(window);
        _stickerWindows.Add(window);
        window.Show();
        MonitorWindowPlacement.PlaceOnCursorMonitor(window, MonitorWindowAlignment.TopRight, margin: 24, offset: offset);
    }

    private void ShowScreenshotActionToast(CaptureArtifact artifact)
    {
        if (!string.Equals(artifact.Kind, "截图", StringComparison.Ordinal))
        {
            return;
        }

        _screenshotActionToastWindow?.Close();
        _screenshotActionToastWindow = new ScreenshotActionToastWindow(artifact.OutputPath, new Point(0, 0));
        _screenshotActionToastWindow.ActionRequested += (_, action) => HandleScreenshotQuickAction(action, artifact);
        _screenshotActionToastWindow.Closed += (_, _) => _screenshotActionToastWindow = null;
        _screenshotActionToastWindow.Show();
        MonitorWindowPlacement.PlaceOnCursorMonitor(_screenshotActionToastWindow, MonitorWindowAlignment.BottomRight);
    }

    private void HandleScreenshotQuickAction(ScreenshotQuickAction action, CaptureArtifact artifact)
    {
        switch (action)
        {
            case ScreenshotQuickAction.PinToScreen:
                ShowScreenshotSticker(artifact);
                _statusService.SetStatus($"截图已贴到屏幕：{artifact.OutputPath}", AppStatusLevel.Success);
                break;
            case ScreenshotQuickAction.Open:
                OpenPath(artifact.OutputPath);
                break;
            case ScreenshotQuickAction.RevealInFolder:
                RevealInFolder(artifact.OutputPath);
                break;
            case ScreenshotQuickAction.SetDefaultSaveOnly:
                Session.ScreenshotAfterCapture = ScreenshotAfterCaptureBehavior.SaveOnly;
                _statusService.SetStatus("截图后默认行为已改为仅保存。", AppStatusLevel.Success);
                break;
            case ScreenshotQuickAction.SetDefaultSaveAndPin:
                Session.ScreenshotAfterCapture = ScreenshotAfterCaptureBehavior.SaveAndPin;
                ShowScreenshotSticker(artifact);
                _statusService.SetStatus("截图后默认行为已改为保存并贴到屏幕。", AppStatusLevel.Success);
                break;
        }
    }

    private void CloseStickerWindows()
    {
        foreach (var window in _stickerWindows.ToArray())
        {
            window.Close();
        }

        _stickerWindows.Clear();
    }

    private List<Window> HideAppWindowsForCapture()
    {
        var hiddenWindows = new List<Window>();
        foreach (Window window in Application.Current.Windows)
        {
            if (!window.IsVisible)
            {
                continue;
            }

            window.Hide();
            hiddenWindows.Add(window);
        }

        return hiddenWindows;
    }

    private static void RestoreHiddenWindows(IEnumerable<Window> windows)
    {
        foreach (var window in windows)
        {
            window.Show();
        }
    }

    private static void OpenPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var targetPath = Directory.Exists(path) || File.Exists(path) ? path : Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = targetPath,
            UseShellExecute = true
        });
    }

    private static void RevealInFolder(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{path}\"",
            UseShellExecute = true
        });
    }

    private void PublishRecordingEnvironmentStatus()
    {
        if (!Session.IncludeSystemAudio)
        {
            _statusService.SetStatus("录制已开始。当前未启用系统声音。", AppStatusLevel.Info);
            return;
        }

        var level = _systemAudioCaptureService.IsSupported
            ? AppStatusLevel.Info
            : AppStatusLevel.Warning;
        _statusService.SetStatus($"录制已开始。{_systemAudioCaptureService.AvailabilityMessage}", level);
    }
}
