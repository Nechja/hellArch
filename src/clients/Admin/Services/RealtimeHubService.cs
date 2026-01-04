using HellArch.CloudEvents;
using Microsoft.AspNetCore.SignalR.Client;

namespace Admin.Services;

/// <summary>
/// Service for connecting to the realtime SignalR hub and receiving live events.
/// Registered as a singleton. Connection is established lazily when Connect is called
/// with a valid session token.
/// </summary>
public sealed class RealtimeHubService(
    IHubConnectionFactory hubConnectionFactory,
    ILogger<RealtimeHubService> logger) : IAsyncDisposable
{
    private readonly List<CloudEventDto> _recentEvents = [];
    private readonly Lock _eventsLock = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private const int MaxRecentEvents = 50;

    private HubConnection? _hubConnection;
    private string? _currentSessionToken;

    public event EventHandler<CloudEventReceivedEventArgs>? OnEventReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? OnConnectionStateChanged;

    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;
    public string ConnectionStateText => _hubConnection?.State.ToString() ?? "Disconnected";
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public IReadOnlyList<CloudEventDto> RecentEvents
    {
        get
        {
            using (_eventsLock.EnterScope())
            {
                return _recentEvents.ToList();
            }
        }
    }

    /// <summary>
    /// Connects to the realtime hub with the provided session token.
    /// If already connected with the same token, this is a no-op.
    /// If connected with a different token, reconnects with the new token.
    /// </summary>
    public async Task Connect(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionToken))
        {
            logger.LogWarning("Cannot connect to realtime hub without a session token");
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // Already connected with this token
            if (_hubConnection?.State == HubConnectionState.Connected && _currentSessionToken == sessionToken)
            {
                return;
            }

            // Different token - disconnect first
            if (_hubConnection is not null && _currentSessionToken != sessionToken)
            {
                await DisconnectInternal();
            }

            // Build new connection if needed
            if (_hubConnection is null)
            {
                _hubConnection = BuildConnection(sessionToken);
                _currentSessionToken = sessionToken;
            }

            // Connect
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync(cancellationToken);
                logger.LogInformation("Connected to realtime hub");
                OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Connected"));
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error connecting to realtime hub");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Failed"));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation while connecting to realtime hub");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Failed"));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Connection to realtime hub was cancelled");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private HubConnection BuildConnection(string sessionToken)
    {
        var connection = hubConnectionFactory.Create(sessionToken);
        ConfigureHandlers(connection);
        return connection;
    }

    private void ConfigureHandlers(HubConnection connection)
    {
        connection.On<CloudEventDto>("ReceiveEvent", HandleEvent);
        connection.On<string>("SessionInvalidated", HandleSessionInvalidated);
        connection.On("UserUpdated", HandleUserUpdated);

        connection.Reconnecting += error =>
        {
            logger.LogWarning(error, "SignalR connection lost, attempting to reconnect...");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Reconnecting"));
            return Task.CompletedTask;
        };

        connection.Reconnected += connectionId =>
        {
            logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Connected"));
            return Task.CompletedTask;
        };

        connection.Closed += error =>
        {
            logger.LogWarning(error, "SignalR connection closed");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
            return Task.CompletedTask;
        };
    }

    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            await DisconnectInternal();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task DisconnectInternal()
    {
        if (_hubConnection is null)
        {
            return;
        }

        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }

        await _hubConnection.DisposeAsync();
        _hubConnection = null;
        _currentSessionToken = null;
        logger.LogInformation("Disconnected from realtime hub");
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
    }

    public async Task SubscribeToEventType(string eventType, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                logger.LogWarning("Cannot subscribe - not connected to hub");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("SubscribeToEventType", eventType, cancellationToken);
                logger.LogDebug("Subscribed to event type: {EventType}", eventType);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to subscribe to {EventType} - connection state changed", eventType);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task UnsubscribeFromEventType(string eventType, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromEventType", eventType, cancellationToken);
                logger.LogDebug("Unsubscribed from event type: {EventType}", eventType);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogDebug(ex, "Failed to unsubscribe from {EventType} - connection state changed", eventType);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void HandleEvent(CloudEventDto cloudEvent)
    {
        logger.LogDebug("Received event: {Type} from {Source}", cloudEvent.Type, cloudEvent.Source);

        EventHandler<CloudEventReceivedEventArgs>? handler;
        using (_eventsLock.EnterScope())
        {
            _recentEvents.Insert(0, cloudEvent);
            if (_recentEvents.Count > MaxRecentEvents)
            {
                _recentEvents.RemoveAt(_recentEvents.Count - 1);
            }
            handler = OnEventReceived;
        }

        handler?.Invoke(this, new CloudEventReceivedEventArgs(cloudEvent));
    }

    private void HandleSessionInvalidated(string reason)
    {
        logger.LogInformation("Session invalidated: {Reason}", reason);
    }

    private void HandleUserUpdated()
    {
        logger.LogInformation("User data updated notification received");
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
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
