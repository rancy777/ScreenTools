using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScreenTools;

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
    public RecordingSessionState Session { get; }
    public RecordingService RecordingService { get; }

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
        Session = _sessionPersistenceService.Load();
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

    public void ShowMainWindow()
    {
        EnsureMainWindow();

        _recordingHudWindow?.Hide();
        _recordingActiveWindow?.Hide();

        _mainWindow!.Show();
        _mainWindow.Activate();
    }

    public void ShowRecordingHud()
    {
        EnsureRecordingHudWindow();

        _mainWindow?.Hide();
        _recordingActiveWindow?.Hide();

        _recordingHudWindow!.Show();
        _recordingHudWindow.Activate();
    }

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

    public void StopRecording()
    {
        HandleRecordingStop(saveOutput: true);
        ShowRecordingHud();
    }

    public void ExitToSettings()
    {
        if (RecordingService.Status != RecordingStatus.Idle)
        {
            HandleRecordingStop(saveOutput: true);
        }
        ShowMainWindow();
    }

    public void ToggleRecording()
    {
        if (RecordingService.Status == RecordingStatus.Idle)
        {
            StartRecording();
            return;
        }

        StopRecording();
    }

    public void CaptureScreenshot()
    {
        ExecuteSafely(() =>
        {
            var artifact = CaptureScreenshotArtifact();
            if (artifact is null)
            {
                _statusService.SetStatus("已取消截图。", AppStatusLevel.Info);
                return;
            }

            _outputHistoryService.Add(artifact);
            HandleScreenshotPresentation(artifact);
            PublishArtifactStatus(artifact, TryCopyScreenshotToClipboard(artifact));
        });
    }

    public async void SaveReplay()
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

        var export = _replayBufferService.PrepareReplayExport(Session);
        if (export is null)
        {
            _statusService.SetStatus("当前还没有足够的回录缓存。", AppStatusLevel.Warning);
            return;
        }

        _isSavingReplay = true;
        _statusService.SetStatus("正在后台导出回录，仅保存画面。", AppStatusLevel.Info);

        try
        {
            var artifact = await Task.Run(() => _replayBufferService.SaveReplay(export));
            _outputHistoryService.Add(artifact);
            PublishArtifactStatus(artifact);
        }
        catch (Exception ex)
        {
            _statusService.SetStatus(ex.Message, AppStatusLevel.Error);
            MessageBox.Show(ex.Message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
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
        _mainWindow.Closed += (_, _) =>
        {
            if (RecordingService.Status != RecordingStatus.Idle)
            {
                RecordingService.Abort();
            }

            _recordingHudWindow?.Close();
            _recordingActiveWindow?.Close();
            _screenshotActionToastWindow?.Close();
            CloseStickerWindows();
            _hotKeyManager?.Dispose();
            _app.Shutdown();
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
    }

    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        ExecuteSafely(() => _sessionPersistenceService.Save(Session));

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

    public void SetShortcutCaptureSuspended(bool suspended)
    {
        if (_hotKeyManager is not null)
        {
            _hotKeyManager.IsSuspended = suspended;
        }
    }

    public void CloseAllScreenshotStickers()
    {
        CloseStickerWindows();
    }

    private void OnHotKeyPressed(object? sender, ShortcutAction action)
    {
        switch (action)
        {
            case ShortcutAction.Screenshot:
                CaptureScreenshot();
                break;
            case ShortcutAction.Recording:
                ToggleRecording();
                break;
            case ShortcutAction.Replay:
                SaveReplay();
                break;
        }
    }

    private static void ExecuteSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (Application.Current is App app)
            {
                app.WindowFlow._statusService.SetStatus(ex.Message, AppStatusLevel.Error);
            }
            MessageBox.Show(ex.Message, "LensSnap", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

    private CaptureArtifact? HandleRecordingStop(bool saveOutput)
    {
        if (!saveOutput)
        {
            RecordingService.Abort();
            return null;
        }

        var artifact = RecordingService.Stop();
        if (artifact is null)
        {
            return null;
        }

        _outputHistoryService.Add(artifact);
        PublishArtifactStatus(artifact);
        return artifact;
    }

    private CaptureArtifact? CaptureScreenshotArtifact()
    {
        return Session.ScreenshotCaptureMode switch
        {
            ScreenshotCaptureMode.Region => CaptureRegionScreenshot(),
            _ => _screenCaptureService.CaptureScreenshot(Session)
        };
    }

    private CaptureArtifact? CaptureRegionScreenshot()
    {
        var hiddenWindows = HideAppWindowsForCapture();
        try
        {
            Thread.Sleep(120);
            var selector = new RegionSelectionWindow();
            var confirmed = selector.ShowDialog();
            if (confirmed != true || selector.SelectedRegion is null)
            {
                return null;
            }

            return _screenCaptureService.CaptureScreenshotRegion(Session, selector.SelectedRegion.Value);
        }
        finally
        {
            RestoreHiddenWindows(hiddenWindows);
        }
    }

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
        var position = new Point(
            SystemParameters.WorkArea.Right - 460 - offset,
            SystemParameters.WorkArea.Top + 96 + offset);
        var window = new ScreenshotStickerWindow(artifact.OutputPath, position);
        window.CloseAllRequested += (_, _) => CloseStickerWindows();
        window.Closed += (_, _) => _stickerWindows.Remove(window);
        _stickerWindows.Add(window);
        window.Show();
    }

    private void ShowScreenshotActionToast(CaptureArtifact artifact)
    {
        if (!string.Equals(artifact.Kind, "截图", StringComparison.Ordinal))
        {
            return;
        }

        _screenshotActionToastWindow?.Close();
        var position = new Point(
            SystemParameters.WorkArea.Right - 400,
            SystemParameters.WorkArea.Bottom - 160);
        _screenshotActionToastWindow = new ScreenshotActionToastWindow(artifact.OutputPath, position);
        _screenshotActionToastWindow.ActionRequested += (_, action) => HandleScreenshotQuickAction(action, artifact);
        _screenshotActionToastWindow.Closed += (_, _) => _screenshotActionToastWindow = null;
        _screenshotActionToastWindow.Show();
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
