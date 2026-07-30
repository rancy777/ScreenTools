using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ScreenTools;

public partial class RecordingActiveWindow : Window
{
    private RecordingSessionState? _session;
    private RecordingService? _recordingService;

    public RecordingActiveWindow()
    {
        InitializeComponent();
    }

    public void BindSession(RecordingSessionState session)
    {
        _session = session;
    }

    public void BindRecordingService(RecordingService recordingService)
    {
        _recordingService = recordingService;
        _recordingService.ElapsedChanged += (_, elapsed) =>
        {
            ElapsedTimeText.Text = elapsed.ToString(@"hh\:mm\:ss");
        };
        _recordingService.StatusChanged += (_, status) =>
        {
            UpdateStatus(status);
        };
        _recordingService.RuntimeInfoChanged += (_, _) =>
        {
            UpdateRuntimeInfo();
        };
        UpdateRuntimeInfo();
    }

    private void PauseResume_Click(object sender, MouseButtonEventArgs e)
    {
        _recordingService?.TogglePause();
    }

    private void StopRecording_Click(object sender, MouseButtonEventArgs e)
    {
        ((App)Application.Current).WindowFlow.StopRecording();
    }

    private void UpdateStatus(RecordingStatus status)
    {
        Title = status switch
        {
            RecordingStatus.Recording when _session is not null => $"Recording Active HUD - {_session.QualityPreset}",
            RecordingStatus.Paused when _session is not null => $"Recording Active HUD - Paused - {_session.QualityPreset}",
            RecordingStatus.Paused => "Recording Active HUD - Paused",
            RecordingStatus.Recording => "Recording Active HUD",
            _ => "Recording Active HUD"
        };

        ElapsedTimeText.Text = _recordingService?.Elapsed.ToString(@"hh\:mm\:ss") ?? "00:00:00";

        var pauseOpacity = status == RecordingStatus.Paused ? 0.35 : 1.0;
        PauseLeftBar.Opacity = pauseOpacity;
        PauseRightBar.Opacity = pauseOpacity;
        UpdateRuntimeInfo();
    }

    private void UpdateRuntimeInfo()
    {
        ActiveAudioSummaryText.Text = _recordingService?.CurrentAudioSummary ?? "等待开始录制";
        ActiveOutputSummaryText.Text = _recordingService?.CurrentOutputSummary
            ?? (_session is null ? "尚未开始" : $"{_session.QualityPreset} · 输出到 设置目录");
    }
}
