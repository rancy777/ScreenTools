using System;

namespace ScreenTools;

public sealed record ClipboardEntry(
    string Kind,
    string OutputPath,
    DateTimeOffset CreatedAt,
    bool CopySucceeded,
    string Detail);
