using System;
using System.Threading;

namespace ScreenTools;

public sealed class AppStatusService
{
    private string _message = "就绪";
    private AppStatusLevel _level = AppStatusLevel.Info;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;
    private readonly SynchronizationContext? _synchronizationContext;

    public event EventHandler? StatusChanged;

    public string Message => _message;

    public AppStatusLevel Level => _level;

    public DateTimeOffset UpdatedAt => _updatedAt;

    public AppStatusService()
    {
        _synchronizationContext = SynchronizationContext.Current;
    }

    public void SetStatus(string message, AppStatusLevel level)
    {
        _message = message;
        _level = level;
        _updatedAt = DateTimeOffset.Now;
        RaiseStatusChanged();
    }

    private void RaiseStatusChanged()
    {
        if (_synchronizationContext is null)
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _synchronizationContext.Post(_ => StatusChanged?.Invoke(this, EventArgs.Empty), null);
    }
}
