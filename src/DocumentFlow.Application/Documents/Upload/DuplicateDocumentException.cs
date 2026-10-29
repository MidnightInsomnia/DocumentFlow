namespace DocumentFlow.Application.Documents.Upload;

public sealed class DuplicateDocumentException(
    Guid existingDocumentId,
    string existingFileName,
    string contentHash,
    bool existingIsArchived = false)
    : Exception("Этот файл уже был загружен.")
{
    public Guid ExistingDocumentId { get; } = existingDocumentId;
    public string ExistingFileName { get; } = existingFileName;
    public string ContentHash { get; } = contentHash;
    public bool ExistingIsArchived { get; } = existingIsArchived;
}
