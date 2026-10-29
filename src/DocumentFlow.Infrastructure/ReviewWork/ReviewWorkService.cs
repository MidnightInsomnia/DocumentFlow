using System.Data;
using System.Text.Json;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentFlow.Infrastructure.ReviewWork;

public sealed record ReviewWorkField(string? Name, string? Value);
public sealed record ReviewDraftRequest(string? Type, IReadOnlyList<ReviewWorkField>? Fields, string? BaseVersion, DateTime? ExpectedDraftUpdatedAtUtc = null);
public sealed record ReviewDraftResult(string Type, IReadOnlyList<ReviewWorkField> Fields, string BaseVersion, DateTime UpdatedAtUtc);
public sealed record ReopenDocumentRequest(string? Reason, string? ExpectedVersion);
public sealed record ResolveDocumentRequest(string? Resolution, string? Reason, string? ExpectedVersion);
public sealed record ReviewWorkResult(Guid Id, string Status, string Type, bool IsArchived, string Version, string? Resolution = null);
public sealed record ReviewWorkHistory(IReadOnlyList<ReviewRevision> Revisions, IReadOnlyList<ReviewDisposition> Dispositions);

public sealed class ReviewWorkException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class ReviewWorkService(DocumentFlowDbContext db, ICurrentUserContext currentUser)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReviewDraftResult?> GetDraftAsync(Guid id, CancellationToken ct)
    {
        var actor = Actor();
        var document = await db.Documents.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        EnsureEditable(document);
        var draft = await db.Set<ReviewDraft>().AsNoTracking()
            .SingleOrDefaultAsync(item => item.DocumentId == id && item.UserId == actor.Id, ct);
        return draft is null ? null : ToResult(draft);
    }

    public async Task<ReviewDraftResult> SaveDraftAsync(Guid id, ReviewDraftRequest request, CancellationToken ct)
    {
        var actor = Actor();
        var version = ParseVersion(request.BaseVersion);
        // A draft may precede choosing a technical schema. Unknown is stored internally,
        // while the public draft contract consistently uses an empty string for this state.
        var type = DocumentType.Unknown;
        if (request.Type != "" &&
            (!Enum.GetNames<DocumentType>().Any(name => name.Equals(request.Type, StringComparison.OrdinalIgnoreCase)) ||
             !Enum.TryParse(request.Type, true, out type) || type == DocumentType.Unknown))
            throw Invalid("Тип черновика: пустая строка, Invoice, Contract или Act.");
        if (request.Fields is null || request.Fields.Count > 20)
            throw Invalid("Передайте массив fields, не более 20 полей.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fields = new List<ReviewWorkField>();
        foreach (var field in request.Fields)
        {
            if (field is null || field.Value?.Length > 4000)
                throw Invalid("Поле не может быть null; значение не должно превышать 4000 символов.");
            var name = DocumentFieldSchema.GetSupportedFieldNames(type)
                .FirstOrDefault(item => item.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
            if (name is null || !names.Add(name))
                throw Invalid("Название поля не поддерживается или повторяется.");
            // Drafts deliberately retain incomplete/invalid input exactly as entered.
            fields.Add(new ReviewWorkField(name, field.Value ?? ""));
        }
        var json = JsonSerializer.Serialize(fields, JsonOptions);
        if (json.Length > 32000) throw Invalid("Черновик не должен превышать 32000 символов.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var document = await LockAsync(id, ct) ?? throw Missing();
        CheckVersion(document, version);
        EnsureEditable(document);
        var draft = await db.Set<ReviewDraft>()
            .SingleOrDefaultAsync(item => item.DocumentId == id && item.UserId == actor.Id, ct);
        // Document CAS does not detect another tab saving the same user's draft.
        // The document row lock makes checking and replacing the draft atomic.
        if (draft?.UpdatedAtUtc != request.ExpectedDraftUpdatedAtUtc?.ToUniversalTime())
            throw new ReviewWorkException(409, "review_draft_conflict",
                "Черновик сохранён в другой вкладке. Ваш ввод не перезаписан. Перечитайте и сравните оба варианта перед сохранением.");
        if (draft is null)
        {
            draft = new ReviewDraft { DocumentId = id, UserId = actor.Id };
            db.Add(draft);
        }
        draft.Type = type;
        draft.FieldsJson = json;
        draft.BaseVersion = version;
        // PostgreSQL persists microseconds; return exactly the persisted concurrency token.
        var now = DateTime.UtcNow;
        now = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);
        draft.UpdatedAtUtc = now > draft.UpdatedAtUtc ? now : draft.UpdatedAtUtc.AddTicks(10);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return ToResult(draft);
    }

    public async Task<ReviewWorkResult> ReopenAsync(Guid id, ReopenDocumentRequest request, CancellationToken ct)
    {
        var actor = Actor();
        var version = ParseVersion(request.ExpectedVersion);
        var reason = Reason(request.Reason);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var document = await LockAsync(id, ct) ?? throw Missing();
        CheckVersion(document, version);
        if (document.IsArchived || document.Status != DocumentStatus.Completed)
            throw Conflict("Вернуть на проверку можно только завершённый документ вне архива.");
        await db.Entry(document).Collection(item => item.Fields).LoadAsync(ct);
        var previousReviewer = await db.DocumentEvents.AsNoTracking()
            .Where(item => item.DocumentId == id && item.Type == DocumentEventType.ManuallyReviewed)
            .OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id).FirstOrDefaultAsync(ct);
        db.Add(new ReviewRevision
        {
            Id = Guid.NewGuid(), DocumentId = id, DocumentVersion = document.Version,
            Type = document.Type, TargetDepartment = document.TargetDepartment,
            ClassificationConfidence = document.ClassificationConfidence, ProcessedAtUtc = document.ProcessedAtUtc,
            FieldsJson = JsonSerializer.Serialize(document.Fields.OrderBy(item => item.Name).Select(item => new
            {
                item.Name, item.Value, item.Confidence, item.WasManuallyEdited, item.CreatedAtUtc, item.UpdatedAtUtc
            }), JsonOptions),
            ActorUserId = actor.Id, ActorDisplayName = actor.Name, Reason = reason, CreatedAtUtc = DateTime.UtcNow,
            PreviousReviewerUserId = previousReviewer?.ActorUserId,
            PreviousReviewerDisplayName = previousReviewer?.ActorDisplayName
        });
        document.ReopenForReview(actor.Id, actor.Name, reason);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return Result(document);
    }

    public async Task<ReviewWorkResult> ResolveAsync(Guid id, ResolveDocumentRequest request, CancellationToken ct)
    {
        var actor = Actor();
        var version = ParseVersion(request.ExpectedVersion);
        var reason = Reason(request.Reason);
        if (request.Resolution is not ("notSupported" or "irrelevant"))
            throw Invalid("resolution должен быть notSupported или irrelevant.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var document = await LockAsync(id, ct) ?? throw Missing();
        CheckVersion(document, version);
        if (document.IsArchived || document.Status is not (DocumentStatus.NeedsReview or DocumentStatus.Failed))
            throw Conflict("Решение доступно только для незархивированного документа на проверке или с ошибкой.");
        var now = DateTime.UtcNow;
        db.Add(new ReviewDisposition
        {
            Id = Guid.NewGuid(), DocumentId = id, DocumentVersion = document.Version,
            Resolution = request.Resolution, Reason = reason,
            ActorUserId = actor.Id, ActorDisplayName = actor.Name, CreatedAtUtc = now
        });
        document.Archive(actor.Id, actor.Name, now);
        var label = request.Resolution == "notSupported" ? "неподдерживаемый тип" : "не относится к обработке";
        document.Events.Add(new DocumentEvent(id, DocumentEventType.DocumentResolved,
            $"Решение: {label} ({request.Resolution}). Причина: {reason}", actor.Id, actor.Name));
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return Result(document) with { Resolution = request.Resolution };
    }

    public async Task<ReviewWorkHistory> GetHistoryAsync(Guid id, CancellationToken ct)
    {
        _ = Actor();
        if (!await db.Documents.AsNoTracking().AnyAsync(item => item.Id == id, ct)) throw Missing();
        var revisions = await db.Set<ReviewRevision>().AsNoTracking().Where(item => item.DocumentId == id)
            .OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id).ToListAsync(ct);
        var dispositions = await db.Set<ReviewDisposition>().AsNoTracking().Where(item => item.DocumentId == id)
            .OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id).ToListAsync(ct);
        return new ReviewWorkHistory(revisions, dispositions);
    }

    private async Task<Document?> LockAsync(Guid id, CancellationToken ct) =>
        (await db.Documents.FromSqlInterpolated($"""SELECT * FROM "Documents" WHERE "Id" = {id} FOR UPDATE""")
            .ToListAsync(ct)).SingleOrDefault();

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw VersionConflict(); }
    }

    private (Guid Id, string Name) Actor()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } id || id == Guid.Empty)
            throw new ReviewWorkException(401, "authentication_required", "Сеанс завершён. Войдите повторно.");
        var name = !string.IsNullOrWhiteSpace(currentUser.DisplayName) ? currentUser.DisplayName
            : !string.IsNullOrWhiteSpace(currentUser.UserName) ? currentUser.UserName : id.ToString();
        return (id, name!.Length <= 200 ? name : name[..200]);
    }

    private static void EnsureEditable(Document? document)
    {
        if (document is null) throw Missing();
        if (document.IsArchived || document.Status != DocumentStatus.NeedsReview)
            throw Conflict("Черновик доступен только для документа, ожидающего ручной проверки, вне архива.");
    }
    private static void CheckVersion(Document document, Guid version)
    {
        if (document.Version != version) throw VersionConflict();
    }
    private static Guid ParseVersion(string? text)
    {
        if (!Guid.TryParse(text, out var version) || version == Guid.Empty)
            throw new ReviewWorkException(400, "document_version_required", "Передайте GUID версии из актуальной карточки документа.");
        return version;
    }
    private static string Reason(string? value)
    {
        var reason = value?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 1000)
            throw Invalid("Укажите причину от 1 до 1000 символов.");
        return reason;
    }
    private static ReviewDraftResult ToResult(ReviewDraft item) => new(item.Type == DocumentType.Unknown ? "" : item.Type.ToString(),
        (JsonSerializer.Deserialize<List<ReviewWorkField>>(item.FieldsJson, JsonOptions) ?? [])
            .Select(field => new ReviewWorkField(field.Name, field.Value ?? "")).ToArray(),
        item.BaseVersion.ToString(), item.UpdatedAtUtc);
    private static ReviewWorkResult Result(Document item) => new(item.Id, item.Status.ToString(),
        item.Type.ToString(), item.IsArchived, item.Version.ToString());
    private static ReviewWorkException Invalid(string message) => new(400, "invalid_review_request", message);
    private static ReviewWorkException Missing() => new(404, "document_not_found", "Документ не найден.");
    private static ReviewWorkException Conflict(string message) => new(409, "document_not_editable", message);
    private static ReviewWorkException VersionConflict() => new(409, "document_version_conflict",
        "Документ изменён. Обновите карточку; черновик не удалён. Сопоставьте изменения перед повторным сохранением.");
}

public static class ReviewWorkServiceCollectionExtensions
{
    public static IServiceCollection AddReviewWork(this IServiceCollection services) =>
        services.AddScoped<ReviewWorkService>();
}
