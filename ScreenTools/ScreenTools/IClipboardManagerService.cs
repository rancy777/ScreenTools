using System;
using System.Collections.Generic;

namespace ScreenTools;

/// <summary>
/// Defines operations for clipboard history and copying captured images.
/// </summary>
public interface IClipboardManagerService
{
    /// <summary>Raised after the clipboard history collection changes.</summary>
    event EventHandler? HistoryChanged;

    /// <summary>Gets the current clipboard history entries, newest first.</summary>
    IReadOnlyList<ClipboardEntry> Entries { get; }

    /// <summary>Copies an image file to the clipboard and returns the operation result.</summary>
    /// <param name="imagePath">Path to the image file to copy.</param>
    /// <param name="kind">Artifact type used in status details.</param>
    /// <returns>The success state and any diagnostic detail from the copy operation.</returns>
    ClipboardCopyResult CopyImageFromFile(string imagePath, string kind);
}
