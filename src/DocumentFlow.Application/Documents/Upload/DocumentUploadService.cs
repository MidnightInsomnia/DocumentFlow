using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentFlow.Application.Documents.Upload;

public sealed class DocumentUploadService(
    IFileStorage fileStorage,
    IDocumentRepository documentRepository,
    IFileContentHasher contentHasher,
    ILogger<DocumentUploadService>? logger = null,
    ICurrentUserContext? currentUser = null)
{
    private readonly ILogger<DocumentUploadService> _logger =
        logger ?? NullLogger<DocumentUploadService>.Instance;

    public const long MaximumFileSizeBytes = 20L * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedFileTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff"
        };

    public async Task<UploadDocumentResult> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var originalFileName = ValidateAndNormalize(command);
        // Form-file streams are rewindable; never persist an exhausted non-seekable stream.
        if (!command.Content.CanSeek)
            throw new DocumentUploadValidationException("File content stream must support seeking.");
        command.Content.Position = 0;
        var contentHash = await contentHasher.ComputeSha256Async(command.Content, cancellationToken);
        command.Content.Position = 0;
        var existing = await documentRepository.FindByContentHashAsync(contentHash, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Duplicate upload rejected. UserId={UserId}, ExistingDocumentId={ExistingDocumentId}.",
                currentUser?.UserId, existing.Id);
            throw new DuplicateDocumentException(existing.Id, existing.OriginalFileName, contentHash, existing.IsArchived);
        }

        var storedFileName = await fileStorage.SaveAsync(
            command.Content,
            originalFileName,
            cancellationToken);

        try
        {
            var document = new Document(
                originalFileName,
                storedFileName,
                NormalizeContentType(command.ContentType),
                command.FileSizeBytes);

            document.SetContentHash(contentHash);
            document.Events.Add(new DocumentEvent(
                document.Id,
                DocumentEventType.DocumentUploaded,
                $"Document '{originalFileName}' uploaded.",
                currentUser?.UserId,
                currentUser?.DisplayName));

            await documentRepository.AddAsync(document, cancellationToken);

            _logger.LogInformation(
                "Document {DocumentId} uploaded. ContentType: {ContentType}, SizeBytes: {SizeBytes}.",
                document.Id,
                document.ContentType,
                document.FileSizeBytes);

            return new UploadDocumentResult(
                document.Id,
                document.OriginalFileName,
                document.Status,
                document.CreatedAtUtc);
        }
        catch (Exception exception)
        {
            try
            {
                await fileStorage.DeleteAsync(storedFileName, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                _logger.LogWarning(cleanupException,
                    "Could not delete stored file for rejected upload. StoredFileName={StoredFileName}.",
                    storedFileName);
            }

            if (exception is DuplicateDocumentException duplicate)
                _logger.LogInformation(
                    "Duplicate upload rejected after insert race. UserId={UserId}, ExistingDocumentId={ExistingDocumentId}.",
                    currentUser?.UserId, duplicate.ExistingDocumentId);
            else
                _logger.LogError(exception, "Document upload failed after storing the original file.");
            throw;
        }
    }

    private static string ValidateAndNormalize(UploadDocumentCommand command)
    {
        if (command.Content is null)
        {
            throw new DocumentUploadValidationException("File content is required.");
        }

        if (string.IsNullOrWhiteSpace(command.OriginalFileName))
        {
            throw new DocumentUploadValidationException("File name is required.");
        }

        if (command.FileSizeBytes <= 0)
        {
            throw new DocumentUploadValidationException("The file must not be empty.");
        }

        if (command.FileSizeBytes > MaximumFileSizeBytes)
        {
            throw new DocumentUploadValidationException("The file exceeds the 20 MiB limit.");
        }

        var originalFileName = Path.GetFileName(
            command.OriginalFileName.Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new DocumentUploadValidationException("File name is invalid.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!AllowedFileTypes.TryGetValue(extension, out var expectedContentType))
        {
            throw new DocumentUploadValidationException(
                $"File extension '{extension}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(command.ContentType) ||
            !AllowedFileTypes.Values.Contains(
                NormalizeContentType(command.ContentType),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new DocumentUploadValidationException(
                $"Content type '{command.ContentType}' is not supported.");
        }

        if (!string.Equals(
                expectedContentType,
                NormalizeContentType(command.ContentType),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentUploadValidationException(
                "File extension does not match its content type.");
        }

        return originalFileName;
    }

    private static string NormalizeContentType(string contentType) =>
        string.Equals(contentType, "image/tif", StringComparison.OrdinalIgnoreCase)
            ? "image/tiff"
            : contentType;
}
