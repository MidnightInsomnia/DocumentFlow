using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

public sealed class PdfHybridTextExtractor(
    IPdfTextExtractor pdfTextExtractor,
    IPdfOcrTextExtractor pdfOcrTextExtractor) : ITextExtractor
{
    public async Task<TextExtractionResult> ExtractAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        try
        {
            return await pdfTextExtractor.ExtractAsync(document, cancellationToken);
        }
        catch (TextExtractionException exception)
            when (exception.Reason == TextExtractionFailureReason.NoTextLayer)
        {
            return await pdfOcrTextExtractor.ExtractAsync(document, cancellationToken);
        }
    }
}
