namespace Admin.Services;

/// <summary>
/// Background service that manages the RealtimeHubService connection lifecycle.
/// Ensures the SignalR connection is established on startup and properly disposed on shutdown.
/// </summary>
public sealed class RealtimeHubHostedService(
    RealtimeHubService hubService,
    ILogger<RealtimeHubHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping realtime hub connection...");
        await hubService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
