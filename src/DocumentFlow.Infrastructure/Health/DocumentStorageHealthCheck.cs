using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DocumentFlow.Infrastructure.Health;

public sealed class DocumentStorageHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private const string UnavailableDescription =
        "Document storage is unavailable or not writable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var configuredRootPath = configuration["DocumentStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return HealthCheckResult.Unhealthy(UnavailableDescription);
        }

        string? healthFilePath = null;
        try
        {
            var rootPath = Path.GetFullPath(configuredRootPath);
            Directory.CreateDirectory(rootPath);
            healthFilePath = Path.Combine(rootPath, $".health-{Guid.NewGuid():N}.tmp");

            await File.WriteAllBytesAsync(
                healthFilePath,
                [0x44, 0x46],
                cancellationToken);
            File.Delete(healthFilePath);
            healthFilePath = null;

            return HealthCheckResult.Healthy(
                "Document storage is available and writable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return HealthCheckResult.Unhealthy(UnavailableDescription);
        }
        finally
        {
            if (healthFilePath is not null)
            {
                try
                {
                    File.Delete(healthFilePath);
                }
                catch
                {
                    // Cleanup failure must not expose local filesystem details.
                }
            }
        }
    }
}
