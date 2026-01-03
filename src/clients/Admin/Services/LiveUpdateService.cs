using System.Text.Json;
using HellArch.CloudEvents;

namespace Admin.Services;

/// <summary>
/// Event args for update events that include an event type string.
/// </summary>
public sealed class UpdateEventArgs(string eventType) : EventArgs
{
    public string EventType { get; } = eventType;
}

/// <summary>
/// Event args for cloud event updates.
/// </summary>
public sealed class CloudEventUpdateEventArgs(CloudEventDto cloudEvent) : EventArgs
{
    public CloudEventDto CloudEvent { get; } = cloudEvent;
}

/// <summary>
/// Manages live updates for Admin dashboard pages.
/// Combines periodic polling with event-driven updates from SignalR.
/// </summary>
public sealed class LiveUpdateService : IAsyncDisposable
{
    private readonly RealtimeHubService _realtimeHub;
    private readonly ToastService _toastService;
    private readonly ILogger<LiveUpdateService> _logger;
    private readonly PeriodicTimer _healthTimer;
    private readonly PeriodicTimer _statsTimer;
    private readonly CancellationTokenSource _cts = new();
    private Task? _healthPollingTask;
    private Task? _statsPollingTask;
    private bool _isStarted;

    public event EventHandler? OnHealthUpdate;
    public event EventHandler? OnStatsUpdate;
    public event EventHandler<UpdateEventArgs>? OnUserUpdate;
    public event EventHandler<UpdateEventArgs>? OnSessionUpdate;
    public event EventHandler<UpdateEventArgs>? OnWebhookUpdate;
    public event EventHandler<CloudEventUpdateEventArgs>? OnAnyEvent;

    public LiveUpdateService(
        RealtimeHubService realtimeHub,
        ToastService toastService,
        ILogger<LiveUpdateService> logger)
    {
        _realtimeHub = realtimeHub;
        _toastService = toastService;
        _logger = logger;

        // Health updates every 15 seconds
        _healthTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        // Stats updates every 30 seconds
        _statsTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        // Subscribe to realtime events
        _realtimeHub.OnEventReceived += HandleEventReceived;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;

        _healthPollingTask = PollHealthUpdates();
        _statsPollingTask = PollStatsUpdates();

        _logger.LogInformation("LiveUpdateService started");
    }

    private async Task PollHealthUpdates()
    {
        try
        {
            // Trigger immediately on start
            OnHealthUpdate?.Invoke(this, EventArgs.Empty);

            while (await _healthTimer.WaitForNextTickAsync(_cts.Token))
            {
                OnHealthUpdate?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async Task PollStatsUpdates()
    {
        try
        {
            // Trigger immediately on start
            OnStatsUpdate?.Invoke(this, EventArgs.Empty);

            while (await _statsTimer.WaitForNextTickAsync(_cts.Token))
            {
                OnStatsUpdate?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private void HandleEventReceived(object? sender, CloudEventReceivedEventArgs e)
    {
        var cloudEvent = e.CloudEvent;
        var eventType = cloudEvent.Type.ToLowerInvariant();

        _logger.LogDebug("LiveUpdate received event: {Type}", cloudEvent.Type);

        // Notify all event subscribers
        OnAnyEvent?.Invoke(this, new CloudEventUpdateEventArgs(cloudEvent));

        // Show toast for significant events
        if (ShouldShowToast(eventType))
        {
            var details = GetEventDetails(cloudEvent);
            _toastService.ShowEvent(cloudEvent.Type, details);
        }

        // Trigger specific update events based on event type
        if (eventType.Contains("user"))
        {
            OnUserUpdate?.Invoke(this, new UpdateEventArgs(cloudEvent.Type));
            OnStatsUpdate?.Invoke(this, EventArgs.Empty);
        }
        else if (eventType.Contains("session"))
        {
            OnSessionUpdate?.Invoke(this, new UpdateEventArgs(cloudEvent.Type));
            OnStatsUpdate?.Invoke(this, EventArgs.Empty);
        }
        else if (eventType.Contains("webhook"))
        {
            OnWebhookUpdate?.Invoke(this, new UpdateEventArgs(cloudEvent.Type));
        }
        else if (eventType.Contains("health") || eventType.Contains("service"))
        {
            OnHealthUpdate?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool ShouldShowToast(string eventType)
    {
        // Show toasts for important events
        return eventType.Contains("created") ||
               eventType.Contains("deleted") ||
               eventType.Contains("error") ||
               eventType.Contains("failed") ||
               eventType.Contains("unhealthy") ||
               eventType.Contains("recovered");
    }

    private static string? GetEventDetails(CloudEventDto cloudEvent)
    {
        if (!cloudEvent.Data.HasValue)
        {
            return null;
        }

        try
        {
            var data = cloudEvent.Data.Value;

            // Try to extract meaningful info from common fields
            if (data.TryGetProperty("email", out var email))
            {
                return email.GetString();
            }

            if (data.TryGetProperty("username", out var username))
            {
                return username.GetString();
            }

            if (data.TryGetProperty("name", out var name))
            {
                return name.GetString();
            }

            if (data.TryGetProperty("user_id", out var userId))
            {
                return $"User {userId.GetString()?[..8]}...";
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public void TriggerHealthUpdate() => OnHealthUpdate?.Invoke(this, EventArgs.Empty);
    public void TriggerStatsUpdate() => OnStatsUpdate?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        _realtimeHub.OnEventReceived -= HandleEventReceived;

        await _cts.CancelAsync();

        if (_healthPollingTask is not null)
        {
            await _healthPollingTask;
        }

        if (_statsPollingTask is not null)
        {
            await _statsPollingTask;
        }

        _healthTimer.Dispose();
        _statsTimer.Dispose();
        _cts.Dispose();

        _logger.LogInformation("LiveUpdateService disposed");
    }
}
