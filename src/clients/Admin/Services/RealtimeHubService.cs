using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Admin.Services;

/// <summary>
/// Service for connecting to the realtime SignalR hub and receiving live events.
/// Registered as a singleton. Connection is established lazily when Connect is called
/// with a valid session token.
/// </summary>
public sealed class RealtimeHubService : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RealtimeHubService> _logger;
    private readonly List<CloudEventDto> _recentEvents = [];
    private readonly object _eventsLock = new();
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
            lock (_eventsLock)
            {
                return _recentEvents.ToList();
            }
        }
    }

    public RealtimeHubService(IConfiguration configuration, ILogger<RealtimeHubService> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
            _logger.LogWarning("Cannot connect to realtime hub without a session token");
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
                _logger.LogInformation("Connected to realtime hub");
                OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Connected"));
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error connecting to realtime hub");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Failed"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while connecting to realtime hub");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Failed"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Connection to realtime hub was cancelled");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private HubConnection BuildConnection(string sessionToken)
    {
        var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL")
            ?? _configuration.GetValue<string>("Gateway:Url")
            ?? "http://localhost:5000";

        var hubUrl = $"{gatewayUrl}/hubs/events";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(null);
                // Pass session token for authentication
                options.Headers["X-Session-Token"] = sessionToken;
                options.Url = new Uri($"{hubUrl}?session_token={Uri.EscapeDataString(sessionToken)}");
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

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
            _logger.LogWarning(error, "SignalR connection lost, attempting to reconnect...");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Reconnecting"));
            return Task.CompletedTask;
        };

        connection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Connected"));
            return Task.CompletedTask;
        };

        connection.Closed += error =>
        {
            _logger.LogWarning(error, "SignalR connection closed");
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
        _logger.LogInformation("Disconnected from realtime hub");
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs("Disconnected"));
    }

    public async Task SubscribeToEventType(string eventType)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot subscribe - not connected to hub");
            return;
        }

        await _hubConnection.InvokeAsync("SubscribeToEventType", eventType);
        _logger.LogDebug("Subscribed to event type: {EventType}", eventType);
    }

    public async Task UnsubscribeFromEventType(string eventType)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
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
