using DocumentFlow.Application.Abstractions.Files;
using Microsoft.Extensions.Configuration;

namespace DocumentFlow.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configuredRootPath = configuration["DocumentStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredRootPath))
        {
            throw new InvalidOperationException(
                "Document storage root path is not configured. " +
                "Set DocumentStorage__RootPath or DocumentStorage:RootPath.");
        }

        _rootPath = Path.GetFullPath(configuredRootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(Path.GetFileName(originalFileName))
            .ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = GetStoragePath(storedFileName);

        try
        {
            await using var destination = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await content.CopyToAsync(destination, cancellationToken);
            return storedFileName;
        }
        catch
        {
            try
            {
                File.Delete(destinationPath);
            }
            catch
            {
                // Cleanup failure must not hide the original exception.
            }

            throw;
        }
    }

    public Task DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetStoragePath(storedFileName);
        File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(
        string storedFileName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetStoragePath(storedFileName);

        Stream stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(stream);
    }

    private string GetStoragePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            storedFileName.Contains('/') ||
            storedFileName.Contains('\\') ||
            !string.Equals(
                Path.GetFileName(storedFileName),
                storedFileName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Stored file name must be a simple file name.",
                nameof(storedFileName));
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, storedFileName));
        var expectedPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Stored file path must remain inside the storage directory.",
                nameof(storedFileName));
        }

        return path;
    }
}
