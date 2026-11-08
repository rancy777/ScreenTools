using System;
using System.Windows.Threading;

namespace ScreenTools;

/// <summary>
/// Publishes the current application status and notifies subscribers when it changes.
/// </summary>
public sealed class AppStatusService
{
    private string _message = "就绪";
    private AppStatusLevel _level = AppStatusLevel.Info;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;
    private readonly Dispatcher _dispatcher;

    /// <summary>
    /// Raised when the current application status message or severity changes.
    /// </summary>
    public event EventHandler? StatusChanged;

    /// <summary>
    /// Gets the current application status message.
    /// </summary>
    public string Message => _message;

    /// <summary>
    /// Gets the severity assigned to the current application status.
    /// </summary>
    public AppStatusLevel Level => _level;

    /// <summary>
    /// Gets the timestamp when the current application status was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt => _updatedAt;

    /// <summary>
    /// Initializes the status service with an informational default state.
    /// </summary>
    public AppStatusService()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// Updates the current status message and severity, then raises the status-changed event.
    /// </summary>
    public void SetStatus(string message, AppStatusLevel level)
    {
        if (!_dispatcher.CheckAccess())
        {
            if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished)
                _dispatcher.BeginInvoke(new Action(() => SetStatus(message, level)));
            return;
        }

        _message = message;
        _level = level;
        _updatedAt = DateTimeOffset.Now;
        RaiseStatusChanged();
    }

    private void RaiseStatusChanged()
    {
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}
