using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ScreenTools;

/// <summary>
/// Presents the active recording controls and runtime information.
/// </summary>
public partial class RecordingActiveWindow : Window
{
    private RecordingSessionState? _session;
    private RecordingService? _recordingService;

    /// <summary>
    /// Initializes the active-recording window and its command bindings.
    /// </summary>
    public RecordingActiveWindow()
    {
        InitializeComponent();
        LocalizationManager.RegisterWindow(this);
    }

    /// <summary>
    /// Connects the window to the shared session settings and their change notifications.
    /// </summary>
    public void BindSession(RecordingSessionState session)
    {
        _session = session;
    }

    /// <summary>
    /// Connects the active-recording window to recording lifecycle and runtime information.
    /// </summary>
    public void BindRecordingService(RecordingService recordingService)
    {
        _recordingService = recordingService;
        _recordingService.ElapsedChanged += (_, elapsed) =>
        {
            RunOnUiThread(() => ElapsedTimeText.Text = elapsed.ToString(@"hh\:mm\:ss"));
        };
        _recordingService.StatusChanged += (_, status) =>
        {
            RunOnUiThread(() => UpdateStatus(status));
        };
        _recordingService.RuntimeInfoChanged += (_, _) =>
        {
            RunOnUiThread(UpdateRuntimeInfo);
        };
        UpdateRuntimeInfo();
    }

    private void PauseResume_Click(object sender, MouseButtonEventArgs e)
    {
        _recordingService?.TogglePause();
    }

    private void StopRecording_Click(object sender, MouseButtonEventArgs e)
    {
        _ = ((App)Application.Current).WindowFlow.StopRecordingAsync();
    }

    private void UpdateStatus(RecordingStatus status)
    {
        var title = status switch
        {
            RecordingStatus.Finalizing => "正在完成录制",
            RecordingStatus.Recording when _session is not null => $"录制状态栏 - {_session.QualityPreset}",
            RecordingStatus.Paused when _session is not null => $"录制状态栏 - 已暂停 - {_session.QualityPreset}",
            RecordingStatus.Paused => "录制状态栏 - 已暂停",
            _ => "录制状态栏"
        };
        Title = LocalizationManager.Translate(title);
        var canControlRecording = status is RecordingStatus.Recording or RecordingStatus.Paused;
        PauseButton.IsEnabled = canControlRecording;
        StopButton.IsEnabled = canControlRecording;
        PauseButton.Opacity = canControlRecording ? 1.0 : 0.45;
        StopButton.Opacity = canControlRecording ? 1.0 : 0.45;

        ElapsedTimeText.Text = _recordingService?.Elapsed.ToString(@"hh\:mm\:ss") ?? "00:00:00";

        var pauseOpacity = status == RecordingStatus.Paused ? 0.35 : 1.0;
        PauseLeftBar.Opacity = pauseOpacity;
        PauseRightBar.Opacity = pauseOpacity;
        UpdateRuntimeInfo();
    }

    private void UpdateRuntimeInfo()
    {
        ActiveAudioSummaryText.Text = LocalizationManager.Translate(_recordingService?.CurrentAudioSummary ?? "等待开始录制");
        ActiveOutputSummaryText.Text = LocalizationManager.Translate(_recordingService?.CurrentOutputSummary
            ?? (_session is null ? "尚未开始" : $"{_session.QualityPreset} · 输出到 设置目录"));
    }

    private void RunOnUiThread(Action action)
    {
        if (Dispatcher.CheckAccess())
            action();
        else if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            Dispatcher.BeginInvoke(action);
    }
}
