using System.Diagnostics;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Evaluation;

public sealed class EvaluationRunner(
    IDocumentProcessingPipeline pipeline,
    EvaluationFileStorage storage,
    bool ocrAvailable,
    string? ocrUnavailableReason)
{
    public async Task WarmUpAsync(EvaluationManifest manifest, CancellationToken cancellationToken)
    {
        var warmUpItems = new[]
        {
            manifest.Documents.FirstOrDefault(item => item.SourceKind == EvaluationSourceKind.TextPdf),
            ocrAvailable
                ? manifest.Documents.FirstOrDefault(item => item.SourceKind != EvaluationSourceKind.TextPdf)
                : null
        }.OfType<EvaluationManifestDocument>();

        foreach (var item in warmUpItems)
        {
            var path = storage.ResolvePath(item.File);
            var document = new Document(
                Path.GetFileName(item.File),
                item.File.Replace('\\', '/'),
                item.ContentType,
                new FileInfo(path).Length);
            document.StartProcessing();
            try
            {
                await pipeline.ProcessAsync(document, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Warm-up is intentionally excluded from results. A document-level OCR failure
                // will still be recorded by the measured corpus run below.
            }
        }
    }

    public async Task<IReadOnlyCollection<EvaluationDocumentResult>> RunAsync(
        EvaluationManifest manifest,
        CancellationToken cancellationToken)
    {
        var results = new List<EvaluationDocumentResult>();
        foreach (var item in manifest.Documents)
        {
            if (item.SourceKind != EvaluationSourceKind.TextPdf && !ocrAvailable)
            {
                results.Add(Skipped(item, ocrUnavailableReason ?? "Tesseract OCR is unavailable."));
                continue;
            }

            var path = storage.ResolvePath(item.File);
            var document = new Document(
                Path.GetFileName(item.File),
                item.File.Replace('\\', '/'),
                item.ContentType,
                new FileInfo(path).Length);
            document.StartProcessing();
            var stopwatch = Stopwatch.StartNew();
            string? error = null;
            try
            {
                await pipeline.ProcessAsync(document, cancellationToken);
                if (document.Status == DocumentStatus.Processing) document.CompleteProcessing();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                error = exception.Message;
                if (document.Status == DocumentStatus.Processing) document.FailProcessing(exception.Message);
            }
            finally
            {
                stopwatch.Stop();
            }

            var actualFields = document.Fields.ToDictionary(
                field => field.Name,
                field => field.Value ?? "",
                StringComparer.Ordinal);
            var fieldResults = EvaluationMetricsCalculator.CompareFields(item.ExpectedFields, actualFields);
            results.Add(new EvaluationDocumentResult(
                item.Id,
                item.SourceKind,
                item.Difficulty,
                item.TemplateId,
                item.ExpectedType,
                document.Type,
                item.ExpectedStatus,
                document.Status,
                item.ExpectedDepartment,
                document.TargetDepartment,
                item.ExpectedFields,
                actualFields,
                item.ExpectedType == document.Type,
                fieldResults,
                EvaluationMetricsCalculator.IsExactMatch(
                    item, document.Type, document.Status, document.TargetDepartment, actualFields),
                stopwatch.ElapsedMilliseconds,
                error));
        }

        return results;
    }

    private static EvaluationDocumentResult Skipped(EvaluationManifestDocument item, string reason) => new(
        item.Id, item.SourceKind, item.Difficulty, item.TemplateId, item.ExpectedType, DocumentType.Unknown,
        item.ExpectedStatus, DocumentStatus.PendingProcessing,
        item.ExpectedDepartment, Department.Unassigned,
        item.ExpectedFields, new Dictionary<string, string>(), false, [], false, 0, null, true, reason);
}
