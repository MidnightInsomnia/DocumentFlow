using System.Text;
using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

public sealed class ImageOcrTextExtractor(
    IFileStorage fileStorage,
    IOcrEngine ocrEngine,
    TiffRasterizer? tiffRasterizer = null) : ITextExtractor
{
    private readonly TiffRasterizer _tiffRasterizer = tiffRasterizer ?? new TiffRasterizer();

    public async Task<TextExtractionResult> ExtractAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var isTiff = string.Equals(document.ContentType, "image/tiff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(document.ContentType, "image/tif", StringComparison.OrdinalIgnoreCase);
        if (!isTiff &&
            !string.Equals(document.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(document.ContentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.UnsupportedFormat,
                "The image OCR extractor supports JPEG, PNG and TIFF images only.");
        }

        await using var image = await fileStorage.OpenReadAsync(
            document.StoredFileName,
            cancellationToken);
        if (isTiff)
        {
            using var pages = _tiffRasterizer.Load(image, cancellationToken);
            var combinedText = new StringBuilder();
            for (var page = 1; page <= pages.PageCount; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await using var png = await pages.RenderPngAsync(page, cancellationToken);
                    var text = await ocrEngine.RecognizeAsync(png, cancellationToken);
                    if (string.IsNullOrWhiteSpace(text))
                        throw new TextExtractionException(TextExtractionFailureReason.OcrFailed,
                            "OCR did not produce usable text.");
                    if (combinedText.Length > 0) combinedText.AppendLine();
                    combinedText.Append("--- PAGE ").Append(page).AppendLine(" ---");
                    combinedText.Append(text.Trim());
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception)
                {
                    throw new TextExtractionException(TextExtractionFailureReason.OcrFailed,
                        $"Не удалось распознать страницу {page} TIFF.", exception);
                }
            }
            return new TextExtractionResult(combinedText.ToString(), TextExtractionMethod.Ocr);
        }

        var imageText = await ocrEngine.RecognizeAsync(image, cancellationToken);
        var usableText = ExtractedTextPolicy.EnsureUsable(
            imageText,
            TextExtractionFailureReason.OcrFailed,
            "OCR did not produce usable text.");

        return new TextExtractionResult(usableText, TextExtractionMethod.Ocr);
    }
}
