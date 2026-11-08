namespace ScreenTools;

/// <summary>
/// Identifies an action requested from the screenshot quick-action toast.
/// </summary>
public enum ScreenshotQuickAction
{
    /// <summary>Creates an on-screen sticker from the saved screenshot.</summary>
    PinToScreen,
    /// <summary>Opens the saved screenshot with its associated application.</summary>
    Open,
    /// <summary>Opens File Explorer with the screenshot selected.</summary>
    RevealInFolder,
    /// <summary>Changes the default post-capture behavior to saving only.</summary>
    SetDefaultSaveOnly,
    /// <summary>Changes the default post-capture behavior to saving and pinning.</summary>
    SetDefaultSaveAndPin
}
