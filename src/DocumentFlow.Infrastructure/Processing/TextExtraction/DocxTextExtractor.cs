using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using Document = DocumentFlow.Domain.Entities.Document;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

public sealed class DocxTextExtractor(IFileStorage fileStorage) : ITextExtractor
{
    private const string DocxMime =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public async Task<TextExtractionResult> ExtractAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(document.ContentType, DocxMime, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(document.StoredFileName), ".docx",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.UnsupportedFormat,
                "The DOCX text extractor supports DOCX documents only.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await using var stream = await fileStorage.OpenReadAsync(
                document.StoredFileName, cancellationToken);
            using var word = WordprocessingDocument.Open(stream, false);
            var body = word.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                throw new TextExtractionException(
                    TextExtractionFailureReason.InvalidDocument,
                    "Не удалось прочитать DOCX: отсутствует основное содержимое документа.");
            }

            var extractedText = ExtractedTextPolicy.EnsureUsable(
                string.Join(Environment.NewLine, ReadBlocks(body.ChildElements, cancellationToken)),
                TextExtractionFailureReason.NoTextLayer,
                "DOCX не содержит извлекаемого текста. Текст внутри встроенных изображений не распознаётся.");
            return new TextExtractionResult(extractedText, TextExtractionMethod.DocxText);
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
                "Не удалось прочитать DOCX: файл повреждён или имеет неверный формат.",
                exception);
        }
    }

    private static IEnumerable<string> ReadBlocks(
        IEnumerable<OpenXmlElement> blocks,
        CancellationToken cancellationToken)
    {
        foreach (var block in blocks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (block)
            {
                case Paragraph paragraph:
                {
                    var text = ReadParagraph(paragraph);
                    if (!string.IsNullOrWhiteSpace(text))
                        yield return text;
                    break;
                }
                case Table table:
                    foreach (var row in table.Elements<TableRow>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var cells = row.Elements<TableCell>()
                            .Select(cell => string.Join(" / ", ReadBlocks(cell.ChildElements, cancellationToken)))
                            .Where(text => !string.IsNullOrWhiteSpace(text));
                        var line = string.Join(" | ", cells);
                        if (!string.IsNullOrWhiteSpace(line))
                            yield return line;
                    }
                    break;
            }
        }
    }

    private static string ReadParagraph(Paragraph paragraph)
    {
        var text = new StringBuilder();
        foreach (var element in paragraph.Descendants())
        {
            switch (element)
            {
                case Text runText:
                    text.Append(runText.Text.Replace('\u00A0', ' '));
                    break;
                case TabChar:
                    text.Append('\t');
                    break;
                case Break or CarriageReturn:
                    text.Append(Environment.NewLine);
                    break;
            }
        }
        return text.ToString().Trim();
    }
}
