namespace DocumentFlow.Infrastructure.Processing.Ocr;

public sealed class TesseractOptions
{
    public const string SectionName = "Tesseract";

    public string ExecutablePath { get; init; } = "tesseract";
    public string? TessdataPath { get; init; }
    public string Languages { get; init; } = "rus+eng";
    public int PageSegmentationMode { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 60;
}
