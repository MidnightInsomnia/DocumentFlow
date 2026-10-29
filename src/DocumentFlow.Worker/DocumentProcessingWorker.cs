using DocumentFlow.Application.Documents.Processing;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Worker;

public sealed class DocumentProcessingWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DocumentProcessingWorkerOptions> options,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    private readonly TimeSpan _pollInterval =
        TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
    private readonly TimeSpan _failureRetryDelay =
        TimeSpan.FromSeconds(options.Value.FailureRetryDelaySeconds);
    private readonly TimeSpan _staleProcessingTimeout =
        TimeSpan.FromMinutes(options.Value.StaleProcessingTimeoutMinutes);
    private readonly TimeSpan _recoveryCheckInterval =
        TimeSpan.FromSeconds(options.Value.RecoveryCheckIntervalSeconds);
    private readonly int _recoveryBatchSize = options.Value.RecoveryBatchSize;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document processing worker started.");

        try
        {
            var nextRecoveryUtc = DateTime.MinValue;
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= nextRecoveryUtc)
                {
                    await RecoverStaleProcessingAsync(stoppingToken);
                    nextRecoveryUtc = DateTime.UtcNow + _recoveryCheckInterval;
                }

                try
                {
                    var processed = await ProcessOneAsync(stoppingToken);
                    if (!processed)
                    {
                        await Task.Delay(_pollInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Document processing attempt failed.");

                    try
                    {
                        await Task.Delay(_failureRetryDelay, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            logger.LogInformation("Document processing worker stopped.");
        }
    }

    private async Task RecoverStaleProcessingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var recoveryService = scope.ServiceProvider
                .GetRequiredService<DocumentProcessingRecoveryService>();
            var recovered = await recoveryService.RecoverAsync(
                DateTime.UtcNow,
                _staleProcessingTimeout,
                _recoveryBatchSize,
                cancellationToken);

            if (recovered > 0)
            {
                logger.LogInformation(
                    "Recovered {Count} stale processing documents as Failed.",
                    recovered);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Stale document recovery attempt failed.");
        }
    }

    private async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var processingService = scope.ServiceProvider
            .GetRequiredService<DocumentProcessingService>();

        return await processingService.ProcessNextAsync(cancellationToken);
    }
}
