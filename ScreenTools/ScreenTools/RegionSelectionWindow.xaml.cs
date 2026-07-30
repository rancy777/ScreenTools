using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Drawing = System.Drawing;
using DrawingImaging = System.Drawing.Imaging;
using Forms = System.Windows.Forms;

namespace ScreenTools;

public partial class RegionSelectionWindow : Window
{
    private const int MagnifierSampleSize = 18;
    private Point? _dragStartPoint;

    public RegionSelectionWindow()
    {
        InitializeComponent();
        var bounds = Forms.SystemInformation.VirtualScreen;
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        Loaded += (_, _) => UpdateInspector(Mouse.GetPosition(this));
    }

    public Rect? SelectedRegion { get; private set; }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        UpdateSelection(_dragStartPoint.Value, _dragStartPoint.Value);
        SelectionRect.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        var current = e.GetPosition(this);
        UpdateInspector(current);

        if (_dragStartPoint is null)
        {
            return;
        }

        UpdateSelection(_dragStartPoint.Value, current);
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStartPoint is null)
        {
            return;
        }

        var current = e.GetPosition(this);
        ReleaseMouseCapture();
        SelectedRegion = BuildAbsoluteRect(_dragStartPoint.Value, current);
        _dragStartPoint = null;

        if (SelectedRegion.Value.Width < 4 || SelectedRegion.Value.Height < 4)
        {
            SelectedRegion = null;
            Close();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            Close();
        }
    }

    private void UpdateSelection(Point start, Point end)
    {
        var rect = BuildCanvasRect(start, end);
        Canvas.SetLeft(SelectionRect, rect.Left);
        Canvas.SetTop(SelectionRect, rect.Top);
        SelectionRect.Width = rect.Width;
        SelectionRect.Height = rect.Height;
        HintText.Text = $"{Math.Round(rect.Width):0} × {Math.Round(rect.Height):0}";
        SelectionSizeText.Text = $"选区: {Math.Round(rect.Width):0} × {Math.Round(rect.Height):0}";
    }

    private Rect BuildCanvasRect(Point start, Point end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        return new Rect(x, y, width, height);
    }

    private Rect BuildAbsoluteRect(Point start, Point end)
    {
        var rect = BuildCanvasRect(start, end);
        return new Rect(rect.Left + Left, rect.Top + Top, rect.Width, rect.Height);
    }

    private void UpdateInspector(Point canvasPoint)
    {
        var screenX = (int)Math.Round(canvasPoint.X + Left);
        var screenY = (int)Math.Round(canvasPoint.Y + Top);
        PositionText.Text = $"X: {screenX}  Y: {screenY}";
        PositionInspector(canvasPoint, screenX, screenY);
    }

    private void PositionInspector(Point canvasPoint, int screenX, int screenY)
    {
        const double offset = 18;
        var left = canvasPoint.X + offset;
        var top = canvasPoint.Y + offset;

        if (left + InspectorBadge.Width > ActualWidth - 12)
        {
            left = canvasPoint.X - InspectorBadge.Width - offset;
        }

        var inspectorHeight = InspectorBadge.ActualHeight > 0 ? InspectorBadge.ActualHeight : 156;
        if (top + inspectorHeight > ActualHeight - 12)
        {
            top = canvasPoint.Y - inspectorHeight - offset;
        }

        Canvas.SetLeft(InspectorBadge, Math.Max(12, left));
        Canvas.SetTop(InspectorBadge, Math.Max(12, top));

        UpdateMagnifier(screenX, screenY);
    }

    private void UpdateMagnifier(int screenX, int screenY)
    {
        using var bitmap = new Drawing.Bitmap(MagnifierSampleSize, MagnifierSampleSize, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                screenX - MagnifierSampleSize / 2,
                screenY - MagnifierSampleSize / 2,
                0,
                0,
                new Drawing.Size(MagnifierSampleSize, MagnifierSampleSize),
                Drawing.CopyPixelOperation.SourceCopy);
        }

        var centerColor = bitmap.GetPixel(MagnifierSampleSize / 2, MagnifierSampleSize / 2);
        ColorText.Text = $"RGB: {centerColor.R}, {centerColor.G}, {centerColor.B}";
        HexText.Text = $"#{centerColor.R:X2}{centerColor.G:X2}{centerColor.B:X2}";
        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(centerColor.R, centerColor.G, centerColor.B));
        MagnifierImage.Source = ConvertBitmapToSource(bitmap);
    }

    private static BitmapSource ConvertBitmapToSource(Drawing.Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }
}
