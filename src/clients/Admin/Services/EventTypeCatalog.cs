namespace Admin.Services;

public sealed record EventTypeInfo(
    string Type,
    string Domain,
    string Entity,
    string Action,
    string Description,
    EventSeverity Severity);

public enum EventSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public static class EventTypeCatalog
{
    public static IReadOnlyList<EventTypeInfo> All { get; } =
    [
        // Auth Events
        new("hellarch.auth.user.created.v1", "Auth", "User", "Created", "New user account created", EventSeverity.Info),
        new("hellarch.auth.user.updated.v1", "Auth", "User", "Updated", "User profile updated", EventSeverity.Info),
        new("hellarch.auth.user.deleted.v1", "Auth", "User", "Deleted", "User account deleted", EventSeverity.Medium),
        new("hellarch.auth.session.created.v1", "Auth", "Session", "Created", "New login session started", EventSeverity.Info),
        new("hellarch.auth.session.ended.v1", "Auth", "Session", "Ended", "Session ended (logout/expiry)", EventSeverity.Info),
        new("hellarch.auth.account.linked.v1", "Auth", "Account", "Linked", "OAuth provider linked", EventSeverity.Info),
        new("hellarch.auth.account.unlinked.v1", "Auth", "Account", "Unlinked", "OAuth provider unlinked", EventSeverity.Info),

        // Security Events
        new("hellarch.security.login.failed.v1", "Security", "Login", "Failed", "Failed login attempt", EventSeverity.Low),
        new("hellarch.security.login.suspicious.v1", "Security", "Login", "Suspicious", "Suspicious login pattern detected (brute force, etc.)", EventSeverity.High),
        new("hellarch.security.account.locked.v1", "Security", "Account", "Locked", "Account locked due to security concerns", EventSeverity.High),
        new("hellarch.security.account.unlocked.v1", "Security", "Account", "Unlocked", "Account unlocked", EventSeverity.Medium),
        new("hellarch.security.password.changed.v1", "Security", "Password", "Changed", "User password changed", EventSeverity.Medium),
        new("hellarch.security.permission.changed.v1", "Security", "Permission", "Changed", "User permissions/role modified", EventSeverity.High),
        new("hellarch.security.session.suspicious.v1", "Security", "Session", "Suspicious", "Session from new device/location", EventSeverity.Medium),
        new("hellarch.security.token.revoked.v1", "Security", "Token", "Revoked", "API token revoked", EventSeverity.Medium),

        // Infrastructure Events
        new("hellarch.infra.service.unhealthy.v1", "Infrastructure", "Service", "Unhealthy", "Service health check failing", EventSeverity.Critical),
        new("hellarch.infra.service.recovered.v1", "Infrastructure", "Service", "Recovered", "Service recovered from unhealthy state", EventSeverity.Info),
        new("hellarch.infra.database.connection.failed.v1", "Infrastructure", "Database", "Connection Failed", "Database connection error", EventSeverity.Critical),
        new("hellarch.infra.database.connection.recovered.v1", "Infrastructure", "Database", "Connection Recovered", "Database connection restored", EventSeverity.Info),
        new("hellarch.infra.queue.connection.failed.v1", "Infrastructure", "Queue", "Connection Failed", "Message queue connection error", EventSeverity.Critical),
        new("hellarch.infra.queue.connection.recovered.v1", "Infrastructure", "Queue", "Connection Recovered", "Message queue connection restored", EventSeverity.Info),
        new("hellarch.infra.cache.connection.failed.v1", "Infrastructure", "Cache", "Connection Failed", "Cache connection error", EventSeverity.High),
        new("hellarch.infra.cache.connection.recovered.v1", "Infrastructure", "Cache", "Connection Recovered", "Cache connection restored", EventSeverity.Info),
        new("hellarch.infra.certificate.expiring.v1", "Infrastructure", "Certificate", "Expiring", "SSL certificate expiring soon", EventSeverity.High),
        new("hellarch.infra.resource.threshold.v1", "Infrastructure", "Resource", "Threshold", "Resource usage exceeded threshold (CPU/memory/disk)", EventSeverity.High),

        // Operations Events
        new("hellarch.ops.deployment.started.v1", "Operations", "Deployment", "Started", "Deployment initiated", EventSeverity.Info),
        new("hellarch.ops.deployment.completed.v1", "Operations", "Deployment", "Completed", "Deployment successful", EventSeverity.Info),
        new("hellarch.ops.deployment.failed.v1", "Operations", "Deployment", "Failed", "Deployment failed", EventSeverity.Critical),
        new("hellarch.ops.deployment.rolledback.v1", "Operations", "Deployment", "Rolled Back", "Deployment rolled back", EventSeverity.High),
        new("hellarch.ops.config.changed.v1", "Operations", "Config", "Changed", "Configuration value changed", EventSeverity.Medium),
        new("hellarch.ops.ratelimit.exceeded.v1", "Operations", "Rate Limit", "Exceeded", "Rate limit triggered", EventSeverity.High),
        new("hellarch.ops.webhook.failing.v1", "Operations", "Webhook", "Failing", "Webhook has consecutive delivery failures", EventSeverity.High),
        new("hellarch.ops.backup.completed.v1", "Operations", "Backup", "Completed", "Backup completed successfully", EventSeverity.Info),
        new("hellarch.ops.backup.failed.v1", "Operations", "Backup", "Failed", "Backup failed", EventSeverity.Critical),

        // Files Events
        new("hellarch.files.file.uploaded.v1", "Files", "File", "Uploaded", "File uploaded successfully", EventSeverity.Info),
        new("hellarch.files.file.deleted.v1", "Files", "File", "Deleted", "File deleted", EventSeverity.Info),
        new("hellarch.files.file.processed.v1", "Files", "File", "Processed", "File processing completed (thumbnail, etc.)", EventSeverity.Info),
    ];

    public static IReadOnlyList<string> Domains { get; } = All
        .Select(e => e.Domain)
        .Distinct()
        .Order()
        .ToList();

    public static IReadOnlyList<EventTypeInfo> GetByDomain(string domain) =>
        All.Where(e => e.Domain == domain).ToList();

    public static string GetSeverityColor(EventSeverity severity) => severity switch
    {
        EventSeverity.Critical => "text-red-500",
        EventSeverity.High => "text-orange-400",
        EventSeverity.Medium => "text-yellow-400",
        EventSeverity.Low => "text-blue-400",
        _ => "text-text-secondary"
    };

    public static string GetSeverityBadge(EventSeverity severity) => severity switch
    {
        EventSeverity.Critical => "badge-danger",
        EventSeverity.High => "badge-warning",
        EventSeverity.Medium => "badge-info",
        _ => "badge"
    };
}
