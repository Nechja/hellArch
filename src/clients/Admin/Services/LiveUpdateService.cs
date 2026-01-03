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
    private readonly Lock _eventLock = new();
    private Task? _healthPollingTask;
    private Task? _statsPollingTask;
    private bool _isStarted;
    private volatile bool _disposed;

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
            InvokeHealthUpdate();

            while (await _healthTimer.WaitForNextTickAsync(_cts.Token))
            {
                InvokeHealthUpdate();
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
            InvokeStatsUpdate();

            while (await _statsTimer.WaitForNextTickAsync(_cts.Token))
            {
                InvokeStatsUpdate();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private void InvokeHealthUpdate()
    {
        if (_disposed)
        {
            return;
        }

        EventHandler? handler;
        using (_eventLock.EnterScope())
        {
            handler = OnHealthUpdate;
        }
        handler?.Invoke(this, EventArgs.Empty);
    }

    private void InvokeStatsUpdate()
    {
        if (_disposed)
        {
            return;
        }

        EventHandler? handler;
        using (_eventLock.EnterScope())
        {
            handler = OnStatsUpdate;
        }
        handler?.Invoke(this, EventArgs.Empty);
    }

    private void HandleEventReceived(object? sender, CloudEventReceivedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        var cloudEvent = e.CloudEvent;
        var eventType = cloudEvent.Type.ToLowerInvariant();

        _logger.LogDebug("LiveUpdate received event: {Type}", cloudEvent.Type);

        // Capture all handlers under lock
        EventHandler<CloudEventUpdateEventArgs>? anyEventHandler;
        EventHandler<UpdateEventArgs>? userHandler;
        EventHandler<UpdateEventArgs>? sessionHandler;
        EventHandler<UpdateEventArgs>? webhookHandler;
        EventHandler? healthHandler;
        EventHandler? statsHandler;

        using (_eventLock.EnterScope())
        {
            anyEventHandler = OnAnyEvent;
            userHandler = OnUserUpdate;
            sessionHandler = OnSessionUpdate;
            webhookHandler = OnWebhookUpdate;
            healthHandler = OnHealthUpdate;
            statsHandler = OnStatsUpdate;
        }

        // Notify all event subscribers
        anyEventHandler?.Invoke(this, new CloudEventUpdateEventArgs(cloudEvent));

        // Show toast for significant events
        if (ShouldShowToast(eventType))
        {
            var details = GetEventDetails(cloudEvent);
            _toastService.ShowEvent(cloudEvent.Type, details);
        }

        // Trigger specific update events based on event type
        if (eventType.Contains("user"))
        {
            userHandler?.Invoke(this, new UpdateEventArgs(cloudEvent.Type));
            statsHandler?.Invoke(this, EventArgs.Empty);
        }
        else if (eventType.Contains("session"))
        {
            sessionHandler?.Invoke(this, new UpdateEventArgs(cloudEvent.Type));
            statsHandler?.Invoke(this, EventArgs.Empty);
        }
        else if (eventType.Contains("webhook"))
        {
            webhookHandler?.Invoke(this, new UpdateEventArgs(cloudEvent.Type));
        }
        else if (eventType.Contains("health") || eventType.Contains("service"))
        {
            healthHandler?.Invoke(this, EventArgs.Empty);
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

    public void TriggerHealthUpdate() => InvokeHealthUpdate();
    public void TriggerStatsUpdate() => InvokeStatsUpdate();

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
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
