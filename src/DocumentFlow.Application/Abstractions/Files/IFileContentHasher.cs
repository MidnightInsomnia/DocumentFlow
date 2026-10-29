namespace DocumentFlow.Application.Abstractions.Files;

public interface IFileContentHasher
{
    // Hashes bytes from the current position. Seekable streams are restored afterwards;
    // non-seekable streams are consumed and must be reopened for later use.
    Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken);
}
