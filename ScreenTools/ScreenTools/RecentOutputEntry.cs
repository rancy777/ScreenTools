using System;

namespace ScreenTools;

public sealed record RecentOutputEntry(
    string Kind,
    string OutputPath,
    DateTimeOffset CreatedAt,
    bool IsVideo,
    int FrameCount);
