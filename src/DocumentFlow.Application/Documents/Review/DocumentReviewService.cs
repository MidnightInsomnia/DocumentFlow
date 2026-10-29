using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing.Validation;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentFlow.Application.Documents.Review;

public sealed class DocumentReviewService(
    IDocumentReviewRepository repository,
    IDocumentValidator validator,
    IDocumentRouter router,
    ILogger<DocumentReviewService>? logger = null,
    ICurrentUserContext? currentUser = null)
{
    private readonly ILogger<DocumentReviewService> _logger =
        logger ?? NullLogger<DocumentReviewService>.Instance;

    public async Task<ReviewDocumentResult> ReviewAsync(
        Guid id,
        ReviewDocumentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Manual review started for document {DocumentId}.",
            id);

        var document = await repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return new ReviewDocumentResult(ReviewDocumentOutcome.NotFound);
        }

        if (command.ExpectedVersion is null || command.ExpectedVersion == Guid.Empty)
            return new ReviewDocumentResult(ReviewDocumentOutcome.InvalidRequest,
                ErrorMessage: "Необходимо передать expectedVersion из актуальной карточки документа.");
        if (document.Version != command.ExpectedVersion.Value)
            return new ReviewDocumentResult(ReviewDocumentOutcome.VersionConflict,
                document.Id, ErrorMessage: "Документ изменён. Обновите карточку; ваш черновик сохранён.");

        if (document.IsArchived || document.Status != DocumentStatus.NeedsReview)
        {
            _logger.LogWarning(
                "Manual review rejected for document {DocumentId}. Current status: {Status}.",
                document.Id,
                document.Status);

            return new ReviewDocumentResult(
                ReviewDocumentOutcome.NotReviewable,
                document.Id,
                document.Status,
                document.Type,
                document.TargetDepartment,
                ErrorMessage: document.IsArchived
                    ? "Сначала восстановите документ из архива."
                    : "Подтвердить можно только документ, ожидающий ручной проверки.");
        }

        var normalizedFields = NormalizeFields(command);
        if (normalizedFields.ErrorMessage is not null)
        {
            return new ReviewDocumentResult(
                ReviewDocumentOutcome.InvalidRequest,
                document.Id,
                document.Status,
                document.Type,
                document.TargetDepartment,
                ErrorMessage: normalizedFields.ErrorMessage);
        }

        var typeChanged = document.Type != command.Type;
        var priorFields = document.Fields.ToDictionary(field => field.Name,
            field => field.Value, StringComparer.OrdinalIgnoreCase);
        var changedNames = normalizedFields.Fields
            .Where(field => typeChanged || !priorFields.TryGetValue(field.Name, out var previous) ||
                !string.Equals(previous, field.Value?.Trim(), StringComparison.Ordinal))
            .Select(field => field.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (typeChanged)
        {
            changedNames.UnionWith(priorFields.Keys);
        }

        document.SetTypeManually(command.Type);
        if (typeChanged)
        {
            document.ClearFieldsForManualReview();
        }

        foreach (var field in normalizedFields.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Value))
            {
                document.RemoveFieldForManualReview(field.Name);
            }
            else
            {
                document.SetFieldManually(field.Name, field.Value);
            }
        }

        var validation = validator.Validate(document);
        if (!validation.IsValid)
        {
            _logger.LogInformation(
                "Manual review validation failed for document {DocumentId}. IssueCount: {IssueCount}.",
                document.Id,
                validation.Issues.Count);

            return new ReviewDocumentResult(
                ReviewDocumentOutcome.ValidationFailed,
                document.Id,
                document.Status,
                document.Type,
                document.TargetDepartment,
                validation.Issues);
        }

        var department = router.Route(document.Type);
        document.SetTargetDepartment(department);
        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.ManuallyReviewed,
            changedNames.Count == 0
                ? "Ручная проверка завершена."
                : $"Ручная проверка завершена. Изменены поля: {string.Join(", ", changedNames.Order(StringComparer.Ordinal))}.",
            currentUser?.UserId,
            currentUser?.DisplayName));
        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.DocumentRouted,
            $"Document routed to {department}."));
        document.CompleteProcessing();
        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.ProcessingCompleted,
            "Document processing completed."));

        try
        {
            await repository.PersistAsync(document, cancellationToken);
        }
        catch (DocumentVersionConflictException)
        {
            return new ReviewDocumentResult(ReviewDocumentOutcome.VersionConflict,
                document.Id, ErrorMessage: "Документ изменён. Обновите карточку; ваш черновик сохранён.");
        }
        catch (DocumentArchivedDuringReviewException)
        {
            return new ReviewDocumentResult(
                ReviewDocumentOutcome.NotReviewable,
                document.Id,
                DocumentStatus.NeedsReview,
                document.Type,
                document.TargetDepartment,
                ErrorMessage: "Restore the archived document before reviewing.");
        }

        _logger.LogInformation(
            "Document {DocumentId} manually reviewed and completed. Type: {DocumentType}, Department: {Department}.",
            document.Id,
            document.Type,
            document.TargetDepartment);

        return new ReviewDocumentResult(
            ReviewDocumentOutcome.Completed,
            document.Id,
            document.Status,
            document.Type,
            document.TargetDepartment, Version: document.Version.ToString());
    }

    private static NormalizedFieldsResult NormalizeFields(ReviewDocumentCommand command)
    {
        if (command.Type is DocumentType.Unknown ||
            command.Type is not (DocumentType.Invoice or DocumentType.Contract or DocumentType.Act))
        {
            return new([], "Выберите тип Invoice, Contract или Act.");
        }

        if (command.Fields is null || command.Fields.Count > 20)
            return new([], "Передайте не более 20 полей документа.");
        var fields = new List<ReviewDocumentField>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in command.Fields)
        {
            if (field is null || string.IsNullOrWhiteSpace(field.Name) || field.Value?.Length > 4000)
                return new([], "Название поля обязательно; значение не должно превышать 4000 символов.");
            var canonicalName = DocumentFieldSchema
                .GetSupportedFieldNames(command.Type)
                .FirstOrDefault(name => string.Equals(
                    name,
                    field.Name,
                    StringComparison.OrdinalIgnoreCase));
            if (canonicalName is null)
            {
                return new([], "Поле не поддерживается выбранным типом документа.");
            }

            if (!names.Add(canonicalName))
            {
                return new([], $"Поле «{canonicalName}» передано несколько раз.");
            }

            var value = field.Value?.Trim();
            if (canonicalName == DocumentFieldNames.Amount && !string.IsNullOrWhiteSpace(value))
                value = DocumentFieldSchema.NormalizeAmount(value);
            fields.Add(new ReviewDocumentField(canonicalName, value));
        }

        if (command.Type == DocumentType.Invoice)
        {
            var supplier = fields.FirstOrDefault(field => field.Name == DocumentFieldNames.SupplierInn);
            if (!string.IsNullOrWhiteSpace(supplier?.Value))
            {
                // Only an explicitly submitted supplier can update the required legacy invoice INN.
                fields.RemoveAll(field => field.Name == DocumentFieldNames.Inn);
                fields.Add(new ReviewDocumentField(DocumentFieldNames.Inn, supplier.Value));
            }
        }
        return new(fields, null);
    }

    private sealed record NormalizedFieldsResult(
        IReadOnlyCollection<ReviewDocumentField> Fields,
        string? ErrorMessage);
}
