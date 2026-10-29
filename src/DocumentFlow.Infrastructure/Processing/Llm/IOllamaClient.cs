using System.Text.Json;

namespace DocumentFlow.Infrastructure.Processing.Llm;

public interface IOllamaClient
{
    Task<string> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken,
        JsonElement schema);
    Task<OllamaModelIdentity> GetModelIdentityAsync(CancellationToken cancellationToken);
}

public sealed record OllamaModelIdentity(string OllamaVersion, string Model, string? Digest, long? Size);
