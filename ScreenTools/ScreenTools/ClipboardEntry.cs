using System;

namespace ScreenTools;

/// <summary>
/// Represents an image or text item retained in clipboard history.
/// </summary>
public sealed record ClipboardEntry(
    string Kind,
    string OutputPath,
    DateTimeOffset CreatedAt,
    bool CopySucceeded,
    string Detail);
