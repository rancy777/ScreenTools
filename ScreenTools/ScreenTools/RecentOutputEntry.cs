using System;

namespace ScreenTools;

/// <summary>
/// Represents one item in the recent-output history.
/// </summary>
public sealed record RecentOutputEntry(
    string Kind,
    string OutputPath,
    DateTimeOffset CreatedAt,
    bool IsVideo,
    int FrameCount);
