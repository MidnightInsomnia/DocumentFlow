namespace DocumentFlow.Application.Abstractions.Processing;

public interface IOcrEngine
{
    Task<string> RecognizeAsync(
        Stream image,
        CancellationToken cancellationToken);
}
