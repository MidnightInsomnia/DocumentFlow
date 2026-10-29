using System.Text;
using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

public sealed class PdfTextExtractor(IFileStorage fileStorage) : IPdfTextExtractor
{
    public async Task<TextExtractionResult> ExtractAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                document.ContentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                Path.GetExtension(document.StoredFileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.UnsupportedFormat,
                "The PDF text extractor supports PDF documents only.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var stream = await fileStorage.OpenReadAsync(
                document.StoredFileName,
                cancellationToken);
            using var pdfDocument = PdfDocument.Open(stream);
            var text = new StringBuilder();

            foreach (var page in pdfDocument.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (text.Length > 0)
                {
                    text.AppendLine();
                }

                text.Append(ContentOrderTextExtractor.GetText(page));
            }

            var extractedText = ExtractedTextPolicy.EnsureUsable(
                text.ToString(),
                TextExtractionFailureReason.NoTextLayer,
                "PDF does not contain an extractable text layer.");

            return new TextExtractionResult(
                extractedText,
                TextExtractionMethod.PdfText);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TextExtractionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.InvalidDocument,
                "The PDF could not be read or is corrupted.",
                exception);
        }
    }
}
