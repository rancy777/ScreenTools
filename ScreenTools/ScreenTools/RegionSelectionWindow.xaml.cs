using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using DrawingImaging = System.Drawing.Imaging;
using Forms = System.Windows.Forms;
using Rectangle = System.Drawing.Rectangle;

namespace ScreenTools;

/// <summary>
/// Provides pure physical-pixel geometry operations for virtual-desktop capture.
/// </summary>
public static class DesktopCaptureGeometry
{
    /// <summary>Returns the smallest rectangle containing all supplied monitor bounds.</summary>
    public static Rectangle GetVirtualBounds(IEnumerable<Rectangle> monitorBounds)
    {
        ArgumentNullException.ThrowIfNull(monitorBounds);
        var bounds = monitorBounds.Where(rectangle => rectangle.Width > 0 && rectangle.Height > 0).ToArray();
        if (bounds.Length == 0)
            throw new ArgumentException("At least one non-empty monitor rectangle is required.", nameof(monitorBounds));

        return Rectangle.FromLTRB(
            bounds.Min(rectangle => rectangle.Left),
            bounds.Min(rectangle => rectangle.Top),
            bounds.Max(rectangle => rectangle.Right),
            bounds.Max(rectangle => rectangle.Bottom));
    }

    /// <summary>Normalizes a WPF rectangle and rounds outward to cover every selected physical pixel.</summary>
    public static Rectangle ToPixelRectangle(Rect region)
    {
        if (!double.IsFinite(region.X) || !double.IsFinite(region.Y) ||
            !double.IsFinite(region.Width) || !double.IsFinite(region.Height))
            throw new ArgumentOutOfRangeException(nameof(region), "Capture coordinates must be finite.");

        var left = Math.Floor(Math.Min(region.Left, region.Right));
        var top = Math.Floor(Math.Min(region.Top, region.Bottom));
        var right = Math.Ceiling(Math.Max(region.Left, region.Right));
        var bottom = Math.Ceiling(Math.Max(region.Top, region.Bottom));

        if (left < int.MinValue || top < int.MinValue || right > int.MaxValue || bottom > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(region), "Capture coordinates exceed the supported desktop range.");

        return Rectangle.FromLTRB((int)left, (int)top, (int)right, (int)bottom);
    }

    /// <summary>Clips a physical-pixel capture rectangle to the virtual desktop.</summary>
    public static Rectangle ClipToVirtualBounds(Rectangle region, Rectangle virtualBounds)
    {
        var clipped = Rectangle.Intersect(region, virtualBounds);
        if (clipped.Width <= 0 || clipped.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(region), "The selected region does not intersect the virtual desktop.");

        return clipped;
    }
}

/// <summary>
/// Displays per-monitor selection overlays and returns the selected rectangle in physical desktop pixels.
/// </summary>
public partial class RegionSelectionWindow : Window
{
    private const int MagnifierSampleSize = 18;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly List<RegionSelectionWindow> ActiveOverlays = [];
    private static TaskCompletionSource<Rect?>? _selectionCompletion;
    private static Point? _selectionStart;
    private static RegionSelectionWindow? _selectionOwner;

    private readonly Rectangle _monitorBounds;
    private bool _isFinishing;

    private RegionSelectionWindow(Rectangle monitorBounds, bool showHint)
    {
        _monitorBounds = monitorBounds;
        InitializeComponent();
        LocalizationManager.RegisterWindow(this);
        HintBadge.Visibility = showHint ? Visibility.Visible : Visibility.Collapsed;
        InspectorBadge.Visibility = Visibility.Collapsed;
        SourceInitialized += Window_SourceInitialized;
        Closed += Window_Closed;
    }

    /// <summary>
    /// Opens one correctly DPI-scaled overlay per display and asynchronously returns a physical-pixel rectangle.
    /// </summary>
    public static Task<Rect?> SelectRegionAsync()
    {
        if (_selectionCompletion is not null)
            throw new InvalidOperationException("A region selection is already active.");

        _selectionCompletion = new TaskCompletionSource<Rect?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _selectionStart = null;
        _selectionOwner = null;

        try
        {
            var screens = Forms.Screen.AllScreens.OrderByDescending(screen => screen.Primary).ToArray();
            for (var index = 0; index < screens.Length; index++)
            {
                var bounds = screens[index].Bounds;
                var overlay = new RegionSelectionWindow(bounds, index == 0);
                ActiveOverlays.Add(overlay);
                overlay.Show();
            }
        }
        catch
        {
            FinishSelection(null);
            throw;
        }

        return _selectionCompletion.Task;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        // SetWindowPos consumes physical pixels. WPF then lays out this monitor-local
        // HWND using the DPI Windows assigned to that monitor.
        if (!SetWindowPos(handle, new IntPtr(-1), _monitorBounds.Left, _monitorBounds.Top,
                _monitorBounds.Width, _monitorBounds.Height, SwpNoActivate | SwpShowWindow))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectionCompletion is null || _isFinishing)
            return;

        _selectionOwner = this;
        _selectionStart = GetCursorPosition();
        InspectorBadge.Visibility = Visibility.Visible;
        if (!CaptureMouse())
        {
            FinishSelection(null);
            return;
        }
        UpdateSelectionOverlays(_selectionStart.Value, _selectionStart.Value);
        UpdateInspector(_selectionStart.Value);
        e.Handled = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_selectionStart is null || _selectionCompletion is null)
            return;

        var current = GetCursorPosition();
        UpdateSelectionOverlays(_selectionStart.Value, current);
        var cursorOwner = ActiveOverlays.FirstOrDefault(overlay =>
            overlay._monitorBounds.Contains((int)current.X, (int)current.Y));
        if (cursorOwner is not null && !ReferenceEquals(cursorOwner, _selectionOwner))
        {
            if (_selectionOwner is not null)
                _selectionOwner.InspectorBadge.Visibility = Visibility.Collapsed;
            _selectionOwner = cursorOwner;
            _selectionOwner.InspectorBadge.Visibility = Visibility.Visible;
        }
        _selectionOwner?.UpdateInspector(current);
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_selectionStart is null || _selectionCompletion is null)
            return;

        var end = GetCursorPosition();
        var start = _selectionStart.Value;
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        if (width < 4 || height < 4)
            FinishSelection(null);
        else
            FinishSelection(new Rect(left, top, width, height));

        e.Handled = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            FinishSelection(null);
            e.Handled = true;
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        if (_selectionCompletion is not null && !_isFinishing)
            FinishSelection(null);
    }

    private static void UpdateSelectionOverlays(Point start, Point end)
    {
        foreach (var overlay in ActiveOverlays)
            overlay.RenderSelection(start, end);
    }

    private void RenderSelection(Point start, Point end)
    {
        var startDip = PointFromScreen(start);
        var endDip = PointFromScreen(end);
        var rectangle = new Rect(startDip, endDip);
        Canvas.SetLeft(SelectionRect, rectangle.Left);
        Canvas.SetTop(SelectionRect, rectangle.Top);
        SelectionRect.Width = rectangle.Width;
        SelectionRect.Height = rectangle.Height;
        SelectionRect.Visibility = Visibility.Visible;

        var physicalWidth = Math.Abs(end.X - start.X);
        var physicalHeight = Math.Abs(end.Y - start.Y);
        HintText.Text = $"{physicalWidth:0} × {physicalHeight:0}";
        SelectionSizeText.Text = LocalizationManager.Translate($"选区: {physicalWidth:0} × {physicalHeight:0}");
    }

    private void UpdateInspector(Point physicalPoint)
    {
        PositionText.Text = $"X: {(int)physicalPoint.X}  Y: {(int)physicalPoint.Y}";
        var local = PointFromScreen(physicalPoint);
        const double offset = 18;
        var left = local.X + offset;
        var top = local.Y + offset;
        if (left + InspectorBadge.Width > ActualWidth - 12)
            left = local.X - InspectorBadge.Width - offset;

        var inspectorHeight = InspectorBadge.ActualHeight > 0 ? InspectorBadge.ActualHeight : 156;
        if (top + inspectorHeight > ActualHeight - 12)
            top = local.Y - inspectorHeight - offset;

        Canvas.SetLeft(InspectorBadge, Math.Max(12, left));
        Canvas.SetTop(InspectorBadge, Math.Max(12, top));
        UpdateMagnifier((int)physicalPoint.X, (int)physicalPoint.Y);
    }

    private void UpdateMagnifier(int screenX, int screenY)
    {
        using var bitmap = new System.Drawing.Bitmap(MagnifierSampleSize, MagnifierSampleSize, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Black);
            var requested = new Rectangle(
                screenX - MagnifierSampleSize / 2,
                screenY - MagnifierSampleSize / 2,
                MagnifierSampleSize,
                MagnifierSampleSize);
            var virtualBounds = DesktopCaptureGeometry.GetVirtualBounds(
                Forms.Screen.AllScreens.Select(screen => screen.Bounds));
            var clipped = Rectangle.Intersect(requested, virtualBounds);
            if (clipped.Width > 0 && clipped.Height > 0)
            {
                graphics.CopyFromScreen(clipped.Left, clipped.Top,
                    clipped.Left - requested.Left, clipped.Top - requested.Top,
                    new System.Drawing.Size(clipped.Width, clipped.Height), System.Drawing.CopyPixelOperation.SourceCopy);
            }
        }

        var centerColor = bitmap.GetPixel(MagnifierSampleSize / 2, MagnifierSampleSize / 2);
        ColorText.Text = $"RGB: {centerColor.R}, {centerColor.G}, {centerColor.B}";
        HexText.Text = $"#{centerColor.R:X2}{centerColor.G:X2}{centerColor.B:X2}";
        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(centerColor.R, centerColor.G, centerColor.B));
        MagnifierImage.Source = ConvertBitmapToSource(bitmap);
    }

    private static BitmapSource ConvertBitmapToSource(System.Drawing.Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    private static Point GetCursorPosition()
    {
        if (!GetCursorPos(out var point))
            throw new InvalidOperationException("Windows could not determine the current desktop cursor position.");
        return new Point(point.X, point.Y);
    }

    private static void FinishSelection(Rect? selection)
    {
        var completion = _selectionCompletion;
        if (completion is null)
            return;

        _selectionCompletion = null;
        _selectionStart = null;
        _selectionOwner = null;
        var overlays = ActiveOverlays.ToArray();
        ActiveOverlays.Clear();

        foreach (var overlay in overlays)
        {
            overlay._isFinishing = true;
            if (overlay.IsMouseCaptured)
                overlay.ReleaseMouseCapture();
            overlay.Close();
        }

        completion.TrySetResult(selection);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y,
        int width, int height, uint flags);
}
