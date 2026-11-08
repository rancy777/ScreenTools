using System;
using System.Collections.Generic;

namespace ScreenTools;

/// <summary>
/// Defines the lifecycle required by an audio capture implementation.
/// </summary>
public interface IAudioCaptureService
{
    /// <summary>Gets whether an audio source is currently available for capture.</summary>
    bool IsSupported { get; }

    /// <summary>Gets whether the source is actively delivering audio samples.</summary>
    bool IsCapturing { get; }

    /// <summary>Gets a user-facing explanation of the current capture availability.</summary>
    string AvailabilityMessage { get; }

    /// <summary>Gets the audio files produced so far, with their recording-timeline offsets.</summary>
    IReadOnlyList<AudioCaptureSegment> OutputSegments { get; }

    /// <summary>Raised when an audio source fails after recording has started.</summary>
    event EventHandler<AudioCaptureFailedEventArgs>? CaptureFailed;

    /// <summary>Refreshes the source's availability before a new recording.</summary>
    void RefreshAvailability();

    /// <summary>Starts writing captured audio to the specified WAV output path.</summary>
    /// <param name="outputPath">Destination path for the audio file.</param>
    /// <param name="timelineOffset">Position of the first sample on the recording timeline.</param>
    void Start(string outputPath, TimeSpan timelineOffset = default);

    /// <summary>Temporarily suspends audio capture without finalizing the output.</summary>
    void Pause();

    /// <summary>Resumes audio capture after a pause.</summary>
    /// <param name="timelineOffset">Position of the resumed segment on the recording timeline.</param>
    void Resume(TimeSpan timelineOffset = default);

    /// <summary>Stops capture and finalizes the output file.</summary>
    void Stop();
}
