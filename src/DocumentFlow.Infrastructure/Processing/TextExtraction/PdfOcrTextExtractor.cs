using System.Text;
using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;
using PDFtoImage;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

public sealed class PdfOcrTextExtractor(
    IFileStorage fileStorage,
    IOcrEngine ocrEngine) : IPdfOcrTextExtractor
{
    private const int MaxOcrPdfPages = 30;
    private static readonly RenderOptions RenderOptions = new() { Dpi = 200 };

    public async Task<TextExtractionResult> ExtractAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(document.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.UnsupportedFormat,
                "The PDF OCR extractor supports PDF documents only.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows() &&
            !OperatingSystem.IsLinux() &&
            !OperatingSystem.IsMacOS() &&
            !OperatingSystem.IsBrowser() &&
            !OperatingSystem.IsAndroid() &&
            !OperatingSystem.IsIOS() &&
            !OperatingSystem.IsMacCatalyst())
        {
            throw new PlatformNotSupportedException(
                "PDF OCR rendering is not supported on this operating system.");
        }

        try
        {
            await using var pdfStream = await fileStorage.OpenReadAsync(
                document.StoredFileName,
                cancellationToken);
#pragma warning disable CA1416 // PDFtoImage supports the guarded desktop/server platforms above.
            var pageCount = Conversion.GetPageCount(
                pdfStream,
                leaveOpen: true,
                password: null);

            if (pageCount > MaxOcrPdfPages)
            {
                throw new TextExtractionException(
                    TextExtractionFailureReason.OcrFailed,
                    "PDF contains more than 30 pages and cannot be OCR processed.");
            }

            var combinedText = new StringBuilder();
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pdfStream.Position = 0;

                await using var imageStream = new MemoryStream();
                Conversion.SavePng(
                    imageStream,
                    pdfStream,
                    pageIndex,
                    leaveOpen: true,
                    password: null,
                    options: RenderOptions);
#pragma warning restore CA1416
                imageStream.Position = 0;

                var pageText = await ocrEngine.RecognizeAsync(
                    imageStream,
                    cancellationToken);

                if (combinedText.Length > 0)
                {
                    combinedText.AppendLine();
                    combinedText.AppendLine();
                }

                combinedText.Append(pageText.Trim());
            }

            var usableText = ExtractedTextPolicy.EnsureUsable(
                combinedText.ToString(),
                TextExtractionFailureReason.OcrFailed,
                "OCR did not produce usable text.");

            return new TextExtractionResult(usableText, TextExtractionMethod.Ocr);
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
                "The PDF could not be rendered for OCR.",
                exception);
        }
    }
}
