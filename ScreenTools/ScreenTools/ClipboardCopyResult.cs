namespace ScreenTools;

public sealed record ClipboardCopyResult(
    bool Success,
    string Detail,
    ClipboardEntry Entry);
