using System.Diagnostics;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing.Validation;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentFlow.Application.Documents.Processing;

public sealed class DocumentProcessingPipeline(
    ITextExtractorResolver extractorResolver,
    IDocumentClassifier documentClassifier,
    IDocumentFieldExtractor documentFieldExtractor,
    IDocumentValidator documentValidator,
    IDocumentRouter documentRouter,
    ILogger<DocumentProcessingPipeline>? logger = null)
    : IDocumentProcessingPipeline
{
    private readonly ILogger<DocumentProcessingPipeline> _logger =
        logger ?? NullLogger<DocumentProcessingPipeline>.Instance;

    public async Task ProcessAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Document {DocumentId}: text extraction started.",
            document.Id);

        var extractor = extractorResolver.Resolve(document);
        var result = await extractor.ExtractAsync(document, cancellationToken);

        _logger.LogDebug(
            "Document {DocumentId}: text extraction completed using {Method}.",
            document.Id,
            result.Method);

        document.SetExtractedText(result.Text);
        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.TextExtracted,
            result.Method switch
            {
                TextExtractionMethod.PdfText => "Text extracted using PDF text layer.",
                TextExtractionMethod.DocxText => "Text extracted using DOCX text.",
                _ => "Text extracted using OCR."
            }));

        var classification = await documentClassifier.ClassifyAsync(
            result.Text,
            cancellationToken);

        _logger.LogDebug(
            "Document {DocumentId}: classification completed as {DocumentType} with confidence {Confidence}.",
            document.Id,
            classification.Type,
            classification.Confidence);

        if (classification.Type != DocumentType.Unknown)
        {
            document.SetClassification(
                classification.Type,
                classification.Confidence);
        }

        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.DocumentClassified,
            classification.Type == DocumentType.Unknown
                ? "Document type could not be determined confidently."
                : $"Document classified as {classification.Type} with confidence {classification.Confidence:F2}."));

        if (classification.Type != DocumentType.Unknown)
        {
            var fields = await documentFieldExtractor.ExtractAsync(
                document.Type,
                document.ExtractedText!,
                cancellationToken);

            foreach (var field in fields)
            {
                document.AddField(field.Name, field.Value, field.Confidence);
            }

            _logger.LogDebug(
                "Document {DocumentId}: extracted {FieldCount} fields.",
                document.Id,
                fields.Count);

            document.Events.Add(new DocumentEvent(
                document.Id,
                DocumentEventType.FieldsExtracted,
                $"Extracted {fields.Count} document fields."));
        }

        var validationResult = documentValidator.Validate(document);
        if (!validationResult.IsValid)
        {
            _logger.LogInformation(
                "Document {DocumentId}: validation requires manual review. IssueCount: {IssueCount}.",
                document.Id,
                validationResult.Issues.Count);

            document.MarkNeedsReview();
            document.Events.Add(new DocumentEvent(
                document.Id,
                DocumentEventType.ManualReviewRequired,
                CreateReviewMessage(validationResult.Issues)));
            LogPipelineDuration(document.Id, stopwatch);
            return;
        }

        _logger.LogDebug(
            "Document {DocumentId}: validation succeeded.",
            document.Id);

        var department = documentRouter.Route(document.Type);

        _logger.LogDebug(
            "Document {DocumentId}: routed to {Department}.",
            document.Id,
            department);

        document.SetTargetDepartment(department);
        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.DocumentRouted,
            $"Document routed to {department}."));

        LogPipelineDuration(document.Id, stopwatch);
    }

    private void LogPipelineDuration(Guid documentId, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        _logger.LogDebug(
            "Document {DocumentId}: pipeline completed in {ElapsedMs} ms.",
            documentId,
            stopwatch.ElapsedMilliseconds);
    }

    private static string CreateReviewMessage(
        IEnumerable<DocumentValidationIssue> issues)
    {
        const int maximumMessageLength = 2000;
        var reasons = issues.Select(issue => issue.Code switch
        {
            DocumentValidationIssueCodes.UnknownDocumentType => "unknown document type",
            DocumentValidationIssueCodes.MissingRequiredField => $"missing field {issue.FieldName}",
            DocumentValidationIssueCodes.InvalidFieldValue => $"field {issue.FieldName} is invalid",
            DocumentValidationIssueCodes.LowFieldConfidence => $"field {issue.FieldName} has low confidence",
            _ => issue.Message
        });
        var message = $"Manual review required: {string.Join("; ", reasons)}.";
        return message.Length <= maximumMessageLength
            ? message
            : message[..maximumMessageLength];
    }
}
