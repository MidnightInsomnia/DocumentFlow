namespace DocumentFlow.Application.Abstractions.Files;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string storedFileName,
        CancellationToken cancellationToken);
}
