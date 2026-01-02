namespace Admin.Services;

public sealed class ToastService
{
    private readonly List<Toast> _toasts = [];
    private readonly object _lock = new();
    private int _nextId;

    public event EventHandler? OnChange;

    public IReadOnlyList<Toast> Toasts
    {
        get
        {
            lock (_lock)
            {
                return _toasts.ToList();
            }
        }
    }

    public void Show(string message, ToastType type = ToastType.Info, int durationMs = 5000)
    {
        var toast = new Toast(Interlocked.Increment(ref _nextId), message, type, durationMs);

        lock (_lock)
        {
            _toasts.Add(toast);
        }

        OnChange?.Invoke(this, EventArgs.Empty);

        // Auto-dismiss after duration
        if (durationMs > 0)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(durationMs);
                Dismiss(toast.Id);
            });
        }
    }

    public void ShowSuccess(string message, int durationMs = 4000) =>
        Show(message, ToastType.Success, durationMs);

    public void ShowError(string message, int durationMs = 6000) =>
        Show(message, ToastType.Error, durationMs);

    public void ShowWarning(string message, int durationMs = 5000) =>
        Show(message, ToastType.Warning, durationMs);

    public void ShowEvent(string eventType, string? details = null)
    {
        var (message, type) = eventType.ToLowerInvariant() switch
        {
            var t when t.Contains("created") => ($"Created: {details ?? eventType}", ToastType.Success),
            var t when t.Contains("deleted") => ($"Deleted: {details ?? eventType}", ToastType.Warning),
            var t when t.Contains("updated") => ($"Updated: {details ?? eventType}", ToastType.Info),
            var t when t.Contains("login") => ($"Login: {details ?? "User logged in"}", ToastType.Info),
            var t when t.Contains("logout") => ($"Logout: {details ?? "User logged out"}", ToastType.Info),
            var t when t.Contains("session") => ($"Session: {details ?? eventType}", ToastType.Info),
            var t when t.Contains("error") || t.Contains("failed") => ($"Error: {details ?? eventType}", ToastType.Error),
            _ => ($"Event: {GetShortEventType(eventType)}", ToastType.Info)
        };

        Show(message, type, 4000);
    }

    public void Dismiss(int id)
    {
        lock (_lock)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == id);
            if (toast is not null)
            {
                _toasts.Remove(toast);
            }
        }

        OnChange?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _toasts.Clear();
        }

        OnChange?.Invoke(this, EventArgs.Empty);
    }

    private static string GetShortEventType(string eventType)
    {
        var parts = eventType.Split('.');
        return parts.Length > 1 ? string.Join(".", parts[^2..]) : eventType;
    }
}

public sealed record Toast(int Id, string Message, ToastType Type, int DurationMs);

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
