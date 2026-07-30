using System;

namespace ScreenTools;

public interface IClipboardManagerService
{
    event EventHandler? HistoryChanged;
    IReadOnlyList<ClipboardEntry> Entries { get; }
    ClipboardCopyResult CopyImageFromFile(string imagePath, string kind);
}
