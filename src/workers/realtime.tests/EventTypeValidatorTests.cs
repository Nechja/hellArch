using Realtime.Security;

namespace Realtime.Tests;

/// <summary>
/// Tests that verify event type validation prevents injection attacks on SignalR group names.
/// These tests prevent regressions that could allow malicious group name manipulation.
/// </summary>
public class EventTypeValidatorTests
{
    [Theory]
    [InlineData("user.created")]
    [InlineData("auth.session.started")]
    [InlineData("file_uploaded")]
    [InlineData("webhook-delivery")]
    [InlineData("A")]
    [InlineData("event123")]
    [InlineData("UserCreated")]
    public void AcceptsValidEventTypes(string eventType)
    {
        Assert.True(EventTypeValidator.IsValid(eventType),
            $"Event type '{eventType}' should be valid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void RejectsEmptyOrWhitespace(string? eventType)
    {
        Assert.False(EventTypeValidator.IsValid(eventType),
            $"Empty/whitespace event type should be rejected");
    }

    [Theory]
    [InlineData("123event")]      // Starts with number
    [InlineData("_event")]        // Starts with underscore
    [InlineData(".event")]        // Starts with dot
    [InlineData("-event")]        // Starts with hyphen
    public void RejectsInvalidStartCharacter(string eventType)
    {
        Assert.False(EventTypeValidator.IsValid(eventType),
            $"Event type starting with non-letter should be rejected: '{eventType}'");
    }

    [Theory]
    [InlineData("event:type")]    // Colon - could manipulate group prefix
    [InlineData("event/type")]    // Slash - path traversal
    [InlineData("event\\type")]   // Backslash - path traversal
    [InlineData("event<script>")]  // HTML injection
    [InlineData("event>type")]
    [InlineData("event&type")]
    [InlineData("event|type")]
    [InlineData("event;type")]
    [InlineData("event'type")]    // SQL injection characters
    [InlineData("event\"type")]
    [InlineData("event`type")]
    [InlineData("event$type")]
    [InlineData("event%type")]
    [InlineData("event#type")]
    [InlineData("event@type")]
    [InlineData("event!type")]
    [InlineData("event*type")]    // Wildcard
    [InlineData("event?type")]    // Query string
    [InlineData("event=type")]
    [InlineData("event+type")]
    [InlineData("event type")]    // Space
    [InlineData("event\ttype")]   // Tab
    [InlineData("event\ntype")]   // Newline
    [InlineData("event\rtype")]   // Carriage return
    public void RejectsSpecialCharacters(string eventType)
    {
        Assert.False(EventTypeValidator.IsValid(eventType),
            $"Event type with special characters should be rejected: '{eventType}'");
    }

    [Fact]
    public void RejectsExcessivelyLongEventTypes()
    {
        // 100 chars should be valid (1 + 99)
        var validLong = "a" + new string('b', 99);
        Assert.True(EventTypeValidator.IsValid(validLong),
            "Event type of exactly 100 chars should be valid");

        // 101 chars should be invalid
        var tooLong = "a" + new string('b', 100);
        Assert.False(EventTypeValidator.IsValid(tooLong),
            "Event type over 100 chars should be rejected");

        // Much longer should also be rejected
        var wayTooLong = new string('a', 1000);
        Assert.False(EventTypeValidator.IsValid(wayTooLong),
            "Very long event type should be rejected");
    }

    [Fact]
    public void RejectsGroupNameInjectionAttempts()
    {
        // Attacker trying to escape to admin group
        Assert.False(EventTypeValidator.IsValid("type:admins"));
        Assert.False(EventTypeValidator.IsValid("../admins"));

        // Attacker trying to join another user's group
        Assert.False(EventTypeValidator.IsValid("user:victim-id"));
        Assert.False(EventTypeValidator.IsValid(":user:123"));
    }

    [Fact]
    public void RejectsUnicodeInjectionAttempts()
    {
        // Unicode characters that might look like valid chars
        Assert.False(EventTypeValidator.IsValid("event\u200Btype")); // Zero-width space
        Assert.False(EventTypeValidator.IsValid("event\u00A0type")); // Non-breaking space
        Assert.False(EventTypeValidator.IsValid("еvent"));           // Cyrillic 'е' instead of Latin 'e'
    }
}
