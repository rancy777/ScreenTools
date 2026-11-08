using System;
using System.Collections.Generic;

namespace ScreenTools;

/// <summary>
/// Defines the operations for retaining and querying recent capture outputs.
/// </summary>
public interface IOutputHistoryService
{
    /// <summary>Raised after a capture artifact is added to or removed from history.</summary>
    event EventHandler? HistoryChanged;

    /// <summary>Gets the retained output entries, newest first.</summary>
    IReadOnlyList<RecentOutputEntry> Entries { get; }

    /// <summary>Adds an artifact to recent-output history and persists the change.</summary>
    /// <param name="artifact">The screenshot, recording, or replay artifact to retain.</param>
    void Add(CaptureArtifact artifact);
}
