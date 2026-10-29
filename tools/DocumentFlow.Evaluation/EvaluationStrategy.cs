using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing.Classification;
using DocumentFlow.Application.Documents.Processing.FieldExtraction;
using DocumentFlow.Infrastructure.Processing.Llm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Evaluation;

public enum EvaluationStrategy
{
    Rules,
    Ollama,
    HybridLlmClassifier,
    HybridLlmExtractor,
    RulesV2,
    HybridV2
}

public sealed record EvaluationCliOptions(string Corpus, string Output, EvaluationStrategy Strategy);

public static class EvaluationStrategyConfiguration
{
    public static EvaluationCliOptions ParseArguments(string[] arguments)
    {
        string? corpus = null;
        string? output = null;
        var strategy = EvaluationStrategy.Rules;
        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] is not ("--corpus" or "--output" or "--strategy") || index + 1 >= arguments.Length)
                throw new InvalidOperationException(
                    "Usage: --corpus <path> --output <path> [--strategy rules|ollama|hybrid-llm-classifier|hybrid-llm-extractor|rules-v2|hybrid-v2]");
            if (arguments[index] == "--corpus") corpus = arguments[++index];
            else if (arguments[index] == "--output") output = arguments[++index];
            else strategy = ParseStrategy(arguments[++index]);
        }
        return new EvaluationCliOptions(corpus ?? "evaluation/corpus", output ?? "evaluation/results", strategy);
    }

    public static EvaluationStrategy ParseStrategy(string value) => value.ToLowerInvariant() switch
    {
        "rules" => EvaluationStrategy.Rules,
        "ollama" => EvaluationStrategy.Ollama,
        "hybrid-llm-classifier" => EvaluationStrategy.HybridLlmClassifier,
        "hybrid-llm-extractor" => EvaluationStrategy.HybridLlmExtractor,
        "rules-v2" => EvaluationStrategy.RulesV2,
        "hybrid-v2" => EvaluationStrategy.HybridV2,
        _ => throw new InvalidOperationException($"Unknown evaluation strategy '{value}'.")
    };

    public static bool RequiresOllama(EvaluationStrategy strategy) =>
        strategy is EvaluationStrategy.Ollama or EvaluationStrategy.HybridLlmClassifier or
            EvaluationStrategy.HybridLlmExtractor or EvaluationStrategy.HybridV2;

    public static void Configure(
        IServiceCollection services,
        EvaluationStrategy strategy,
        OllamaOptions? ollamaOptions = null)
    {
        services.RemoveAll<IDocumentClassifier>();
        services.RemoveAll<IDocumentFieldExtractor>();

        if (RequiresOllama(strategy))
        {
            ArgumentNullException.ThrowIfNull(ollamaOptions);
            services.AddSingleton<IOptions<OllamaOptions>>(Options.Create(ollamaOptions));
            services.AddHttpClient<IOllamaClient, OllamaClient>(client =>
                client.BaseAddress = new Uri(ollamaOptions.BaseUrl.TrimEnd('/') + "/"));
        }

        services.AddScoped<IDocumentClassifier>(strategy switch
        {
            EvaluationStrategy.Ollama or EvaluationStrategy.HybridLlmClassifier or EvaluationStrategy.HybridV2 =>
                provider => ActivatorUtilities.CreateInstance<OllamaDocumentClassifier>(provider),
            EvaluationStrategy.RulesV2 => _ => new RuleBasedDocumentClassifierV2(),
            _ => _ => new RuleBasedDocumentClassifier()
        });
        services.AddScoped<IDocumentFieldExtractor>(strategy switch
        {
            EvaluationStrategy.Ollama or EvaluationStrategy.HybridLlmExtractor =>
                provider => ActivatorUtilities.CreateInstance<OllamaDocumentFieldExtractor>(provider),
            EvaluationStrategy.RulesV2 or EvaluationStrategy.HybridV2 => _ => new RuleBasedDocumentFieldExtractorV2(),
            _ => _ => new RuleBasedDocumentFieldExtractor()
        });
    }
}
