using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace ScreenTools;

public partial class MainWindow : Window
{
    private RecordingSessionState? _session;
    private OutputHistoryService? _outputHistoryService;
    private ClipboardManagerService? _clipboardManagerService;
    private AppStatusService? _statusService;
    private SystemAudioCaptureService? _systemAudioCaptureService;
    private bool _isSyncingFromSession;
    private ComboBox? _screenshotFormatComboBox;
    private ComboBox? _screenshotModeComboBox;
    private CheckBox? _microphoneToggle;
    private CheckBox? _systemAudioToggle;
    private ComboBox? _qualityPresetComboBox;
    private ComboBox? _screenshotBehaviorComboBox;
    private CheckBox? _copyScreenshotToggle;
    private CheckBox? _launchAtStartupToggle;
    private CheckBox? _replayBufferToggle;
    private RadioButton? _replay30RadioButton;
    private RadioButton? _replay60RadioButton;
    private Button? _screenshotShortcutButton;
    private Button? _recordingShortcutButton;
    private Button? _replayShortcutButton;
    private ShortcutAction? _capturingShortcutAction;
    private HashSet<string> _pendingShortcutTokens = new(StringComparer.OrdinalIgnoreCase);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ResolveControlReferences();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        PreviewMouseDown += MainWindow_PreviewMouseDown;
    }

    public void BindSession(RecordingSessionState session)
    {
        _session = session;
        _session.StateChanged += (_, _) => ApplySessionState();
        ApplySessionState();
    }

    public void BindOutputHistory(OutputHistoryService outputHistoryService)
    {
        _outputHistoryService = outputHistoryService;
        _outputHistoryService.HistoryChanged += (_, _) => RenderRecentOutputs();
        RenderRecentOutputs();
    }

    public void BindClipboardHistory(ClipboardManagerService clipboardManagerService)
    {
        _clipboardManagerService = clipboardManagerService;
        _clipboardManagerService.HistoryChanged += (_, _) => RenderClipboardEntries();
        RenderClipboardEntries();
    }

    public void BindStatus(AppStatusService statusService)
    {
        _statusService = statusService;
        _statusService.StatusChanged += (_, _) => RenderStatus();
        RenderStatus();
    }

    public void BindSystemAudioCapture(SystemAudioCaptureService systemAudioCaptureService)
    {
        _systemAudioCaptureService = systemAudioCaptureService;
        RenderEnvironmentSummary();
    }

    private void ApplySessionState()
    {
        if (_session is null)
        {
            return;
        }

        _isSyncingFromSession = true;

        _screenshotFormatComboBox ??= FindDescendant<ComboBox>(ScreenshotFormatRow);
        _screenshotModeComboBox ??= FindDescendant<ComboBox>(ScreenshotModeRow);
        _microphoneToggle ??= FindDescendant<CheckBox>(MicrophoneRow);
        _systemAudioToggle ??= FindDescendant<CheckBox>(SystemAudioRow);
        _qualityPresetComboBox ??= FindDescendant<ComboBox>(QualityPresetRow);
        _screenshotBehaviorComboBox ??= FindDescendant<ComboBox>(ScreenshotBehaviorRow);
        _copyScreenshotToggle ??= FindDescendant<CheckBox>(CopyScreenshotRow);
        _launchAtStartupToggle ??= FindDescendant<CheckBox>(LaunchAtStartupRow);
        _replayBufferToggle ??= FindDescendant<CheckBox>(ReplayBufferRow);
        _replay30RadioButton ??= FindDescendantByTag<RadioButton>(ReplayDurationRow, "30");
        _replay60RadioButton ??= FindDescendantByTag<RadioButton>(ReplayDurationRow, "60");
        _screenshotShortcutButton ??= FindDescendantByTag<Button>(ScreenshotFormatRow, "Screenshot");
        _recordingShortcutButton ??= FindDescendantByTag<Button>(MicrophoneRow, "Recording");
        _replayShortcutButton ??= FindDescendantByTag<Button>(ReplayDurationRow, "Replay");

        if (_screenshotFormatComboBox is not null)
        {
            _screenshotFormatComboBox.SelectedIndex = _session.ScreenshotFormat switch
            {
                "JPG" => 1,
                "BMP" => 2,
                _ => 0
            };
        }

        if (_screenshotModeComboBox is not null)
        {
            _screenshotModeComboBox.SelectedIndex = _session.ScreenshotCaptureMode == ScreenshotCaptureMode.Region ? 1 : 0;
        }

        if (_microphoneToggle is not null)
        {
            _microphoneToggle.IsChecked = _session.IncludeMicrophone;
        }

        if (_systemAudioToggle is not null)
        {
            _systemAudioToggle.IsChecked = _session.IncludeSystemAudio;
        }

        if (_qualityPresetComboBox is not null)
        {
            _qualityPresetComboBox.SelectedIndex = _session.QualityPreset switch
            {
                "720P 30FPS" => 0,
                "1440P 30FPS" => 2,
                _ => 1
            };
        }

        if (_screenshotBehaviorComboBox is not null)
        {
            _screenshotBehaviorComboBox.SelectedIndex = _session.ScreenshotAfterCapture switch
            {
                ScreenshotAfterCaptureBehavior.SaveOnly => 1,
                ScreenshotAfterCaptureBehavior.SaveAndPin => 2,
                _ => 0
            };
        }

        if (_copyScreenshotToggle is not null)
        {
            _copyScreenshotToggle.IsChecked = _session.CopyScreenshotToClipboard;
        }

        if (_launchAtStartupToggle is not null)
        {
            _launchAtStartupToggle.IsChecked = _session.LaunchAtStartup;
        }

        if (_replayBufferToggle is not null)
        {
            _replayBufferToggle.IsChecked = _session.ReplayBufferEnabled;
        }

        if (_replay30RadioButton is not null && _replay60RadioButton is not null)
        {
            _replay30RadioButton.IsChecked = _session.ReplaySeconds == 30;
            _replay60RadioButton.IsChecked = _session.ReplaySeconds == 60;
        }

        OutputDirectoryRow.Description = _session.OutputDirectory;
        RenderShortcutButtons();
        RenderEnvironmentSummary();

        _isSyncingFromSession = false;
    }

    private void ScreenshotFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null || sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _session.ScreenshotFormat = item.Content?.ToString() ?? "PNG";
    }

    private void MicrophoneToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.IncludeMicrophone = sender is CheckBox checkBox && checkBox.IsChecked == true;
    }

    private void SystemAudioToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.IncludeSystemAudio = sender is CheckBox checkBox && checkBox.IsChecked == true;
    }

    private void ReplayDuration_Checked(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null || sender is not RadioButton radioButton)
        {
            return;
        }

        if (int.TryParse(radioButton.Tag?.ToString(), out var replaySeconds))
        {
            _session.ReplaySeconds = replaySeconds;
        }
    }

    private void ReplayBufferToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.ReplayBufferEnabled = sender is CheckBox checkBox && checkBox.IsChecked == true;
    }

    private void LaunchAtStartupToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.LaunchAtStartup = sender is CheckBox checkBox && checkBox.IsChecked == true;
    }

    private void QualityPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null || sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _session.QualityPreset = item.Content?.ToString() ?? "1080P 60FPS";
    }

    private void ScreenshotModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null || sender is not ComboBox comboBox)
        {
            return;
        }

        _session.ScreenshotCaptureMode = comboBox.SelectedIndex == 1
            ? ScreenshotCaptureMode.Region
            : ScreenshotCaptureMode.FullScreen;
    }

    private void ScreenshotBehaviorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null || sender is not ComboBox comboBox)
        {
            return;
        }

        _session.ScreenshotAfterCapture = comboBox.SelectedIndex switch
        {
            1 => ScreenshotAfterCaptureBehavior.SaveOnly,
            2 => ScreenshotAfterCaptureBehavior.SaveAndPin,
            _ => ScreenshotAfterCaptureBehavior.ShowQuickActions
        };
    }

    private void CopyScreenshotToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingFromSession || _session is null)
        {
            return;
        }

        _session.CopyScreenshotToClipboard = sender is CheckBox checkBox && checkBox.IsChecked == true;
    }

    private void ChangeOutputDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null)
        {
            return;
        }

        using var dialog = new Forms.FolderBrowserDialog
        {
            InitialDirectory = _session.OutputDirectory,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            _session.OutputDirectory = dialog.SelectedPath;
        }
    }

    private void CloseAllStickers_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).WindowFlow.CloseAllScreenshotStickers();
        _statusService?.SetStatus("已关闭所有截图贴纸。", AppStatusLevel.Info);
    }

    private void ResolveControlReferences()
    {
        _screenshotFormatComboBox ??= FindDescendant<ComboBox>(ScreenshotFormatRow);
        _screenshotModeComboBox ??= FindDescendant<ComboBox>(ScreenshotModeRow);
        _microphoneToggle ??= FindDescendant<CheckBox>(MicrophoneRow);
        _systemAudioToggle ??= FindDescendant<CheckBox>(SystemAudioRow);
        _copyScreenshotToggle ??= FindDescendant<CheckBox>(CopyScreenshotRow);
        _launchAtStartupToggle ??= FindDescendant<CheckBox>(LaunchAtStartupRow);
        _replayBufferToggle ??= FindDescendant<CheckBox>(ReplayBufferRow);
        _replay30RadioButton ??= FindDescendantByTag<RadioButton>(ReplayDurationRow, "30");
        _replay60RadioButton ??= FindDescendantByTag<RadioButton>(ReplayDurationRow, "60");
        _screenshotBehaviorComboBox ??= FindDescendant<ComboBox>(ScreenshotBehaviorRow);
        _screenshotShortcutButton ??= FindDescendantByTag<Button>(ScreenshotFormatRow, "Screenshot");
        _recordingShortcutButton ??= FindDescendantByTag<Button>(MicrophoneRow, "Recording");
        _replayShortcutButton ??= FindDescendantByTag<Button>(ReplayDurationRow, "Replay");
        _qualityPresetComboBox ??= FindDescendant<ComboBox>(QualityPresetRow);
        ApplySessionState();
        RenderClipboardEntries();
        RenderRecentOutputs();
        RenderStatus();
        RenderEnvironmentSummary();
    }

    private void RenderClipboardEntries()
    {
        if (!IsInitialized || ClipboardEntriesHost is null || ClipboardEmptyState is null)
        {
            return;
        }

        ClipboardEntriesHost.Children.Clear();
        var entries = _clipboardManagerService?.Entries ?? Array.Empty<ClipboardEntry>();
        ClipboardEmptyState.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var entry in entries)
        {
            ClipboardEntriesHost.Children.Add(BuildClipboardEntryRow(entry));
        }
    }

    private FrameworkElement BuildClipboardEntryRow(ClipboardEntry entry)
    {
        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(14, 12, 14, 12),
            Background = (Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (Brush)FindResource("HairlineBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12)
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var preview = BuildClipboardPreview(entry);
        Grid.SetColumn(preview, 0);
        root.Children.Add(preview);

        var infoStack = new StackPanel
        {
            Margin = new Thickness(14, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        titleRow.Children.Add(new TextBlock
        {
            Text = $"{entry.Kind}  {Path.GetFileName(entry.OutputPath)}",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("HeadingBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });
        titleRow.Children.Add(BuildClipboardStatusChip(entry));
        infoStack.Children.Add(titleRow);
        infoStack.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 4, 0, 0),
            Text = $"{entry.CreatedAt.LocalDateTime:MM-dd HH:mm:ss} · {entry.Detail}",
            FontSize = 12,
            Foreground = (Brush)FindResource("SubtleTextBrush"),
            TextWrapping = TextWrapping.Wrap
        });

        Grid.SetColumn(infoStack, 1);
        root.Children.Add(infoStack);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        actions.Children.Add(CreateActionButton(entry.CopySucceeded ? "再次复制" : "重试复制", entry.OutputPath, RetryClipboardCopy_Click));
        actions.Children.Add(CreateActionButton("打开", entry.OutputPath, OpenRecentOutput_Click, new Thickness(8, 0, 0, 0)));
        actions.Children.Add(CreateActionButton("定位", entry.OutputPath, OpenRecentOutputFolder_Click, new Thickness(8, 0, 0, 0)));

        Grid.SetColumn(actions, 2);
        root.Children.Add(actions);

        border.Child = root;
        return border;
    }

    private void RenderRecentOutputs()
    {
        if (!IsInitialized || RecentOutputsHost is null || RecentOutputsEmptyState is null)
        {
            return;
        }

        RecentOutputsHost.Children.Clear();
        var entries = _outputHistoryService?.Entries ?? Array.Empty<RecentOutputEntry>();
        RecentOutputsEmptyState.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var entry in entries)
        {
            RecentOutputsHost.Children.Add(BuildRecentOutputRow(entry));
        }
    }

    private FrameworkElement BuildRecentOutputRow(RecentOutputEntry entry)
    {
        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(14, 12, 14, 12),
            Background = (Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (Brush)FindResource("HairlineBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12)
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition());
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoStack = new StackPanel();
        infoStack.Children.Add(new TextBlock
        {
            Text = $"{entry.Kind}  {Path.GetFileName(entry.OutputPath)}",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("HeadingBrush")
        });
        infoStack.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 4, 0, 0),
            Text = $"{entry.CreatedAt.LocalDateTime:MM-dd HH:mm} · {GetEntrySummary(entry)}",
            FontSize = 12,
            Foreground = (Brush)FindResource("SubtleTextBrush")
        });

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        if (!entry.IsVideo)
        {
            actions.Children.Add(CreateActionButton("复制", entry.OutputPath, RetryClipboardCopy_Click));
        }

        actions.Children.Add(CreateActionButton("打开", entry.OutputPath, OpenRecentOutput_Click));
        actions.Children.Add(CreateActionButton("定位", entry.OutputPath, OpenRecentOutputFolder_Click, new Thickness(8, 0, 0, 0)));

        Grid.SetColumn(actions, 1);
        root.Children.Add(infoStack);
        root.Children.Add(actions);
        border.Child = root;
        return border;
    }

    private FrameworkElement BuildClipboardPreview(ClipboardEntry entry)
    {
        var container = new Border
        {
            Width = 86,
            Height = 58,
            Background = (Brush)FindResource("KeycapBrush"),
            BorderBrush = (Brush)FindResource("KeycapBorderBrush"),
            BorderThickness = new Thickness(0.5),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (File.Exists(entry.OutputPath))
        {
            var image = new Image
            {
                Stretch = Stretch.UniformToFill
            };

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(entry.OutputPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                image.Source = bitmap;
                container.Child = image;
                return container;
            }
            catch
            {
            }
        }

        container.Child = new TextBlock
        {
            Text = "无预览",
            FontSize = 11,
            Foreground = (Brush)FindResource("SubtleTextBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        return container;
    }

    private FrameworkElement BuildClipboardStatusChip(ClipboardEntry entry)
    {
        var chip = new Border
        {
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(8, 2, 8, 2),
            CornerRadius = new CornerRadius(8),
            Background = (Brush)new BrushConverter().ConvertFromString(entry.CopySucceeded ? "#12005EB1" : "#14D97706")!
        };

        chip.Child = new TextBlock
        {
            Text = entry.CopySucceeded ? "已复制" : "复制失败",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)new BrushConverter().ConvertFromString(entry.CopySucceeded ? "#FF005EB1" : "#FFB45309")!
        };
        return chip;
    }

    private Button CreateActionButton(string content, string path, RoutedEventHandler clickHandler, Thickness? margin = null)
    {
        var button = new Button
        {
            Content = content,
            Tag = path,
            Margin = margin ?? new Thickness(0),
            MinWidth = 72
        };

        if (TryFindResource("LinkActionButtonStyle") is Style style)
        {
            button.Style = style;
        }

        button.Click += clickHandler;
        return button;
    }

    private void RetryClipboardCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path } || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var result = _clipboardManagerService?.CopyImageFromFile(path, "截图");
        if (result is null)
        {
            return;
        }

        var message = result.Success
            ? $"已重新复制到剪贴板：{Path.GetFileName(path)}"
            : $"复制失败：{Path.GetFileName(path)} · {result.Detail}";
        _statusService?.SetStatus(message, result.Success ? AppStatusLevel.Success : AppStatusLevel.Warning);
    }

    private static string GetEntrySummary(RecentOutputEntry entry)
    {
        if (entry.IsVideo)
        {
            return $"视频输出 · {entry.FrameCount} 帧";
        }

        return entry.FrameCount <= 1
            ? "单次输出"
            : $"{entry.FrameCount} 帧";
    }

    private static void OpenRecentOutput_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path } || string.IsNullOrWhiteSpace(path))
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

    private static void OpenRecentOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path } || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return;
        }

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

    private void RenderStatus()
    {
        if (!IsInitialized || _statusService is null)
        {
            return;
        }

        StatusMessageText.Text = _statusService.Message;
        StatusMetaText.Text = $"{_statusService.UpdatedAt.LocalDateTime:MM-dd HH:mm:ss} · 最近状态";

        var (background, border, indicator) = _statusService.Level switch
        {
            AppStatusLevel.Success => ("#FFF1FAF3", "#33248A3D", "#248A3D"),
            AppStatusLevel.Warning => ("#FFFFF7ED", "#33D97706", "#D97706"),
            AppStatusLevel.Error => ("#FFFEF2F2", "#33DC2626", "#DC2626"),
            _ => ("#FFF6F7F8", "#221E3A5F", "#2563EB")
        };

        StatusBanner.Background = (Brush)new BrushConverter().ConvertFromString(background)!;
        StatusBanner.BorderBrush = (Brush)new BrushConverter().ConvertFromString(border)!;
        StatusIndicator.Fill = (Brush)new BrushConverter().ConvertFromString(indicator)!;
    }

    private void RenderEnvironmentSummary()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (_systemAudioCaptureService is not null)
        {
            SystemAudioRow.Description = _systemAudioCaptureService.AvailabilityMessage;
        }
    }

    private void RenderShortcutCaptureBanner()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (_capturingShortcutAction is null)
        {
            ShortcutCaptureBanner.Visibility = Visibility.Collapsed;
            return;
        }

        var preview = _pendingShortcutTokens.Count == 0
            ? "请按下键盘或鼠标组合"
            : ShortcutGesture.FromEnumerable(_pendingShortcutTokens).DisplayText;
        ShortcutCaptureTitleText.Text = $"正在录入{GetActionLabel(_capturingShortcutAction.Value)}快捷键";
        ShortcutCaptureHintText.Text = $"{preview} · Esc 取消 · Backspace 清空";
        ShortcutCaptureBanner.Visibility = Visibility.Visible;
    }

    private void ShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
        {
            return;
        }

        if (!Enum.TryParse<ShortcutAction>(tag, out var action))
        {
            return;
        }

        _capturingShortcutAction = action;
        _pendingShortcutTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ((App)Application.Current).WindowFlow.SetShortcutCaptureSuspended(true);
        _statusService?.SetStatus("正在录入快捷键。按下键盘或鼠标组合，按 Esc 取消，按 Backspace 清空。", AppStatusLevel.Info);
        RenderShortcutButtons();
        RenderShortcutCaptureBanner();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturingShortcutAction is null)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            FinishShortcutCapture(applyChange: false);
            e.Handled = true;
            return;
        }

        if (key == Key.Back || key == Key.Delete)
        {
            ApplyCapturedShortcut(ShortcutGesture.FromEnumerable([]));
            e.Handled = true;
            return;
        }

        var token = NormalizeCaptureKey(key);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _pendingShortcutTokens.Add(token);
        RenderShortcutCaptureBanner();
        if (!ShortcutGesture.IsModifier(token))
        {
            ApplyCapturedShortcut(ShortcutGesture.FromEnumerable(_pendingShortcutTokens));
        }

        e.Handled = true;
    }

    private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_capturingShortcutAction is null)
        {
            return;
        }

        if (e.ChangedButton == MouseButton.Left && e.OriginalSource is DependencyObject source && IsShortcutButton(source))
        {
            return;
        }

        var token = e.ChangedButton switch
        {
            MouseButton.Left => "LeftMouse",
            MouseButton.Right => "RightMouse",
            MouseButton.Middle => "MiddleMouse",
            MouseButton.XButton1 => "XButton1",
            MouseButton.XButton2 => "XButton2",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _pendingShortcutTokens.Add(token);
        RenderShortcutCaptureBanner();
        ApplyCapturedShortcut(ShortcutGesture.FromEnumerable(_pendingShortcutTokens));
        e.Handled = true;
    }

    private void ApplyCapturedShortcut(ShortcutGesture gesture)
    {
        if (_capturingShortcutAction is null || _session is null)
        {
            return;
        }

        if (!ValidateShortcutGesture(gesture, out var validationMessage))
        {
            _statusService?.SetStatus(validationMessage, AppStatusLevel.Warning);
            _pendingShortcutTokens.Clear();
            RenderShortcutButtons();
            RenderShortcutCaptureBanner();
            return;
        }

        if (TryGetConflictingAction(_capturingShortcutAction.Value, gesture, out var conflictingAction))
        {
            _statusService?.SetStatus(
                $"快捷键冲突：{gesture.DisplayText} 已用于{GetActionLabel(conflictingAction)}。",
                AppStatusLevel.Warning);
            _pendingShortcutTokens.Clear();
            RenderShortcutButtons();
            RenderShortcutCaptureBanner();
            return;
        }

        switch (_capturingShortcutAction.Value)
        {
            case ShortcutAction.Screenshot:
                _session.ScreenshotShortcut = gesture;
                break;
            case ShortcutAction.Recording:
                _session.RecordingShortcut = gesture;
                break;
            case ShortcutAction.Replay:
                _session.ReplayShortcut = gesture;
                break;
        }

        FinishShortcutCapture(applyChange: true);
    }

    private void FinishShortcutCapture(bool applyChange)
    {
        _capturingShortcutAction = null;
        _pendingShortcutTokens.Clear();
        ((App)Application.Current).WindowFlow.SetShortcutCaptureSuspended(false);
        RenderShortcutButtons();
        RenderShortcutCaptureBanner();

        if (_session is null || _statusService is null)
        {
            return;
        }

        if (applyChange)
        {
            _statusService.SetStatus("快捷键已更新。", AppStatusLevel.Success);
        }
        else
        {
            _statusService.SetStatus("已取消快捷键录入。", AppStatusLevel.Info);
        }
    }

    private void RenderShortcutButtons()
    {
        if (!IsInitialized || _session is null)
        {
            return;
        }

        if (_screenshotShortcutButton is not null)
        {
            _screenshotShortcutButton.Content = GetShortcutButtonText(ShortcutAction.Screenshot, _session.ScreenshotShortcut);
        }

        if (_recordingShortcutButton is not null)
        {
            _recordingShortcutButton.Content = GetShortcutButtonText(ShortcutAction.Recording, _session.RecordingShortcut);
        }

        if (_replayShortcutButton is not null)
        {
            _replayShortcutButton.Content = GetShortcutButtonText(ShortcutAction.Replay, _session.ReplayShortcut);
        }
    }

    private string GetShortcutButtonText(ShortcutAction action, ShortcutGesture gesture)
    {
        if (_capturingShortcutAction == action)
        {
            var preview = _pendingShortcutTokens.Count == 0
                ? "请按组合"
                : ShortcutGesture.FromEnumerable(_pendingShortcutTokens).DisplayText;
            return $"录入中: {preview}";
        }

        return gesture.DisplayText;
    }

    private bool TryGetConflictingAction(ShortcutAction currentAction, ShortcutGesture gesture, out ShortcutAction conflictingAction)
    {
        conflictingAction = currentAction;
        if (_session is null || gesture.IsEmpty)
        {
            return false;
        }

        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            if (action == currentAction)
            {
                continue;
            }

            var existing = GetShortcutGesture(action);
            if (existing.Tokens.Count == gesture.Tokens.Count &&
                existing.Tokens.SequenceEqual(gesture.Tokens, StringComparer.OrdinalIgnoreCase))
            {
                conflictingAction = action;
                return true;
            }
        }

        return false;
    }

    private ShortcutGesture GetShortcutGesture(ShortcutAction action)
    {
        return action switch
        {
            ShortcutAction.Screenshot => _session?.ScreenshotShortcut ?? ShortcutGesture.CreateDefault(action),
            ShortcutAction.Recording => _session?.RecordingShortcut ?? ShortcutGesture.CreateDefault(action),
            ShortcutAction.Replay => _session?.ReplayShortcut ?? ShortcutGesture.CreateDefault(action),
            _ => ShortcutGesture.CreateDefault(action)
        };
    }

    private static string GetActionLabel(ShortcutAction action)
    {
        return action switch
        {
            ShortcutAction.Screenshot => "截图",
            ShortcutAction.Recording => "录制",
            ShortcutAction.Replay => "回录",
            _ => action.ToString()
        };
    }

    private static bool ValidateShortcutGesture(ShortcutGesture gesture, out string message)
    {
        message = string.Empty;
        if (gesture.IsEmpty)
        {
            return true;
        }

        var nonModifierTokens = gesture.Tokens.FindAll(token => !ShortcutGesture.IsModifier(token));
        if (nonModifierTokens.Count == 0)
        {
            message = "快捷键不能只包含 Ctrl、Alt、Shift 或 Win。";
            return false;
        }

        return true;
    }

    private static bool IsShortcutButton(DependencyObject source)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.Tag is string tag &&
                (tag == nameof(ShortcutAction.Screenshot) || tag == nameof(ShortcutAction.Recording) || tag == nameof(ShortcutAction.Replay)))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static string NormalizeCaptureKey(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => "Ctrl",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LWin or Key.RWin => "Win",
            Key.System or Key.None => string.Empty,
            >= Key.A and <= Key.Z => key.ToString().ToUpperInvariant(),
            >= Key.D0 and <= Key.D9 => key.ToString()[1..],
            >= Key.NumPad0 and <= Key.NumPad9 => key.ToString().Replace("NumPad", "Num"),
            Key.OemPlus => "=",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemTilde => "`",
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Tab => "Tab",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            _ when key >= Key.F1 && key <= Key.F24 => key.ToString().ToUpperInvariant(),
            _ => key.ToString()
        };
    }

    private static T? FindDescendant<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root is null)
        {
            return null;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static T? FindDescendantByTag<T>(DependencyObject? root, string tag) where T : FrameworkElement
    {
        foreach (var element in FindDescendants<T>(root))
        {
            if (element.Tag?.ToString() == tag)
            {
                return element;
            }
        }

        return null;
    }

    private static System.Collections.Generic.IEnumerable<T> FindDescendants<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root is null)
        {
            yield break;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindDescendants<T>(child))
            {
                yield return nested;
            }
        }
    }
}
