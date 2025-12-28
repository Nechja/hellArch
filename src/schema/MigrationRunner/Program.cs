using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Schema.Contexts;

const string ServiceName = "migration-runner";
var activitySource = new ActivitySource(ServiceName);

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: MigrationRunner <context> <connection-string>");
    Console.Error.WriteLine("  context: auth | user");
    return 1;
}

var contextType = args[0].ToLowerInvariant();
var connectionString = args[1];

// Set up OTEL if endpoint is configured
using var tracerProvider = BuildTracerProvider();

using var activity = activitySource.StartActivity("database.migrate", ActivityKind.Client);
activity?.SetTag("db.context", contextType);
activity?.SetTag("db.system", "postgresql");

Console.WriteLine($"==> Running migrations for {contextType} context");

try
{
    var migrationCount = contextType switch
    {
        "auth" => RunAuthMigrations(connectionString),
        "user" => RunUserMigrations(connectionString),
        _ => throw new ArgumentException($"Unknown context: {contextType}")
    };

    activity?.SetTag("db.migrations.applied", migrationCount);
    activity?.SetStatus(ActivityStatusCode.Ok);
    Console.WriteLine("==> Migrations completed successfully");
    activity?.Stop();
    tracerProvider?.ForceFlush(10000);
    return 0;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.AddException(ex);
    Console.Error.WriteLine($"==> Migration FAILED: {ex.Message}");
    activity?.Stop();
    tracerProvider?.ForceFlush(10000);
    return 1;
}

int RunAuthMigrations(string connString)
{
    var options = new DbContextOptionsBuilder<AuthDbContext>()
        .UseNpgsql(connString)
        .UseSnakeCaseNamingConvention()
        .Options;

    using var context = new AuthDbContext(options);
    var pending = context.Database.GetPendingMigrations().ToList();

    using var migrationActivity = activitySource.StartActivity("migrations.apply");
    migrationActivity?.SetTag("db.migrations.pending", pending.Count);

    foreach (var migration in pending)
    {
        using var stepActivity = activitySource.StartActivity("migration.step");
        stepActivity?.SetTag("db.migration.name", migration);
        Console.WriteLine($"    Applying: {migration}");
    }

    context.Database.Migrate();
    return pending.Count;
}

int RunUserMigrations(string connString)
{
    var options = new DbContextOptionsBuilder<UserDbContext>()
        .UseNpgsql(connString)
        .UseSnakeCaseNamingConvention()
        .Options;

    using var context = new UserDbContext(options);
    var pending = context.Database.GetPendingMigrations().ToList();

    using var migrationActivity = activitySource.StartActivity("migrations.apply");
    migrationActivity?.SetTag("db.migrations.pending", pending.Count);

    foreach (var migration in pending)
    {
        using var stepActivity = activitySource.StartActivity("migration.step");
        stepActivity?.SetTag("db.migration.name", migration);
        Console.WriteLine($"    Applying: {migration}");
    }

    context.Database.Migrate();
    return pending.Count;
}

TracerProvider? BuildTracerProvider()
{
    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
    if (string.IsNullOrEmpty(otlpEndpoint))
    {
        return null;
    }

    Console.WriteLine($"==> OTEL exporting to {otlpEndpoint}");

    return Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(ServiceName)
            .AddAttributes([
                new("service.context", contextType),
                new("deployment.environment", Environment.GetEnvironmentVariable("HELLARCH_ENV") ?? "development")
            ]))
        .AddSource(ServiceName)
        .AddOtlpExporter(opts =>
        {
            // Append /v1/traces for HTTP protocol
            var endpoint = otlpEndpoint.TrimEnd('/') + "/v1/traces";
            opts.Endpoint = new Uri(endpoint);
            opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        })
        .Build();
}
