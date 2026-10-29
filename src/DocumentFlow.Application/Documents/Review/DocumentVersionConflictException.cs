namespace DocumentFlow.Application.Documents.Review;

public sealed class DocumentVersionConflictException(Guid documentId, Exception? innerException = null)
    : Exception($"Документ {documentId} изменён другим пользователем. Обновите документ; ваш черновик сохранён.", innerException);
