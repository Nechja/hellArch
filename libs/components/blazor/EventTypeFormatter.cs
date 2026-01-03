namespace HellArch.Components;

public static class EventTypeFormatter
{
    public static string GetShort(string eventType)
    {
        var parts = eventType.Split('.');
        return parts.Length >= 4 ? $"{parts[^3]}.{parts[^2]}" : eventType;
    }
}
