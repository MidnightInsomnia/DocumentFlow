namespace DocumentFlow.Infrastructure.Processing.Llm;

public sealed class AiReviewOptions
{
    public const string SectionName = "AiReview";
    public bool Enabled { get; init; }
}
