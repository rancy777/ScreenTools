using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ScreenTools;

public partial class ScreenshotStickerWindow : Window
{
    private const double MinZoom = 0.2;
    private const double MaxZoom = 3.0;
    private static readonly double[] OpacityLevels = [1.0, 0.85, 0.7, 0.55];
    private double _zoom = 1.0;
    private int _opacityIndex;
    private string _imagePath;

    public ScreenshotStickerWindow(string imagePath, Point position)
    {
        InitializeComponent();
        Left = position.X;
        Top = position.Y;
        _imagePath = imagePath;
        LoadImage(imagePath);
        UpdateZoomText();
        UpdateOpacityText();
    }

    public event EventHandler? CloseAllRequested;

    private void LoadImage(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("未找到截图文件。", imagePath);
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        PreviewImage.Source = bitmap;
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Close();
            return;
        }

        DragMove();
    }

    private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _zoom = Math.Clamp(_zoom + (e.Delta > 0 ? 0.1 : -0.1), MinZoom, MaxZoom);
        ImageScaleTransform.ScaleX = _zoom;
        ImageScaleTransform.ScaleY = _zoom;
        UpdateZoomText();
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseAllButton_Click(object sender, RoutedEventArgs e)
    {
        CloseAllRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_imagePath))
        {
            ((App)Application.Current).WindowFlow.CopyImageToClipboard(_imagePath, "贴纸截图");
        }
    }

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_imagePath))
        {
            return;
        }

        var extension = Path.GetExtension(_imagePath);
        var dialog = new SaveFileDialog
        {
            FileName = Path.GetFileName(_imagePath),
            DefaultExt = extension,
            Filter = "PNG 图像|*.png|JPEG 图像|*.jpg;*.jpeg|BMP 图像|*.bmp|所有文件|*.*"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        File.Copy(_imagePath, dialog.FileName, overwrite: true);
        _imagePath = dialog.FileName;
    }

    private void OpacityButton_Click(object sender, RoutedEventArgs e)
    {
        _opacityIndex = (_opacityIndex + 1) % OpacityLevels.Length;
        Opacity = OpacityLevels[_opacityIndex];
        UpdateOpacityText();
    }

    private void UpdateZoomText()
    {
        ZoomText.Text = $"{Math.Round(_zoom * 100):0}%";
    }

    private void UpdateOpacityText()
    {
        OpacityText.Text = $"透明 {Math.Round(Opacity * 100):0}%";
    }
}
