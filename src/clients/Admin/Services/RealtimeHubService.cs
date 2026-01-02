using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Admin.Services;

/// <summary>
/// Service for connecting to the realtime SignalR hub and receiving live events.
/// Registered as a singleton to maintain a single connection across all components.
/// </summary>
public sealed class RealtimeHubService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<RealtimeHubService> _logger;
    private readonly List<CloudEventDto> _recentEvents = [];
    private readonly object _eventsLock = new();
    private const int MaxRecentEvents = 50;

    public event EventHandler<CloudEventReceivedEventArgs>? OnEventReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? OnConnectionStateChanged;

    public HubConnectionState ConnectionState => _hubConnection.State;
    public string ConnectionStateText => _hubConnection.State.ToString();

    public IReadOnlyList<CloudEventDto> RecentEvents
    {
        get
        {
            lock (_eventsLock)
            {
                return _recentEvents.ToList();
            }
        }
    }

    public RealtimeHubService(IConfiguration configuration, ILogger<RealtimeHubService> logger)
    {
        _logger = logger;

        var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL")
            ?? configuration.GetValue<string>("Gateway:Url")
            ?? "http://localhost:5000";

        var hubUrl = $"{gatewayUrl}/hubs/events";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        ConfigureHandlers();
    }

    private void ConfigureHandlers()
    {
        _hubConnection.On<CloudEventDto>("ReceiveEvent", HandleEvent);
        _hubConnection.On<string>("SessionInvalidated", HandleSessionInvalidated);
        _hubConnection.On("UserUpdated", HandleUserUpdated);

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR connection lost, attempting to reconnect...");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Reconnecting"));
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Connected"));
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogWarning(error, "SignalR connection closed");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("Connected to realtime hub");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Connected"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to realtime hub");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Failed"));
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            return;
        }

        await _hubConnection.StopAsync(cancellationToken);
        _logger.LogInformation("Disconnected from realtime hub");
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
    }

    public async Task SubscribeToEventType(string eventType)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot subscribe - not connected to hub");
            return;
        }

        await _hubConnection.InvokeAsync("SubscribeToEventType", eventType);
        _logger.LogDebug("Subscribed to event type: {EventType}", eventType);
    }

    public async Task UnsubscribeFromEventType(string eventType)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            return;
        }

        await _hubConnection.InvokeAsync("UnsubscribeFromEventType", eventType);
        _logger.LogDebug("Unsubscribed from event type: {EventType}", eventType);
    }

    private void HandleEvent(CloudEventDto cloudEvent)
    {
        _logger.LogDebug("Received event: {Type} from {Source}", cloudEvent.Type, cloudEvent.Source);

        lock (_eventsLock)
        {
            _recentEvents.Insert(0, cloudEvent);
            if (_recentEvents.Count > MaxRecentEvents)
            {
                _recentEvents.RemoveAt(_recentEvents.Count - 1);
            }
        }

        OnEventReceived?.Invoke(this, new CloudEventReceivedEventArgs(cloudEvent));
    }

    private void HandleSessionInvalidated(string reason)
    {
        _logger.LogInformation("Session invalidated: {Reason}", reason);
    }

    private void HandleUserUpdated()
    {
        _logger.LogInformation("User data updated notification received");
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}

public sealed class CloudEventReceivedEventArgs(CloudEventDto cloudEvent) : EventArgs
{
    public CloudEventDto CloudEvent { get; } = cloudEvent;
}

public sealed class ConnectionStateChangedEventArgs(string state) : EventArgs
{
    public string State { get; } = state;
}

/// <summary>
/// CloudEvent DTO matching the realtime worker's format.
/// </summary>
public sealed record CloudEventDto
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset Time { get; init; }
    public JsonElement? Data { get; init; }

    public string? GetUserId()
    {
        if (Data is not { } data)
        {
            return null;
        }

        if (data.TryGetProperty("user_id", out var userId))
        {
            return userId.GetString();
        }

        return null;
    }
}
