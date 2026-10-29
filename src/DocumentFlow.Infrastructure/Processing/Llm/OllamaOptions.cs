namespace DocumentFlow.Infrastructure.Processing.Llm;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "qwen2.5:7b-instruct";
    public int TimeoutSeconds { get; init; } = 120;
    public decimal Temperature { get; init; } = 0;
    public int Seed { get; init; } = 42;
}
