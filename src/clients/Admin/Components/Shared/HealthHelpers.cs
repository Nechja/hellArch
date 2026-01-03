using System.Globalization;
using MudBlazor;

namespace Admin.Helpers;

public static class HealthHelpers
{
    public static Color GetHealthColor(string status) => status.ToLowerInvariant() switch
    {
        "healthy" => Color.Success,
        "degraded" => Color.Warning,
        _ => Color.Error
    };

    public static string GetHealthIcon(string status) => status.ToLowerInvariant() switch
    {
        "healthy" => Icons.Material.Filled.CheckCircle,
        "degraded" => Icons.Material.Filled.Warning,
        _ => Icons.Material.Filled.Error
    };

    public static Severity GetHealthSeverity(string status) => status.ToLowerInvariant() switch
    {
        "healthy" => Severity.Success,
        "degraded" => Severity.Warning,
        _ => Severity.Error
    };

    public static string FormatServiceName(string name) => name switch
    {
        "auth-db" => "Auth DB",
        "user-db" => "User DB",
        "rabbitmq" => "RabbitMQ",
        _ => char.ToUpper(name[0], CultureInfo.InvariantCulture) + name[1..]
    };

    public static string FormatWorkerName(string queueName)
    {
        var parts = queueName.Split('.', '_', '-');
        return string.Join(" ", parts.Select(p =>
            string.IsNullOrEmpty(p) ? "" : char.ToUpper(p[0], CultureInfo.InvariantCulture) + p[1..]));
    }
}
