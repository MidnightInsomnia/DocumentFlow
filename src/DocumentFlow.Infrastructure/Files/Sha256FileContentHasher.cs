using System.Security.Cryptography;
using DocumentFlow.Application.Abstractions.Files;

namespace DocumentFlow.Infrastructure.Files;

public sealed class Sha256FileContentHasher : IFileContentHasher
{
    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var originalPosition = stream.CanSeek ? stream.Position : (long?)null;
        try
        {
            var digest = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(digest).ToLowerInvariant();
        }
        finally
        {
            if (originalPosition.HasValue)
                stream.Position = originalPosition.Value;
        }
    }
}
