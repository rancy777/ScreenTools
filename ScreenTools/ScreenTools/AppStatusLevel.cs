namespace ScreenTools;

/// <summary>
/// Defines the severity levels used for application status messages.
/// </summary>
public enum AppStatusLevel
{
    /// <summary>Indicates an informational status that does not require user intervention.</summary>
    Info,
    /// <summary>Indicates that the requested operation completed successfully.</summary>
    Success,
    /// <summary>Indicates a recoverable issue or a partial result.</summary>
    Warning,
    /// <summary>Indicates that the requested operation failed.</summary>
    Error
}
