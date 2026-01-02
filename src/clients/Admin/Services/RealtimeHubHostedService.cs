namespace Admin.Services;

/// <summary>
/// Background service that manages the RealtimeHubService connection lifecycle.
/// Ensures the SignalR connection is established on startup and properly disposed on shutdown.
/// Also triggers toast notifications for important events.
/// </summary>
public sealed class RealtimeHubHostedService(
    RealtimeHubService hubService,
    ToastService toastService,
    ILogger<RealtimeHubHostedService> logger) : BackgroundService
{
    // Event types that should trigger toasts
    private static readonly HashSet<string> ToastEventPatterns =
    [
        "user.created",
        "user.updated",
        "user.deleted",
        "session.created",
        "session.ended",
        "permission.granted",
        "permission.revoked",
        "webhook"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to events for toast notifications
        hubService.OnEventReceived += HandleEventForToast;

        // Wait a moment for the app to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await hubService.StartAsync(stoppingToken);
                logger.LogInformation("Realtime hub connection established");

                // Keep running until cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error connecting to realtime hub, retrying in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Invalid operation connecting to realtime hub, retrying in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void HandleEventForToast(object? sender, CloudEventReceivedEventArgs e)
    {
        var eventType = e.CloudEvent.Type.ToLowerInvariant();

        // Check if this event type should trigger a toast
        if (!ShouldShowToast(eventType))
        {
            return;
        }

        // Extract details for the toast message
        var details = GetEventDetails(e.CloudEvent);
        toastService.ShowEvent(eventType, details);
    }

    private static bool ShouldShowToast(string eventType)
    {
        return ToastEventPatterns.Any(pattern =>
            eventType.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetEventDetails(CloudEventDto cloudEvent)
    {
        if (cloudEvent.Data is not { } data)
        {
            return null;
        }

        // Try to extract meaningful details from the event data
        if (data.TryGetProperty("username", out var username))
        {
            return username.GetString();
        }

        if (data.TryGetProperty("user_id", out var userId))
        {
            return $"User {userId.GetString()}";
        }

        if (data.TryGetProperty("name", out var name))
        {
            return name.GetString();
        }

        return null;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        hubService.OnEventReceived -= HandleEventForToast;
        logger.LogInformation("Stopping realtime hub connection...");
        await hubService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
