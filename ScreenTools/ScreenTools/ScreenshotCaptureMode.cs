namespace ScreenTools;

/// <summary>
/// Defines whether screenshots capture the full display or a selected region.
/// </summary>
public enum ScreenshotCaptureMode
{
    /// <summary>Captures the full virtual desktop.</summary>
    FullScreen,
    /// <summary>Prompts the user to select a rectangular capture region.</summary>
    Region
}
