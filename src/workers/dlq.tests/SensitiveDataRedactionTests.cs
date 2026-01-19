using System.Text.Json;
using DlqWorker.Services;

namespace DlqWorker.Tests;

/// <summary>
/// Tests that verify sensitive data is properly redacted before storage.
/// These tests prevent regressions that could leak PII in dead letter storage.
/// </summary>
public class SensitiveDataRedactionTests
{
    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("PASSWORD")]
    [InlineData("secret")]
    [InlineData("token")]
    [InlineData("apikey")]
    [InlineData("api_key")]
    [InlineData("authorization")]
    [InlineData("credential")]
    [InlineData("ssn")]
    [InlineData("credit_card")]
    public void RedactsSensitiveFieldsRegardlessOfCase(string fieldName)
    {
        // Arrange - simulate an event body containing a sensitive field
        var json = $$"""{"user": "john", "{{fieldName}}": "super_secret_value"}""";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var redacted = DlqConsumer.RedactSensitiveFields(element);

        // Assert - sensitive value must be redacted
        var redactedJson = redacted.GetRawText();
        Assert.DoesNotContain("super_secret_value", redactedJson);
        Assert.Contains("[REDACTED]", redactedJson);
        // Non-sensitive field should be preserved
        Assert.Contains("john", redactedJson);
    }

    [Fact]
    public void RedactsNestedSensitiveFields()
    {
        // Arrange - sensitive data buried in nested object
        var json = """
        {
            "user": {
                "name": "john",
                "credentials": {
                    "password": "hunter2",
                    "token": "secret-token"
                }
            }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var redacted = DlqConsumer.RedactSensitiveFields(element);

        // Assert
        var redactedJson = redacted.GetRawText();
        Assert.DoesNotContain("hunter2", redactedJson);
        Assert.DoesNotContain("secret-token", redactedJson);
        Assert.Contains("john", redactedJson);
    }

    [Fact]
    public void RedactsSensitiveFieldsInArrays()
    {
        // Arrange - array of objects containing sensitive data
        var json = """
        {
            "users": [
                {"name": "alice", "password": "alice123"},
                {"name": "bob", "password": "bob456"}
            ]
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var redacted = DlqConsumer.RedactSensitiveFields(element);

        // Assert
        var redactedJson = redacted.GetRawText();
        Assert.DoesNotContain("alice123", redactedJson);
        Assert.DoesNotContain("bob456", redactedJson);
        Assert.Contains("alice", redactedJson);
        Assert.Contains("bob", redactedJson);
    }

    [Fact]
    public void PreservesNonSensitiveData()
    {
        // Arrange - normal event data without sensitive fields
        var json = """
        {
            "event_type": "user.created",
            "data": {
                "id": "123",
                "email": "test@example.com",
                "created_at": "2024-01-15T10:30:00Z"
            }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var redacted = DlqConsumer.RedactSensitiveFields(element);

        // Assert - all data should be preserved
        var redactedJson = redacted.GetRawText();
        Assert.Contains("user.created", redactedJson);
        Assert.Contains("123", redactedJson);
        Assert.Contains("test@example.com", redactedJson);
        Assert.DoesNotContain("[REDACTED]", redactedJson);
    }

    [Fact]
    public void HandlesEmptyObject()
    {
        var element = JsonDocument.Parse("{}").RootElement;

        var redacted = DlqConsumer.RedactSensitiveFields(element);

        Assert.Equal("{}", redacted.GetRawText());
    }

    [Fact]
    public void HandlesEmptyArray()
    {
        var json = """{"items": []}""";
        var element = JsonDocument.Parse(json).RootElement;

        var redacted = DlqConsumer.RedactSensitiveFields(element);

        Assert.Contains("[]", redacted.GetRawText());
    }

    [Fact]
    public void PreservesPrimitiveValues()
    {
        var json = """
        {
            "count": 42,
            "enabled": true,
            "ratio": 3.14,
            "nothing": null
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var redacted = DlqConsumer.RedactSensitiveFields(element);
        var redactedJson = redacted.GetRawText();

        Assert.Contains("42", redactedJson);
        Assert.Contains("true", redactedJson);
        Assert.Contains("3.14", redactedJson);
        Assert.Contains("null", redactedJson);
    }

    [Fact]
    public void DoesNotRedactFieldsContainingSensitiveWordsAsSubstring()
    {
        // "reset_password_token" contains "password" but the field name itself
        // is not in our sensitive list, so it depends on exact matching
        var json = """{"user_id": "123", "has_password_reset": true}""";
        var element = JsonDocument.Parse(json).RootElement;

        var redacted = DlqConsumer.RedactSensitiveFields(element);
        var redactedJson = redacted.GetRawText();

        // The field name "has_password_reset" is not exactly "password"
        // so it should not be redacted
        Assert.Contains("true", redactedJson);
    }
}
