using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFlow.Application;
using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Evaluation;
using DocumentFlow.Infrastructure.Processing.Ocr;
using DocumentFlow.Infrastructure.Processing.Llm;
using DocumentFlow.Infrastructure.Processing.TextExtraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

try
{
    var options = EvaluationStrategyConfiguration.ParseArguments(args);
    var corpusPath = Path.GetFullPath(options.Corpus);
    var manifestPath = Path.Combine(corpusPath, "manifest.json");
    if (!File.Exists(manifestPath)) throw new InvalidOperationException($"Corpus manifest was not found: {manifestPath}");

    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    var manifest = JsonSerializer.Deserialize<EvaluationManifest>(
        await File.ReadAllTextAsync(manifestPath), jsonOptions)
        ?? throw new InvalidOperationException("Corpus manifest is empty.");
    var storage = new EvaluationFileStorage(corpusPath);
    var errors = EvaluationManifestValidator.Validate(manifest, storage);
    if (errors.Count > 0)
        throw new InvalidOperationException("Invalid corpus manifest:\n- " + string.Join("\n- ", errors));
    PrintCorpusSummary(manifest);
    PrintDuplicateHashWarnings(manifest, storage);
    if (string.Equals(Path.GetFileName(corpusPath), "corpus-v2-holdout", StringComparison.OrdinalIgnoreCase))
    {
        var duplicateErrors = EvaluationCorpusHashValidator.FindDuplicateHashes(manifest, storage);
        var referenceFiles = Path.Combine(Directory.GetParent(corpusPath)!.FullName, "corpus-v1", "files");
        var overlapErrors = EvaluationCorpusHashValidator.FindCrossCorpusOverlaps(manifest, storage, referenceFiles);
        var hashErrors = duplicateErrors.Concat(overlapErrors).ToArray();
        if (hashErrors.Length > 0)
            throw new InvalidOperationException("Holdout corpus hash validation failed:\n- " + string.Join("\n- ", hashErrors));
        Console.WriteLine("Holdout hash validation: no internal duplicates or corpus-v1 overlaps.\n");
    }

    var tesseractPath = Environment.GetEnvironmentVariable("Tesseract__ExecutablePath") ?? "tesseract";
    var tesseractLanguages = Environment.GetEnvironmentVariable("Tesseract__Languages") ?? "rus+eng";
    var ocr = CheckTesseract(tesseractPath, tesseractLanguages);
    if (EvaluationStrategyConfiguration.RequiresOllama(options.Strategy) && !ocr.Available)
        throw new InvalidOperationException($"Tesseract rus+eng is required for the full LLM evaluation: {ocr.Reason}");

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddApplication();
    services.AddSingleton<IFileStorage>(storage);
    services.AddSingleton<IOptions<TesseractOptions>>(Options.Create(new TesseractOptions
    {
        ExecutablePath = tesseractPath,
        Languages = tesseractLanguages
    }));
    services.AddScoped<IOcrEngine, TesseractOcrEngine>();
    services.AddScoped<PdfTextExtractor>();
    services.AddScoped<IPdfTextExtractor>(provider => provider.GetRequiredService<PdfTextExtractor>());
    services.AddScoped<PdfOcrTextExtractor>();
    services.AddScoped<IPdfOcrTextExtractor>(provider => provider.GetRequiredService<PdfOcrTextExtractor>());
    services.AddScoped<PdfHybridTextExtractor>();
    services.AddScoped<ImageOcrTextExtractor>();
    services.AddScoped<ITextExtractorResolver, TextExtractorResolver>();
    OllamaOptions? ollamaOptions = null;
    if (EvaluationStrategyConfiguration.RequiresOllama(options.Strategy))
    {
        ollamaOptions = new OllamaOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("Ollama__BaseUrl") ?? "http://localhost:11434",
            Model = Environment.GetEnvironmentVariable("Ollama__Model") ?? "qwen2.5:7b-instruct",
            TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("Ollama__TimeoutSeconds"), out var timeoutSeconds)
                ? timeoutSeconds
                : 120
        };
    }
    EvaluationStrategyConfiguration.Configure(services, options.Strategy, ollamaOptions);
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    if (EvaluationStrategyConfiguration.RequiresOllama(options.Strategy))
    {
        var identity = await scope.ServiceProvider.GetRequiredService<IOllamaClient>()
            .GetModelIdentityAsync(CancellationToken.None);
        Console.WriteLine($"Ollama version: {identity.OllamaVersion}");
        Console.WriteLine($"Model: {identity.Model}");
        Console.WriteLine($"Model digest: {identity.Digest ?? "unavailable"}");
        Console.WriteLine($"Model size: {(identity.Size?.ToString() ?? "unavailable")} bytes\n");
    }
    var runner = new EvaluationRunner(
        scope.ServiceProvider.GetRequiredService<IDocumentProcessingPipeline>(),
        storage,
        ocr.Available,
        ocr.Reason);
    await runner.WarmUpAsync(manifest, CancellationToken.None);
    var results = await runner.RunAsync(manifest, CancellationToken.None);
    var summary = EvaluationMetricsCalculator.Calculate(results, DateTime.UtcNow);
    var reportDirectory = Path.Combine(
        Path.GetFullPath(options.Output),
        DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
    await EvaluationReportWriter.WriteAsync(summary, reportDirectory, CancellationToken.None);

    Console.WriteLine("DocumentFlow Evaluation\n");
    Console.WriteLine($"Corpus: {summary.CorpusSize}");
    Console.WriteLine($"Executed: {summary.Executed}");
    Console.WriteLine($"Skipped: {summary.Skipped}\n");
    Console.WriteLine($"Classification accuracy: {summary.Overall.ClassificationAccuracyPercent}%");
    Console.WriteLine($"Field accuracy: {summary.Overall.FieldAccuracyPercent}%");
    Console.WriteLine($"Exact document match: {summary.Overall.ExactMatchPercent}%\n");
    Console.WriteLine($"Completed: {summary.CompletedCount}");
    Console.WriteLine($"NeedsReview: {summary.NeedsReviewCount}");
    Console.WriteLine($"Failed: {summary.FailedCount}\n");
    Console.WriteLine($"Average: {summary.Overall.Timing.AverageMs} ms");
    Console.WriteLine($"Median: {summary.Overall.Timing.MedianMs} ms");
    Console.WriteLine($"P95: {summary.Overall.Timing.P95Ms} ms\n");
    Console.WriteLine($"Report:\n{reportDirectory}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"DocumentFlow Evaluation failed: {exception.Message}");
    return 1;
}

static (bool Available, string? Reason) CheckTesseract(string executable, string languages)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo(executable, "--list-langs")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });
        if (process is null) return (false, "Tesseract could not be started.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        if (process.ExitCode != 0) return (false, "Tesseract language check failed.");
        var available = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = languages.Split('+', StringSplitOptions.RemoveEmptyEntries).Where(language => !available.Contains(language)).ToArray();
        return missing.Length == 0 ? (true, null) : (false, $"Tesseract languages are unavailable: {string.Join(", ", missing)}.");
    }
    catch
    {
        return (false, "Tesseract OCR is unavailable.");
    }
}

static void PrintCorpusSummary(EvaluationManifest manifest)
{
    Console.WriteLine("Corpus validation summary");
    Console.WriteLine($"Total documents: {manifest.Documents.Count}");
    static string Counts<T>(IEnumerable<T> values) where T : notnull =>
        string.Join(", ", values.GroupBy(value => value).OrderBy(group => group.Key).Select(group => $"{group.Key}={group.Count()}"));
    Console.WriteLine($"By type: {Counts(manifest.Documents.Select(item => item.ExpectedType))}");
    Console.WriteLine($"By source: {Counts(manifest.Documents.Select(item => item.SourceKind))}");
    Console.WriteLine($"By difficulty: {Counts(manifest.Documents.Select(item => item.Difficulty))}");
    Console.WriteLine($"By expected status: {Counts(manifest.Documents.Select(item => item.ExpectedStatus))}");
    Console.WriteLine($"Templates: {manifest.Documents.Select(item => item.TemplateId).Distinct(StringComparer.OrdinalIgnoreCase).Count()}\n");
}

static void PrintDuplicateHashWarnings(EvaluationManifest manifest, EvaluationFileStorage storage)
{
    var duplicates = manifest.Documents
        .Select(item => new
        {
            item.Id,
            Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(storage.ResolvePath(item.File))))
        })
        .GroupBy(item => item.Hash)
        .Where(group => group.Count() > 1)
        .ToArray();
    foreach (var duplicate in duplicates)
        Console.WriteLine($"WARNING: duplicate file hash for ids: {string.Join(", ", duplicate.Select(item => item.Id))}");
}
