using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Infrastructure.Processing.Llm;

public sealed class OllamaClient(HttpClient httpClient, IOptions<OllamaOptions> options) : IOllamaClient
{
    private readonly OllamaOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GenerateJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        JsonElement schema)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/chat", new
            {
                model = _options.Model,
                stream = false,
                format = schema,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                options = new { temperature = _options.Temperature, seed = _options.Seed }
            }, JsonOptions, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new LlmProcessingException($"Ollama returned HTTP {(int)response.StatusCode}.");
            var body = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, timeout.Token);
            if (string.IsNullOrWhiteSpace(body?.Message?.Content))
                throw new LlmProcessingException("Ollama returned an empty structured response.");
            return body.Message.Content;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new LlmProcessingException($"Ollama request exceeded the {_options.TimeoutSeconds}-second timeout.");
        }
        catch (HttpRequestException exception)
        {
            throw new LlmProcessingException("Ollama is unavailable.", exception);
        }
        catch (JsonException exception)
        {
            throw new LlmProcessingException("Ollama returned invalid response JSON.", exception);
        }
    }

    public async Task<OllamaModelIdentity> GetModelIdentityAsync(CancellationToken cancellationToken)
    {
        try
        {
            var version = await httpClient.GetFromJsonAsync<VersionResponse>("api/version", JsonOptions, cancellationToken)
                          ?? throw new LlmProcessingException("Ollama version response is empty.");
            var tags = await httpClient.GetFromJsonAsync<TagsResponse>("api/tags", JsonOptions, cancellationToken)
                       ?? throw new LlmProcessingException("Ollama model list response is empty.");
            var model = tags.Models.FirstOrDefault(item =>
                string.Equals(item.Name, _options.Model, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Model, _options.Model, StringComparison.OrdinalIgnoreCase));
            if (model is null)
                throw new LlmProcessingException(
                    $"Ollama model '{_options.Model}' is not installed. Run: ollama pull {_options.Model}");
            return new OllamaModelIdentity(version.Version, model.Name ?? model.Model ?? _options.Model, model.Digest, model.Size);
        }
        catch (LlmProcessingException) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new LlmProcessingException("Ollama is unavailable.", exception);
        }
    }

    private sealed record ChatResponse(ChatMessage? Message);
    private sealed record ChatMessage(string? Content);
    private sealed record VersionResponse(string Version);
    private sealed record TagsResponse(ModelResponse[] Models);
    private sealed record ModelResponse(string? Name, string? Model, string? Digest, long? Size);
}
