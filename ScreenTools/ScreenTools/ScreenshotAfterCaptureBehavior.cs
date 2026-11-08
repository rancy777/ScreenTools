namespace ScreenTools;

/// <summary>
/// Defines the action shown or performed after a screenshot is saved.
/// </summary>
public enum ScreenshotAfterCaptureBehavior
{
    /// <summary>Shows the quick-action toast after saving a screenshot.</summary>
    ShowQuickActions,
    /// <summary>Saves the screenshot without showing a follow-up window.</summary>
    SaveOnly,
    /// <summary>Saves the screenshot and pins it to the desktop as a sticker.</summary>
    SaveAndPin
}
