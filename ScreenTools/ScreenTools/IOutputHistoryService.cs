using System;

namespace ScreenTools;

public interface IOutputHistoryService
{
    event EventHandler? HistoryChanged;
    IReadOnlyList<RecentOutputEntry> Entries { get; }
    void Add(CaptureArtifact artifact);
}
