using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentFlow.Infrastructure.Workspace;

public static class WorkspaceServiceCollectionExtensions
{
    // API host only: register here, not in both API and Worker. No migrations or network calls during registration.
    public static IServiceCollection AddDocumentWorkspace(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SemanticSearchOptions>().Bind(configuration.GetSection(SemanticSearchOptions.SectionName))
            .Validate(x => x.BatchSize is >= 1 and <= 32 && x.IntervalSeconds is >= 1 and <= 3600 &&
                x.TimeoutSeconds is >= 1 and <= 300 && x.MaxInputCharacters is >= 1000 and <= 64000,
                "SemanticSearch limits: BatchSize 1..32, IntervalSeconds 1..3600, TimeoutSeconds 1..300, MaxInputCharacters 1000..64000.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Model) && x.Model.Length <= 200,
                "SemanticSearch:Model must contain 1..200 characters.")
            .ValidateOnStart();
        services.AddSingleton<WorkspaceIndexState>();
        services.AddHttpClient("DocumentWorkspace.Ollama", client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddScoped<LocalEmbeddingClient>();
        services.AddScoped<AiKindProposalClient>();
        services.AddScoped<KindDiscoveryService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<DocumentWorkspaceService>();
        services.AddScoped<DocumentSearchService>();
        services.AddScoped<WorkspaceIndexingService>();
        services.AddScoped<WorkspaceInitializer>();
        services.AddHostedService<WorkspaceIndexingHostedService>();
        return services;
    }
}
