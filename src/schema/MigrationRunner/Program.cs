using Microsoft.EntityFrameworkCore;
using Schema.Contexts;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: MigrationRunner <context> <connection-string>");
    Console.Error.WriteLine("  context: auth | user");
    return 1;
}

var contextType = args[0].ToLowerInvariant();
var connectionString = args[1];

Console.WriteLine($"==> Running migrations for {contextType} context");

try
{
    if (contextType == "auth")
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new AuthDbContext(options);
        context.Database.Migrate();
    }
    else if (contextType == "user")
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new UserDbContext(options);
        context.Database.Migrate();
    }
    else
    {
        Console.Error.WriteLine($"Unknown context: {contextType}");
        return 1;
    }

    Console.WriteLine("==> Migrations completed successfully");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"==> Migration FAILED: {ex.Message}");
    return 1;
}
