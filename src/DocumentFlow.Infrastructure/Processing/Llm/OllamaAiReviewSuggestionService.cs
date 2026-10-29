using System.Diagnostics;
using System.Text.Json;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Documents.Review.AI;
using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Infrastructure.Processing.Llm;

public sealed class OllamaAiReviewSuggestionService(
    IDocumentReviewContextRepository repository,
    OllamaDocumentClassifier classifier,
    OllamaDocumentFieldExtractor extractor,
    IOptions<AiReviewOptions> options,
    ILogger<OllamaAiReviewSuggestionService> logger) : IAiReviewSuggestionService
{
    private readonly AiReviewOptions _options = options.Value;

    public async Task<AiReviewSuggestionResult> GetAsync(Guid documentId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return new AiReviewSuggestionResult(AiReviewSuggestionOutcome.Disabled,
                ErrorMessage: "AI-assisted review is disabled.");

        var document = await repository.GetByIdAsync(documentId, cancellationToken);
        if (document is null) return new AiReviewSuggestionResult(AiReviewSuggestionOutcome.NotFound);
        if (document.Status != DocumentStatus.NeedsReview)
            return new AiReviewSuggestionResult(AiReviewSuggestionOutcome.NotReviewable,
                ErrorMessage: "AI suggestions are available only for documents awaiting manual review.");
        if (string.IsNullOrWhiteSpace(document.ExtractedText))
            return new AiReviewSuggestionResult(AiReviewSuggestionOutcome.MissingExtractedText,
                ErrorMessage: "The document has no extracted text available for an AI suggestion.");

        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("AI review suggestion started for document {DocumentId}.", documentId);
        try
        {
            var classification = await classifier.ClassifyAsync(document.ExtractedText, cancellationToken);
            var fields = classification.Type == DocumentType.Unknown
                ? []
                : await extractor.ExtractAsync(classification.Type, document.ExtractedText, cancellationToken);
            var suggestion = new AiReviewSuggestion(
                classification.Type,
                classification.Confidence,
                fields.Select(field => new AiReviewSuggestedField(field.Name, field.Value!, field.Confidence)).ToArray());
            logger.LogInformation(
                "AI review suggestion completed for document {DocumentId}. SuggestedType: {SuggestedType}; FieldCount: {FieldCount}; DurationMs: {DurationMs}.",
                documentId, suggestion.SuggestedType, suggestion.Fields.Count, stopwatch.ElapsedMilliseconds);
            return new AiReviewSuggestionResult(AiReviewSuggestionOutcome.Success, suggestion);
        }
        catch (LlmProcessingException exception)
        {
            var outcome = exception.Message.Contains("HTTP 404", StringComparison.OrdinalIgnoreCase) ||
                          exception.Message.Contains("not installed", StringComparison.OrdinalIgnoreCase)
                ? AiReviewSuggestionOutcome.ModelUnavailable
                : exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                  exception.Message.Contains("exceeded", StringComparison.OrdinalIgnoreCase)
                    ? AiReviewSuggestionOutcome.TimedOut
                    : exception.InnerException is HttpRequestException or TaskCanceledException ||
                      exception.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                        ? AiReviewSuggestionOutcome.UpstreamUnavailable
                        : AiReviewSuggestionOutcome.InvalidUpstreamResponse;
            if (outcome == AiReviewSuggestionOutcome.InvalidUpstreamResponse)
            {
                // JSON parser diagnostics expose the failing path/type, never document text or raw AI output.
                var jsonError = exception.InnerException as JsonException;
                logger.LogWarning(
                    "AI invalid response for document {DocumentId}. ContractCategory: {ContractCategory}; JsonPath: {JsonPath}; JsonReason: {JsonReason}.",
                    documentId, jsonError is null ? "ApplicationValidation" : "JsonDeserialization",
                    jsonError?.Path, jsonError?.Message);
            }
            logger.LogWarning(
                "AI review suggestion failed for document {DocumentId}. Category: {Category}; DurationMs: {DurationMs}.",
                documentId, outcome, stopwatch.ElapsedMilliseconds);
            return new AiReviewSuggestionResult(
                outcome,
                ErrorMessage: outcome switch
                {
                    AiReviewSuggestionOutcome.ModelUnavailable => "The configured local AI model is unavailable.",
                    AiReviewSuggestionOutcome.TimedOut => "The local AI service did not respond before the timeout.",
                    AiReviewSuggestionOutcome.UpstreamUnavailable => "The local AI service is unavailable.",
                    _ => "The local AI service returned an invalid response."
                });
        }
    }
}
