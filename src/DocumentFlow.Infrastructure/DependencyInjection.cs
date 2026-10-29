using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Infrastructure.Files;
using DocumentFlow.Infrastructure.Persistence;
using DocumentFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using DocumentFlow.Infrastructure.Persistence.Repositories;
using DocumentFlow.Infrastructure.Processing.TextExtraction;
using DocumentFlow.Infrastructure.Processing.Ocr;
using DocumentFlow.Infrastructure.Processing.Llm;
using DocumentFlow.Application.Documents.Review.AI;
using Microsoft.EntityFrameworkCore;
using DocumentFlow.Infrastructure.ReviewWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeIdentity = true)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured. " +
                "Set ConnectionStrings__Postgres or configure it in appsettings.");

        services.AddDbContext<DocumentFlowDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<WorkerHeartbeatStore>();
        if (includeIdentity)
        {
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 12;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<DocumentFlowDbContext>()
            .AddDefaultTokenProviders();
        }
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentArchiveRepository, DocumentArchiveRepository>();
        services.AddScoped<IDocumentProcessingRepository, DocumentProcessingRepository>();
        services.AddScoped<IDocumentReadRepository, DocumentReadRepository>();
        services.AddScoped<IDocumentInsightsRepository, DocumentInsightsRepository>();
        services.AddScoped<IDocumentFileRepository, DocumentFileRepository>();
        services.AddSingleton<IFileContentHasher, Sha256FileContentHasher>();
        services.AddScoped<IDocumentReviewRepository, DocumentReviewRepository>();
        services.AddScoped<IDocumentReviewContextRepository, DocumentReviewContextRepository>();
        services.AddScoped<IDocumentRetryRepository, DocumentRetryRepository>();
        services.AddScoped<IDocumentReprocessRepository, DocumentReprocessRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDocumentActionAuditRepository, DocumentActionAuditRepository>();
        services.AddOptions<TesseractOptions>()
            .Bind(configuration.GetSection(TesseractOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ExecutablePath),
                "Tesseract ExecutablePath is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Languages),
                "Tesseract Languages is required.")
            .Validate(
                options => options.PageSegmentationMode >= 0,
                "Tesseract PageSegmentationMode must be non-negative.")
            .Validate(
                options => options.TimeoutSeconds > 0,
                "Tesseract TimeoutSeconds must be greater than zero.");
        services.AddScoped<IOcrEngine, TesseractOcrEngine>();
        services.AddScoped<PdfTextExtractor>();
        services.AddScoped<IPdfTextExtractor>(provider =>
            provider.GetRequiredService<PdfTextExtractor>());
        services.AddScoped<PdfOcrTextExtractor>();
        services.AddScoped<IPdfOcrTextExtractor>(provider =>
            provider.GetRequiredService<PdfOcrTextExtractor>());
        services.AddScoped<PdfHybridTextExtractor>();
        services.AddSingleton<TiffRasterizer>();
        services.AddScoped<ImageOcrTextExtractor>();
        services.AddScoped<DocxTextExtractor>();
        services.AddScoped<ITextExtractorResolver, TextExtractorResolver>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddOptions<AiReviewOptions>()
            .Bind(configuration.GetSection(AiReviewOptions.SectionName));
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName));
        services.AddHttpClient<IOllamaClient, OllamaClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<OllamaDocumentClassifier>();
        services.AddScoped<OllamaDocumentFieldExtractor>();
        services.AddScoped<IAiReviewSuggestionService, OllamaAiReviewSuggestionService>();

        services.AddReviewWork();
        return services;
    }
}
