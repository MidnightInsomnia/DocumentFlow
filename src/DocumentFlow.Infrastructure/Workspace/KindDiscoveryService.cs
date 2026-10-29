using System.Security.Cryptography;
using System.Text;
using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentFlow.Infrastructure.Workspace;

public sealed record KindDiscoveryResponse(Guid DocumentId, string Status, string Message, bool CanRetry,
    DateTime? LastAttemptAtUtc, DateTime? NextRetryAtUtc, Guid? KindId, string? KindName, string? ErrorCode);

internal sealed record KindDiscoverySource(Guid Id, DocumentType Type, DocumentStatus Status, bool IsArchived,
    string Text, DocumentWorkspace? Workspace, DocumentKind? Kind);

// Uses the existing persisted attempt record; no schema change or remote call on status/retry requests.
public sealed class KindDiscoveryService(DocumentFlowDbContext db, AiKindProposalClient proposals,
    WorkspaceIndexState state, ILogger<KindDiscoveryService> logger)
{
    private static readonly TimeSpan FailedRetry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NoProposalRetry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ManualRetryCooldown = TimeSpan.FromSeconds(30);

    private async Task<KindDiscoverySource> LoadAsync(Guid id, CancellationToken ct)
    {
        var document = await db.Documents.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Type, x.Status, x.IsArchived,
                Text = (x.ExtractedText ?? "").Substring(0, Math.Min((x.ExtractedText ?? "").Length, 16000)) })
            .SingleOrDefaultAsync(ct) ?? throw new WorkspaceException(404, "Документ не найден.");
        var workspace = await db.Set<DocumentWorkspace>().AsNoTracking().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
        var kind = workspace?.KindId is Guid kindId
            ? await db.Set<DocumentKind>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == kindId, ct) : null;
        return new(document.Id, document.Type, document.Status, document.IsArchived, document.Text.Trim(), workspace, kind);
    }
    private string Fingerprint(KindDiscoverySource source) => Convert.ToHexStringLower(SHA256.HashData(
        Encoding.UTF8.GetBytes("kind-discovery-v2\n" + proposals.Model + "\n" + source.Text)));
    private static bool Eligible(KindDiscoverySource source) => !source.IsArchived && source.Type == DocumentType.Unknown &&
        (source.Status is DocumentStatus.NeedsReview or DocumentStatus.Completed) && source.Workspace?.KindId is null && source.Text.Length >= 40;

    public async Task<KindDiscoveryResponse> GetAsync(Guid id, CancellationToken ct)
    {
        CatalogService.ValidId(id);
        var source = await LoadAsync(id, ct);
        var previous = await db.Set<AiKindDiscoveryState>().AsNoTracking().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
        return Response(source, previous);
    }
    private KindDiscoveryResponse Response(KindDiscoverySource source, AiKindDiscoveryState? previous)
    {
        KindDiscoveryResponse Result(string status, string message, bool retry = false, DateTime? last = null,
            DateTime? next = null, string? code = null) =>
            new(source.Id, status, message, retry, last, next, source.Kind?.Id, source.Kind?.Name, code);
        if (source.IsArchived) return Result("Archived", "Для архивного документа автоматическое предложение отключено.");
        if (source.Status is DocumentStatus.PendingProcessing or DocumentStatus.Processing)
            return Result("Processing", "Ожидается завершение распознавания документа.");
        if (source.Status == DocumentStatus.Failed)
            return Result("ProcessingFailed", "Обработка документа завершилась ошибкой. Сначала повторите обработку документа.", code: "document_processing_failed");
        if (source.Kind is not null)
            return Result(source.Kind.IsDraft ? "Suggested" : "Assigned", source.Kind.IsDraft
                ? "Предложен новый вид. Требуется подтверждение администратора." : "Вид документа уже назначен.");
        if (source.Type != DocumentType.Unknown) return Result("NotApplicable", "Для известной технической схемы новый вид автоматически не предлагается.");
        if (!proposals.Enabled) return Result("AiDisabled", "Автоматические предложения отключены (AiReview:Enabled=false).", code: "ai_disabled");
        if (source.Text.Length < 40) return Result("InsufficientText", "Для предложения вида нужно не менее 40 символов извлечённого текста. Проверьте распознавание.", code: "insufficient_text");
        if (state.IsDiscovering(source.Id)) return Result("Discovering", "Локальная модель определяет вид документа.", last: previous?.LastAttemptAtUtc);
        var current = previous?.Fingerprint == Fingerprint(source);
        var canRetry = previous is null || previous.LastAttemptAtUtc <= DateTime.UtcNow - ManualRetryCooldown;
        if (!current) return Result("Pending", "Документ ожидает автоматического предложения вида.", canRetry);
        var next = previous!.LastAttemptAtUtc + (previous.Succeeded ? NoProposalRetry : FailedRetry);
        if (next <= DateTime.UtcNow) return Result("Pending", "Запланирована следующая попытка определения вида.", canRetry, previous.LastAttemptAtUtc, next);
        return previous.Succeeded
            ? Result("NoProposal", "Автоматическое определение завершено без нового назначения. Можно создать вид вручную или повторить определение.", canRetry, previous.LastAttemptAtUtc, next)
            : Result("RetryScheduled", "Модель недоступна или вернула непригодный ответ. Попытка будет повторена автоматически.", canRetry,
                previous.LastAttemptAtUtc, next, "ai_unavailable_or_invalid_response");
    }

    public async Task<KindDiscoveryResponse> RetryAsync(Guid id, CancellationToken ct)
    {
        CatalogService.ValidId(id);
        await using (var transaction = await CatalogService.LockAsync(db, ct))
        {
            await LockDocumentAsync(id, ct);
            var source = await LoadAsync(id, ct);
            var tracking = await db.Set<AiKindDiscoveryState>().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
            var response = Response(source, tracking);
            if (!Eligible(source) || !proposals.Enabled || state.IsDiscovering(id))
                throw new WorkspaceException(409, response.Message);
            if (!response.CanRetry) throw new WorkspaceException(429, "Повторное определение доступно через 30 секунд после предыдущего запроса.");
            if (tracking is null) { tracking = new AiKindDiscoveryState { DocumentId = id }; db.Add(tracking); }
            // Empty fingerprint is a durable queue marker. The hosted indexer will claim it on its next pass.
            tracking.Fingerprint = "";
            tracking.LastAttemptAtUtc = DateTime.UtcNow;
            tracking.Succeeded = false;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        return await GetAsync(id, ct);
    }

    public async Task DiscoverAsync(Guid id, CancellationToken ct)
    {
        if (!proposals.Enabled || !state.TryBeginDiscovery(id)) return;
        try
        {
            KindDiscoverySource source;
            string fingerprint;
            DateTime attempt;
            // Persist a bounded claim before inference so another API instance cannot immediately duplicate it.
            await using (var transaction = await CatalogService.LockAsync(db, ct))
            {
                await LockDocumentAsync(id, ct);
                source = await LoadAsync(id, ct);
                if (!Eligible(source)) return;
                fingerprint = Fingerprint(source);
                var tracking = await db.Set<AiKindDiscoveryState>().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
                var now = DateTime.UtcNow;
                attempt = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);
                if (tracking?.Fingerprint == fingerprint && tracking.LastAttemptAtUtc >
                    attempt - (tracking.Succeeded ? NoProposalRetry : FailedRetry)) return;
                if (tracking is null) { tracking = new AiKindDiscoveryState { DocumentId = id }; db.Add(tracking); }
                tracking.Fingerprint = fingerprint;
                tracking.LastAttemptAtUtc = attempt;
                tracking.Succeeded = false;
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            // Completion compares the microsecond-normalized claim timestamp and fingerprint.
            // A newer queued retry or changed source wins; old inference cannot overwrite it.
            db.ChangeTracker.Clear();
            string? name = null;
            var succeeded = false;
            try { name = await proposals.ProposeAsync(source.Text, ct); succeeded = true; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { logger.LogWarning("AI kind discovery failed for {DocumentId}; {ErrorType}", id, ex.GetType().Name); }
            await using (var transaction = await CatalogService.LockAsync(db, ct))
            {
                await LockDocumentAsync(id, ct);
                var fresh = await LoadAsync(id, ct);
                var tracking = await db.Set<AiKindDiscoveryState>().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
                if (tracking is null || tracking.Fingerprint != fingerprint || tracking.LastAttemptAtUtc != attempt ||
                    !Eligible(fresh) || Fingerprint(fresh) != fingerprint ||
                    fresh.Workspace?.LastUpdatedAtUtc != source.Workspace?.LastUpdatedAtUtc) return;
                if (succeeded && name is not null)
                {
                    var normalized = CatalogService.Normalize(name);
                    var kind = await db.Set<DocumentKind>().SingleOrDefaultAsync(x => x.NormalizedName == normalized, ct);
                    if (kind is null)
                    {
                        kind = new DocumentKind { Name = name, NormalizedName = normalized, BaseType = DocumentType.Unknown,
                            IsDraft = true, IsActive = false, CreatedAutomatically = true, IsSystem = false };
                        db.Add(kind);
                    }
                    if (kind.IsDraft || kind.IsActive)
                    {
                        var workspace = await db.Set<DocumentWorkspace>().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
                        if (workspace is null) { workspace = new DocumentWorkspace { DocumentId = id }; db.Add(workspace); }
                        workspace.KindId = kind.Id;
                        workspace.LastUpdatedAtUtc = DateTime.UtcNow;
                    }
                    else succeeded = false; // Do not silently reactivate a deliberately disabled catalog entry.
                }
                // A valid null is NoProposal, not terminal success: it expires after 30 minutes.
                tracking.Succeeded = succeeded;
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
        }
        finally { state.EndDiscovery(id); }
    }
    private async Task LockDocumentAsync(Guid id, CancellationToken ct) =>
        _ = await db.Documents.FromSqlInterpolated($"""SELECT * FROM "Documents" WHERE "Id" = {id} FOR UPDATE""")
            .AsNoTracking().Select(x => x.Id).ToListAsync(ct);
}
