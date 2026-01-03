using Microsoft.AspNetCore.SignalR.Client;

namespace Admin.Services;

/// <summary>
/// Factory abstraction for creating SignalR hub connections.
/// Enables dependency injection and testability for hub services.
/// </summary>
public interface IHubConnectionFactory
{
    /// <summary>
    /// Creates a new hub connection with the specified session token for authentication.
    /// </summary>
    HubConnection Create(string sessionToken);
}
