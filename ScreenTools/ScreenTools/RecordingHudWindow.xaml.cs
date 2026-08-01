using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenTools;

public partial class RecordingHudWindow : Window
{
    private RecordingSessionState? _session;
    private AppStatusService? _statusService;
    private bool _isSyncingFromSession;

    public RecordingHudWindow()
    {
        InitializeComponent();
    }

    public void BindSession(RecordingSessionState session)
    {
        _session = session;
        _session.StateChanged += (_, _) => ApplySessionState();
        ApplySessionState();
    }

    public void BindStatus(AppStatusService statusService)
    {
        _statusService = statusService;
        _statusService.StatusChanged += (_, _) => RenderStatus();
        RenderStatus();
    }

    private void ApplySessionState()
    {
        if (_session is null || !IsVisible)
        {
            return;
        }

        _isSyncingFromSession = true;

        HudMicrophoneToggle.IsChecked = _session.IncludeMicrophone;
        HudSystemAudioToggle.IsChecked = _session.IncludeSystemAudio;
        HudQualityPresetComboBox.SelectedIndex = _session.QualityPreset switch
        {
            "720P 30FPS" => 0,
            "1440P 30FPS" => 2,
            _ => 1
        };
        ReplayDurationBadgeText.Text = $"{_session.ReplaySeconds}s";

        _isSyncingFromSession = false;
    }

    private void RenderStatus()
    {
        if (_statusService is null || !IsVisible)
        {
            return;
        }

        HudStatusMessageText.Text = _statusService.Message;
        HudStatusMetaText.Text = $"{_statusService.UpdatedAt.LocalDateTime:HH:mm:ss} · 最近状态";

        var (background, border, indicator) = _statusService.Level switch
        {
            AppStatusLevel.Success => ("#FFF1FAF3", "#33248A3D", "#248A3D"),
            AppStatusLevel.Warning => ("#FFFFF7ED", "#33D97706", "#D97706"),
            AppStatusLevel.Error => ("#FFFEF2F2", "#33DC2626", "#DC2626"),
            _ => ("#FFF6F7F8", "#221E3A5F", "#2563EB")
        };

        HudStatusBanner.Background = (Brush)new BrushConverter().ConvertFromString(background)!;
        HudStatusBanner.BorderBrush = (Brush)new BrushConverter().ConvertFromString(border)!;
        HudStatusIndicator.Fill = (Brush)new BrushConverter().ConvertFromString(indicator)!;
    }

    private void BackToSettings_Click(object sender, MouseButtonEventArgs e)
    {
        ((App)Application.Current).WindowFlow.ExitToSettings();
    }

    private void StartRecording_Click(object sender, MouseButtonEventArgs e)
    {
        ((App)Application.Current).WindowFlow.StartRecording();
    }

    private void HudMicrophoneToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.IncludeMicrophone = HudMicrophoneToggle.IsChecked == true;
    }

    private void HudSystemAudioToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.IncludeSystemAudio = HudSystemAudioToggle.IsChecked == true;
    }

    private void HudQualityPresetComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null || HudQualityPresetComboBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item)
        {
            return;
        }

        _session.QualityPreset = item.Content?.ToString() ?? "1080P 60FPS";
    }
}
