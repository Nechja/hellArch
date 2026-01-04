namespace Admin.Services;

/// <summary>
/// Abstraction for toast notification operations.
/// </summary>
public interface IToastService
{
    IReadOnlyList<Toast> Toasts { get; }
    event EventHandler? OnChange;

    void Show(string message, ToastType type = ToastType.Info, int durationMs = 5000);
    void ShowSuccess(string message, int durationMs = 4000);
    void ShowError(string message, int durationMs = 6000);
    void ShowWarning(string message, int durationMs = 5000);
    void ShowEvent(string eventType, string? details = null);
    void Dismiss(int id);
    void Clear();
}
