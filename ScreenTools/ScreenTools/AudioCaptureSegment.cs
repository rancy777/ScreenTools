using System;

namespace ScreenTools;

/// <summary>
/// Describes one captured audio segment and its position on the recording timeline.
/// </summary>
public sealed record AudioCaptureSegment(string Path, TimeSpan StartOffset);

/// <summary>
/// Describes an audio-source failure without requiring the video recording to stop.
/// </summary>
public sealed class AudioCaptureFailedEventArgs : EventArgs
{
    /// <summary>Creates an audio capture failure notification.</summary>
    public AudioCaptureFailedEventArgs(string sourceName, Exception exception)
    {
        SourceName = sourceName;
        Exception = exception;
    }

    /// <summary>Gets the human-readable audio source name.</summary>
    public string SourceName { get; }

    /// <summary>Gets the underlying capture exception.</summary>
    public Exception Exception { get; }
}