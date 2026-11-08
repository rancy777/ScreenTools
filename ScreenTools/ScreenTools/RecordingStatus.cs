namespace ScreenTools;

/// <summary>
/// Defines the lifecycle states of a recording operation.
/// </summary>
public enum RecordingStatus
{
    /// <summary>No recording session is currently active.</summary>
    Idle,
    /// <summary>Frames and enabled audio sources are being captured.</summary>
    Recording,
    /// <summary>Capture is temporarily suspended and can be resumed.</summary>
    Paused,
    /// <summary>Capture has stopped and the output is being persisted or encoded.</summary>
    Finalizing
}
