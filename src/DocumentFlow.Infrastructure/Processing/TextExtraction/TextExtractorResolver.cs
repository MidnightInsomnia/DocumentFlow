using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

public sealed class TextExtractorResolver(
    PdfHybridTextExtractor pdfHybridTextExtractor,
    ImageOcrTextExtractor imageOcrTextExtractor,
    DocxTextExtractor? docxTextExtractor = null)
    : ITextExtractorResolver
{
    public ITextExtractor Resolve(Document document)
    {
        if (string.Equals(
            document.ContentType,
            "application/pdf",
            StringComparison.OrdinalIgnoreCase))
        {
            return pdfHybridTextExtractor;
        }

        if (string.Equals(document.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(document.ContentType, "image/png", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(document.ContentType, "image/tiff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(document.ContentType, "image/tif", StringComparison.OrdinalIgnoreCase))
        {
            return imageOcrTextExtractor;
        }

        if (string.Equals(document.ContentType,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase) && docxTextExtractor is not null)
        {
            return docxTextExtractor;
        }

        throw new TextExtractionException(
            TextExtractionFailureReason.UnsupportedFormat,
            $"No text extractor is available for content type '{document.ContentType}'.");
    }
}
