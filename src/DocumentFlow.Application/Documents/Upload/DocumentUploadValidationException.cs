namespace DocumentFlow.Application.Documents.Upload;

public sealed class DocumentUploadValidationException(string message)
    : Exception(message);
