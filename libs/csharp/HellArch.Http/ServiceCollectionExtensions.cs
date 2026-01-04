using Microsoft.Extensions.DependencyInjection;

namespace HellArch.Http;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResilientHttpClients(this IServiceCollection services)
    {
        services.AddTransient<ResilientHttpHandler>();

        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.AddHttpMessageHandler<ResilientHttpHandler>();
        });

        return services;
    }
}
