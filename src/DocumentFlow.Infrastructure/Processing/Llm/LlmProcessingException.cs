namespace DocumentFlow.Infrastructure.Processing.Llm;

public sealed class LlmProcessingException : Exception
{
    public LlmProcessingException(string message) : base(message) { }
    public LlmProcessingException(string message, Exception innerException) : base(message, innerException) { }
}
