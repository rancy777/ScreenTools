namespace ScreenTools;

/// <summary>
/// Identifies the application action invoked by a registered shortcut.
/// </summary>
public enum ShortcutAction
{
    /// <summary>Captures a screenshot using the current screenshot settings.</summary>
    Screenshot,
    /// <summary>Starts or stops a recording, depending on the current recording state.</summary>
    Recording,
    /// <summary>Exports the currently buffered instant-replay clip.</summary>
    Replay
}
