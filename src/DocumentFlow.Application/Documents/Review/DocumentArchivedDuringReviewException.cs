namespace DocumentFlow.Application.Documents.Review;

public sealed class DocumentArchivedDuringReviewException(Guid documentId, Exception innerException)
    : Exception($"Document {documentId} was archived during manual review.", innerException);
