namespace ScreenTools;

/// <summary>
/// Reports whether an image was copied to the Windows clipboard and any related detail.
/// </summary>
public sealed record ClipboardCopyResult(
    bool Success,
    string Detail,
    ClipboardEntry Entry);
