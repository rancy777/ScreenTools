using System;

namespace ScreenTools;

public sealed class AppStatusService
{
    private string _message = "就绪";
    private AppStatusLevel _level = AppStatusLevel.Info;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;

    public event EventHandler? StatusChanged;

    public string Message => _message;

    public AppStatusLevel Level => _level;

    public DateTimeOffset UpdatedAt => _updatedAt;

    public void SetStatus(string message, AppStatusLevel level)
    {
        _message = message;
        _level = level;
        _updatedAt = DateTimeOffset.Now;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}
