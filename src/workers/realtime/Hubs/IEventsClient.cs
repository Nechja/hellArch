using HellArch.CloudEvents;

namespace Realtime.Hubs;

/// <summary>
/// Typed client interface for SignalR hub.
/// Defines methods the server can invoke on connected clients.
/// </summary>
public interface IEventsClient
{
    /// <summary>
    /// Receives a CloudEvent. This is the primary method for pushing events to clients.
    /// </summary>
    Task ReceiveEvent(CloudEventDto cloudEvent);

    /// <summary>
    /// Receives a notification that a session was invalidated.
    /// Clients should handle this by redirecting to login.
    /// </summary>
    Task SessionInvalidated(string reason);

    /// <summary>
    /// Receives a notification that user data was updated.
    /// Clients should refresh their cached user data.
    /// </summary>
    Task UserUpdated();
}
