using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Realtime.Hubs;

namespace Realtime.Tests;

/// <summary>
/// Tests that verify the EventsHub has proper security attributes.
/// These tests catch accidental removal of authorization requirements.
/// </summary>
public class EventsHubSecurityTests
{
    [Fact]
    public void HubRequiresAuthorization()
    {
        // This test prevents accidentally removing the [Authorize] attribute
        var hubType = typeof(EventsHub);
        var authorizeAttribute = hubType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void HubIsSealed()
    {
        // Security best practice: hub should be sealed to prevent override attacks
        Assert.True(typeof(EventsHub).IsSealed,
            "EventsHub should be sealed to prevent malicious subclassing");
    }

    [Fact]
    public void PublicMethodsAreOnlyTheExpectedOnes()
    {
        // This test ensures no new public methods are added without security review
        var hubType = typeof(EventsHub);
        var publicMethods = hubType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet();

        var expectedMethods = new HashSet<string>
        {
            "OnConnectedAsync",
            "OnDisconnectedAsync",
            "SubscribeToEventType",
            "UnsubscribeFromEventType"
        };

        // Check for unexpected methods
        var unexpectedMethods = publicMethods.Except(expectedMethods).ToList();
        Assert.Empty(unexpectedMethods);
    }
}
