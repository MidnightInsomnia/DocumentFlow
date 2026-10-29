using System.ComponentModel;
using System.Diagnostics;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Infrastructure.Processing.Ocr;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Infrastructure.Health;

public enum OcrHealthStatus { Healthy, MissingLanguages, Unavailable, Timeout, Failed }

public sealed record OcrHealthResult(
    OcrHealthStatus Status,
    IReadOnlyList<string> AvailableLanguages,
    IReadOnlyList<string> MissingLanguages,
    string? ErrorCode = null);

/// <summary>Lists languages without processing a document or an image.</summary>
public interface ITesseractLanguageLister
{
    Task<string> ListAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken);
    Task<string> ListAsync(TesseractOptions options, TimeSpan timeout, CancellationToken cancellationToken) =>
        ListAsync(options.ExecutablePath, timeout, cancellationToken);
}

public sealed class TesseractProcessException(string code, int exitCode) : Exception(code)
{
    public string Code { get; } = code;
    public int ExitCode { get; } = exitCode;
}

public sealed class ProcessTesseractLanguageLister : ITesseractLanguageLister
{
    public Task<string> ListAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken) =>
        ListAsync(new TesseractOptions { ExecutablePath = executablePath }, timeout, cancellationToken);

    public async Task<string> ListAsync(TesseractOptions options, TimeSpan timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process { StartInfo = TesseractProcessConfiguration.Create(options) };
        process.StartInfo.ArgumentList.Add("--list-langs");
        process.Start();
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            var stdout = process.StandardOutput.ReadToEndAsync(deadline.Token);
            var stderr = process.StandardError.ReadToEndAsync(deadline.Token);
            await process.WaitForExitAsync(deadline.Token);
            var output = await stdout;
            var error = await stderr;
            if (TesseractProcessConfiguration.LanguageLoadingFailed(error))
                throw new TesseractProcessException("ocr_missing_languages", process.ExitCode);
            if (process.ExitCode != 0) throw new TesseractProcessException("ocr_process_failed", process.ExitCode);
            // Some builds list languages on stderr. Parse only identifiers, not paths/errors.
            return output + "\n" + error;
        }
        finally { TesseractProcessConfiguration.Kill(process); }
    }
}

public sealed class OcrHealthProbe(IOptions<TesseractOptions> options, ITesseractLanguageLister lister)
{
    public async Task<OcrHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        try
        {
            var output = await lister.ListAsync(configured,
                TimeSpan.FromSeconds(Math.Clamp(configured.TimeoutSeconds, 1, 5)), cancellationToken);
            var available = TesseractLanguagePreflight.ParseAvailableLanguages(output);
            var required = configured.Languages.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var missing = required.Where(language => !available.Contains(language, StringComparer.OrdinalIgnoreCase))
                .Where(TesseractLanguagePreflight.IsLanguageIdentifier).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            try { TesseractLanguagePreflight.EnsureAvailable(configured.Languages, available); }
            catch (TextExtractionException) { return new(OcrHealthStatus.MissingLanguages, available, missing, "ocr_missing_languages"); }
            return new(OcrHealthStatus.Healthy, available, []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return new(OcrHealthStatus.Timeout, [], [], "ocr_timeout"); }
        catch (Win32Exception) { return new(OcrHealthStatus.Unavailable, [], [], "ocr_executable_unavailable"); }
        catch (TesseractProcessException ex)
        {
            return new(ex.Code == "ocr_missing_languages" ? OcrHealthStatus.MissingLanguages : OcrHealthStatus.Failed, [], [], ex.Code);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException)
        {
            return new(OcrHealthStatus.Failed, [], [], "ocr_process_failed");
        }
    }
}
