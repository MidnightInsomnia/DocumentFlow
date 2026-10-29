using Microsoft.Extensions.DependencyInjection;

namespace DocumentFlow.Infrastructure.Health;

public static class HealthCheckServiceCollectionExtensions
{
    private static readonly string[] ReadyTags = ["ready"];

    public static IServiceCollection AddDocumentFlowHealthChecks(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>(
                "postgres",
                tags: ReadyTags,
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<DocumentStorageHealthCheck>(
                "storage",
                tags: ReadyTags,
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
