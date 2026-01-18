using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Realtime.Security;

namespace Realtime.Hubs;

/// <summary>
/// SignalR hub for real-time event broadcasting.
/// Clients are automatically added to user-specific groups on connection.
/// </summary>
[Authorize]
public sealed class EventsHub(ILogger<EventsHub> logger) : Hub<IEventsClient>
{
    public const string UserGroupPrefix = "user:";
    public const string AdminGroup = "admins";
    public const string AllUsersGroup = "all";

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        // All authenticated users join the "all" group for broadcast events
        await Groups.AddToGroupAsync(connectionId, AllUsersGroup);

        if (userId is not null)
        {
            // Join user-specific group for targeted events
            await Groups.AddToGroupAsync(connectionId, $"{UserGroupPrefix}{userId}");
            logger.LogInformation(
                "Client connected: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
        }
        else
        {
            logger.LogInformation(
                "Anonymous client connected: {ConnectionId}",
                connectionId);
        }

        // Check if user is admin and add to admin group
        if (IsAdmin())
        {
            await Groups.AddToGroupAsync(connectionId, AdminGroup);
            logger.LogDebug("Added {ConnectionId} to admin group", connectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        if (exception is not null)
        {
            logger.LogWarning(
                exception,
                "Client disconnected with error: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
        }
        else
        {
            logger.LogInformation(
                "Client disconnected: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to specific event types.
    /// Events matching these types will be sent to the client.
    /// </summary>
    public async Task SubscribeToEventType(string eventType)
    {
        if (!EventTypeValidator.IsValid(eventType))
        {
            logger.LogWarning(
                "Client {ConnectionId} attempted to subscribe to invalid event type: {EventType}",
                Context.ConnectionId,
                eventType);
            throw new HubException("Invalid event type format");
        }

        var groupName = $"event:{eventType}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        logger.LogDebug(
            "Client {ConnectionId} subscribed to event type: {EventType}",
            Context.ConnectionId,
            eventType);
    }

    /// <summary>
    /// Allows clients to unsubscribe from specific event types.
    /// </summary>
    public async Task UnsubscribeFromEventType(string eventType)
    {
        if (!EventTypeValidator.IsValid(eventType))
        {
            logger.LogWarning(
                "Client {ConnectionId} attempted to unsubscribe from invalid event type: {EventType}",
                Context.ConnectionId,
                eventType);
            throw new HubException("Invalid event type format");
        }

        var groupName = $"event:{eventType}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        logger.LogDebug(
            "Client {ConnectionId} unsubscribed from event type: {EventType}",
            Context.ConnectionId,
            eventType);
    }

    private string? GetUserId() =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value;

    private bool IsAdmin() =>
        Context.User?.IsInRole("admin") == true
        || Context.User?.HasClaim("role", "admin") == true;
}
