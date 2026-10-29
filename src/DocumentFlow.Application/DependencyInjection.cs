using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Application.Documents.Processing.Classification;
using DocumentFlow.Application.Documents.Processing.FieldExtraction;
using DocumentFlow.Application.Documents.Processing.Routing;
using DocumentFlow.Application.Documents.Processing.Validation;
using DocumentFlow.Application.Documents.Files;
using DocumentFlow.Application.Documents.Read;
using DocumentFlow.Application.Documents.Review;
using DocumentFlow.Application.Documents.Retry;
using DocumentFlow.Application.Documents.Reprocess;
using DocumentFlow.Application.Documents.Upload;
using DocumentFlow.Application.Documents.Archive;
using DocumentFlow.Application.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentFlow.Application;

public static class DependencyInjection
{
    // The frozen evaluation runner still uses the parameterless registration.
    // Its original routing defaults live in this embedded configuration, not in C# branches.
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddApplication(RoutingOptions.EmbeddedDefaultsConfiguration());

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RoutingOptions>()
            .Configure(options => options.Load(configuration))
            .ValidateOnStart();
        services.AddScoped<DocumentUploadService>();
        services.AddScoped<DocumentArchiveService>();
        services.AddScoped<DocumentProcessingService>();
        services.AddScoped<DocumentProcessingRecoveryService>();
        services.AddScoped<DocumentReadService>();
        services.AddScoped<DocumentInsightsService>();
        services.AddScoped<DocumentFileService>();
        services.AddScoped<DocumentReviewService>();
        services.AddScoped<DocumentActionAuditService>();
        services.AddScoped<DocumentReviewContextService>();
        services.AddScoped<DocumentRetryService>();
        services.AddScoped<DocumentReprocessService>();
        services.AddScoped<DashboardService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDocumentProcessingPipeline, DocumentProcessingPipeline>();
        services.AddScoped<IDocumentClassifier, ProductionRuleBasedDocumentClassifier>();
        services.AddScoped<IDocumentFieldExtractor, ProductionRuleBasedDocumentFieldExtractor>();
        services.AddScoped<IDocumentValidator, RuleBasedDocumentValidator>();
        services.AddScoped<IDocumentRouter, RuleBasedDocumentRouter>();

        return services;
    }
}
