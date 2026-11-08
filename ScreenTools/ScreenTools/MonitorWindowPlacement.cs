using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace ScreenTools;

/// <summary>Defines how a floating application window is aligned within a monitor work area.</summary>
public enum MonitorWindowAlignment
{
    /// <summary>Centers the window in the destination monitor's work area.</summary>
    Center,
    /// <summary>Aligns the window to the top-right corner of the destination monitor.</summary>
    TopRight,
    /// <summary>Aligns the window to the bottom-right corner of the destination monitor.</summary>
    BottomRight
}

/// <summary>Places WPF windows using physical monitor work areas and the destination monitor's DPI.</summary>
public static class MonitorWindowPlacement
{
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoSize = 0x0001;

    /// <summary>Calculates a clamped physical-pixel window rectangle inside a monitor work area.</summary>
    public static DrawingRectangle CalculateBounds(DrawingRectangle workArea, DrawingSize windowSize,
        MonitorWindowAlignment alignment, int margin = 24, int offset = 0)
    {
        if (workArea.Width <= 0 || workArea.Height <= 0 || windowSize.Width <= 0 || windowSize.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(workArea), "The work area and window size must be positive.");
        if (margin < 0 || offset < 0)
            throw new ArgumentOutOfRangeException(nameof(margin), "Margins and offsets cannot be negative.");

        var width = Math.Min(windowSize.Width, workArea.Width);
        var height = Math.Min(windowSize.Height, workArea.Height);
        var x = alignment switch
        {
            MonitorWindowAlignment.Center => workArea.Left + (workArea.Width - width) / 2,
            MonitorWindowAlignment.TopRight or MonitorWindowAlignment.BottomRight => workArea.Right - width - margin - offset,
            _ => workArea.Left + margin
        };
        var y = alignment switch
        {
            MonitorWindowAlignment.Center => workArea.Top + (workArea.Height - height) / 2,
            MonitorWindowAlignment.BottomRight => workArea.Bottom - height - margin - offset,
            _ => workArea.Top + margin + offset
        };

        x = Math.Clamp(x, workArea.Left, Math.Max(workArea.Left, workArea.Right - width));
        y = Math.Clamp(y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height));
        return new DrawingRectangle(x, y, width, height);
    }

    /// <summary>Moves a shown WPF window to the monitor under the cursor and aligns it within that monitor.</summary>
    public static void PlaceOnCursorMonitor(Window window, MonitorWindowAlignment alignment, int margin = 24, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(window);
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("The window must be shown before monitor placement.");

        var cursor = Forms.Cursor.Position;
        var workArea = Forms.Screen.FromPoint(cursor).WorkingArea;

        // Move first so Windows sends WM_DPICHANGED for the destination monitor.
        if (!SetWindowPos(handle, IntPtr.Zero, workArea.Left + margin, workArea.Top + margin,
                0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        var dpi = GetDpiForWindow(handle);
        if (dpi == 0)
            dpi = 96;

        var scale = dpi / 96d;
        var desiredWidth = Math.Max(1, (int)Math.Ceiling((window.ActualWidth > 0 ? window.ActualWidth : window.Width) * scale));
        var desiredHeight = Math.Max(1, (int)Math.Ceiling((window.ActualHeight > 0 ? window.ActualHeight : window.Height) * scale));
        var bounds = CalculateBounds(workArea, new DrawingSize(desiredWidth, desiredHeight), alignment, margin, offset);

        if (!SetWindowPos(handle, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                SwpNoZOrder | SwpNoActivate))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y,
        int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);
}
