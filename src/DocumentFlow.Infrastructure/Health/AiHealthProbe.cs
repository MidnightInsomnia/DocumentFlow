using System.Text.Json;
using DocumentFlow.Infrastructure.Processing.Llm;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Infrastructure.Health;

public enum AiHealthStatus { Disabled, Available, ModelUnavailable, Unavailable }

public sealed record AiHealthResult(AiHealthStatus Status);

/// <summary>Read-only Ollama connectivity and model probe; never calls generation endpoints.</summary>
public sealed class AiHealthProbe(HttpClient httpClient, IOptions<AiReviewOptions> aiOptions,
    IOptions<OllamaOptions> ollamaOptions)
{
    public async Task<AiHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!aiOptions.Value.Enabled) return new AiHealthResult(AiHealthStatus.Disabled);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(ollamaOptions.Value.TimeoutSeconds, 1, 5)));
        try
        {
            var baseUri = new Uri(ollamaOptions.Value.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            using var version = await httpClient.GetAsync(new Uri(baseUri, "api/version"), deadline.Token);
            if (!version.IsSuccessStatusCode) return new AiHealthResult(AiHealthStatus.Unavailable);
            using var versionJson = JsonDocument.Parse(await version.Content.ReadAsStringAsync(deadline.Token));
            if (!versionJson.RootElement.TryGetProperty("version", out var versionValue) ||
                versionValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(versionValue.GetString()))
                return new AiHealthResult(AiHealthStatus.Unavailable);

            using var tags = await httpClient.GetAsync(new Uri(baseUri, "api/tags"), deadline.Token);
            if (!tags.IsSuccessStatusCode) return new AiHealthResult(AiHealthStatus.Unavailable);
            using var tagsJson = JsonDocument.Parse(await tags.Content.ReadAsStringAsync(deadline.Token));
            if (!tagsJson.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                return new AiHealthResult(AiHealthStatus.Unavailable);
            var configuredModel = ollamaOptions.Value.Model;
            var found = models.EnumerateArray().Any(model =>
                Matches(model, "name", configuredModel) || Matches(model, "model", configuredModel));
            return new AiHealthResult(found ? AiHealthStatus.Available : AiHealthStatus.ModelUnavailable);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or UriFormatException or InvalidOperationException)
        {
            return new AiHealthResult(AiHealthStatus.Unavailable);
        }
    }

    private static bool Matches(JsonElement model, string property, string expected) =>
        model.ValueKind == JsonValueKind.Object && model.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase);
}
