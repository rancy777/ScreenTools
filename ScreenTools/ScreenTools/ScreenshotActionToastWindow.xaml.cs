using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenTools;

public partial class ScreenshotActionToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer;

    public ScreenshotActionToastWindow(string screenshotPath, Point position)
    {
        InitializeComponent();
        Left = position.X;
        Top = position.Y;
        PathText.Text = Path.GetFileName(screenshotPath);
        ScreenshotPath = screenshotPath;
        LoadPreview(screenshotPath);
        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _closeTimer.Tick += (_, _) => Close();
        Loaded += (_, _) => _closeTimer.Start();
        Activated += (_, _) => RestartTimer();
        MouseEnter += (_, _) => _closeTimer.Stop();
        MouseLeave += (_, _) => RestartTimer();
    }

    public string ScreenshotPath { get; }

    public event EventHandler<ScreenshotQuickAction>? ActionRequested;

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke(this, ScreenshotQuickAction.PinToScreen);
        Close();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke(this, ScreenshotQuickAction.Open);
        Close();
    }

    private void RevealButton_Click(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke(this, ScreenshotQuickAction.RevealInFolder);
        Close();
    }

    private void DefaultSaveOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke(this, ScreenshotQuickAction.SetDefaultSaveOnly);
        Close();
    }

    private void DefaultPinButton_Click(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke(this, ScreenshotQuickAction.SetDefaultSaveAndPin);
        Close();
    }

    private void RestartTimer()
    {
        _closeTimer.Stop();
        _closeTimer.Start();
    }

    private void LoadPreview(string screenshotPath)
    {
        if (!File.Exists(screenshotPath))
        {
            return;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(screenshotPath, UriKind.Absolute);
        bitmap.DecodePixelWidth = 144;
        bitmap.EndInit();
        bitmap.Freeze();
        PreviewImage.Source = bitmap;
    }
}
