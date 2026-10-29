using System.ComponentModel;
using System.Diagnostics;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Infrastructure.Health;
using DocumentFlow.Infrastructure.Processing.TextExtraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Infrastructure.Processing.Ocr;

public sealed class TesseractOcrEngine(IOptions<TesseractOptions> options, ILogger<TesseractOcrEngine>? logger = null) : IOcrEngine
{
    private readonly TesseractOptions _options = options.Value;
    private readonly ILogger<TesseractOcrEngine> _logger = logger ?? NullLogger<TesseractOcrEngine>.Instance;

    public async Task<string> RecognizeAsync(Stream image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLanguagesAvailableAsync(cancellationToken);
        using var process = new Process { StartInfo = TesseractProcessConfiguration.Create(_options, redirectInput: true) };
        process.StartInfo.ArgumentList.Add("stdin");
        process.StartInfo.ArgumentList.Add("stdout");
        process.StartInfo.ArgumentList.Add("-l");
        process.StartInfo.ArgumentList.Add(_options.Languages);
        process.StartInfo.ArgumentList.Add("--psm");
        process.StartInfo.ArgumentList.Add(_options.PageSegmentationMode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        try { process.Start(); }
        catch (Win32Exception ex) { throw Unavailable(ex); }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var stdout = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var stderr = process.StandardError.ReadToEndAsync(deadline.Token);
        try
        {
            var inputFailed = false;
            try { await image.CopyToAsync(process.StandardInput.BaseStream, deadline.Token); }
            catch (IOException) { inputFailed = true; } // An early native exit can close stdin. Still collect its diagnostics.
            try { process.StandardInput.Close(); }
            catch (IOException) { inputFailed = true; }
            await process.WaitForExitAsync(deadline.Token);
            var output = await stdout;
            var error = await stderr;
            var languageFailure = TesseractProcessConfiguration.LanguageLoadingFailed(error);
            if (process.ExitCode != 0 || languageFailure || inputFailed)
            {
                // stderr can contain paths or document content. Log bounded metadata, never its raw contents.
                _logger.LogWarning("Tesseract OCR exited {ExitCode}; languageFailure={LanguageFailure}; stderrCharacters={Length}",
                    process.ExitCode, languageFailure, error.Length);
                throw Failure(languageFailure
                    ? "[ocr_missing_languages] Tesseract не загрузил языковые данные. Проверьте rus/eng.traineddata и Tesseract:TessdataPath."
                    : $"[ocr_process_failed] Tesseract завершился с кодом {process.ExitCode}. Проверьте формат и целостность изображения.");
            }
            return ExtractedTextPolicy.EnsureUsable(output, TextExtractionFailureReason.OcrFailed,
                "[ocr_no_text] OCR не получил пригодный текст. Проверьте качество и ориентацию изображения.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex) { throw Failure($"[ocr_timeout] OCR превысил лимит {_options.TimeoutSeconds} с. Проверьте размер изображения или Tesseract:TimeoutSeconds.", ex); }
        catch (TextExtractionException) { throw; }
        catch (Exception ex) { throw Failure("[ocr_process_failed] Ошибка обмена данными с Tesseract.", ex); }
        finally { TesseractProcessConfiguration.Kill(process); }
    }

    private async Task EnsureLanguagesAvailableAsync(CancellationToken ct)
    {
        var seconds = Math.Min(_options.TimeoutSeconds, 5);
        try
        {
            var output = await new ProcessTesseractLanguageLister().ListAsync(_options,
                TimeSpan.FromSeconds(seconds), ct);
            TesseractLanguagePreflight.EnsureAvailable(_options.Languages, TesseractLanguagePreflight.ParseAvailableLanguages(output));
        }
        catch (Win32Exception ex) { throw Unavailable(ex); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex) { throw Failure($"[ocr_timeout] Проверка языков Tesseract не завершилась за {seconds} с.", ex); }
        catch (TesseractProcessException ex)
        {
            _logger.LogWarning("Tesseract preflight failed: {Code}; exit={ExitCode}", ex.Code, ex.ExitCode);
            throw Failure(ex.Code == "ocr_missing_languages"
                ? "[ocr_missing_languages] Tesseract не загрузил языковые данные. Проверьте Tesseract:TessdataPath."
                : $"[ocr_process_failed] Проверка языков Tesseract завершилась с кодом {ex.ExitCode}.", ex);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        { throw Failure("[ocr_process_failed] Ошибка проверки языков Tesseract. Проверьте executable и каталог языковых данных.", ex); }
    }

    private static TextExtractionException Unavailable(Exception ex) => Failure(
        "[ocr_executable_unavailable] Tesseract не найден или не запускается. Установите движок и rus/eng.traineddata; укажите Tesseract:ExecutablePath. Системный PATH менять не требуется.", ex);
    private static TextExtractionException Failure(string message, Exception? ex = null) =>
        ex is null ? new(TextExtractionFailureReason.OcrFailed, message) : new(TextExtractionFailureReason.OcrFailed, message, ex);
}
