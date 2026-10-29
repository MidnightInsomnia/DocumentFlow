using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentFlow.Infrastructure.Workspace;

internal sealed record IndexSource(Guid Id, string FileName, DocumentType Type, string Text,
    IReadOnlyList<IndexField> Fields, Guid? KindId, string? KindName, string? DepartmentName, DateTime ChangedAtUtc, bool IsArchived);
internal sealed record IndexField(string Name, string? Value);

public sealed class WorkspaceIndexingService(DocumentFlowDbContext db, LocalEmbeddingClient embeddings,
    KindDiscoveryService discovery, WorkspaceIndexState state, ILogger<WorkspaceIndexingService> logger)
{
    private async Task<IndexSource?> LoadSourceAsync(Guid id, CancellationToken ct)
    {
        var max = embeddings.Options.MaxInputCharacters;
        var d = await db.Documents.AsNoTracking()
            .Where(x => x.Id == id && x.Status != DocumentStatus.PendingProcessing && x.Status != DocumentStatus.Processing)
            .Select(x => new
            {
                x.Id, x.OriginalFileName, x.Type, x.CreatedAtUtc, x.ProcessedAtUtc, x.ProcessingStartedAtUtc, x.IsArchived,
                LastFieldUpdatedAtUtc = x.Fields.Select(f => (DateTime?)(f.UpdatedAtUtc ?? f.CreatedAtUtc)).Max(),
                Text = (x.ExtractedText ?? "").Substring(0, Math.Min((x.ExtractedText ?? "").Length, max)),
                Fields = x.Fields.OrderBy(f => f.Name).Take(64).Select(f => new IndexField(f.Name, f.Value)).ToList()
            }).SingleOrDefaultAsync(ct);
        if (d is null) return null;
        var w = await db.Set<DocumentWorkspace>().AsNoTracking().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
        var kind = w?.KindId is Guid kindId ? await db.Set<DocumentKind>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == kindId, ct) : null;
        var department = w?.DepartmentId is Guid departmentId ? await db.Set<OrganizationDepartment>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == departmentId, ct) : null;
        var changedAt = new[] { d.CreatedAtUtc, d.ProcessedAtUtc ?? DateTime.MinValue,
            d.ProcessingStartedAtUtc ?? DateTime.MinValue, d.LastFieldUpdatedAtUtc ?? DateTime.MinValue,
            w?.LastUpdatedAtUtc ?? DateTime.MinValue }.Max();
        return new(d.Id, d.OriginalFileName, d.Type, d.Text, d.Fields, w?.KindId, kind?.Name, department?.Name, changedAt, d.IsArchived);
    }
    private string Input(IndexSource source)
    {
        // Prefer business fields over OCR text; all truncation is deterministic and part of the fingerprint.
        var header = $"Файл: {source.FileName}\nТехническая схема: {source.Type}\nВид: {source.KindName}\nОтдел: {source.DepartmentName}\n" +
            string.Join("\n", source.Fields.Select(x => x.Name + ": " + x.Value)) + "\nТекст:\n";
        var input = header + source.Text;
        return input[..Math.Min(input.Length, embeddings.Options.MaxInputCharacters)];
    }
    private static string Fingerprint(string input) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("workspace-v1\n" + input)));

    public async Task<int> IndexBatchAsync(Guid? after, CancellationToken ct)
    {
        string? modelKey = null;
        if (embeddings.Options.Enabled)
        {
            try { modelKey = await embeddings.GetModelKeyAsync(ct); }
            catch (SemanticUnavailableException) { /* Catalog/discovery/text search remain independent. */ }
        }
        if (state.ConsumeRebuild())
        {
            try
            {
                await using var transaction = await CatalogService.LockAsync(db, ct);
                await db.Set<DocumentSearchIndex>().ExecuteDeleteAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch { state.RequestRebuild(); throw; }
        }
        var candidates = db.Documents.AsNoTracking()
            .Where(x => x.Status != DocumentStatus.PendingProcessing && x.Status != DocumentStatus.Processing);
        if (after.HasValue) candidates = candidates.Where(x => x.Id.CompareTo(after.Value) > 0);
        var documents = await candidates.OrderBy(x => x.Id).Take(embeddings.Options.BatchSize)
            .Select(x => new { x.Id, x.Type, x.TargetDepartment }).ToListAsync(ct);
        LastDocumentId = documents.LastOrDefault()?.Id;
        var changed = 0;
        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // This also gives newly completed known documents their stable system catalog metadata.
                await using (var transaction = await CatalogService.LockAsync(db, ct))
                {
                    if (!await db.Set<DocumentWorkspace>().AnyAsync(x => x.DocumentId == document.Id, ct))
                    {
                        var active = await WorkspaceInitializer.ActiveDefaultsAsync(db, ct);
                        db.Add(WorkspaceInitializer.NewWorkspace(document.Id, document.Type, document.TargetDepartment, active));
                        await db.SaveChangesAsync(ct);
                    }
                    await transaction.CommitAsync(ct);
                }
                var source = await LoadSourceAsync(document.Id, ct);
                if (source is null) continue;
                if (!source.IsArchived && source.Type == DocumentType.Unknown && source.KindId is null && source.Text.Length >= 40)
                {
                    await discovery.DiscoverAsync(source.Id, ct);
                    source = await LoadSourceAsync(document.Id, ct);
                    if (source is null) continue;
                }
                if (modelKey is null || (source.Text.Length == 0 && !source.Fields.Any(f => !string.IsNullOrWhiteSpace(f.Value)))) continue;
                var input = Input(source);
                var fingerprint = Fingerprint(input);
                var previous = await db.Set<DocumentSearchIndex>().AsNoTracking().SingleOrDefaultAsync(x => x.DocumentId == source.Id, ct);
                if (previous?.Model == modelKey && previous.Fingerprint == fingerprint)
                {
                    if (previous.IndexedAtUtc < source.ChangedAtUtc)
                    {
                        await using var transaction = await CatalogService.LockAsync(db, ct);
                        await LockDocumentAsync(source.Id, ct);
                        var fresh = await LoadSourceAsync(source.Id, ct);
                        if (fresh is not null && Fingerprint(Input(fresh)) == fingerprint)
                            await db.Set<DocumentSearchIndex>().Where(x => x.DocumentId == source.Id && x.Model == modelKey && x.Fingerprint == fingerprint)
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IndexedAtUtc, DateTime.UtcNow), ct);
                        await transaction.CommitAsync(ct);
                    }
                    continue;
                }
                var vector = (await embeddings.EmbedAsync(new[] { input }, ct))[0];
                // Recheck digest after inference; no row can be labelled with another model's identity.
                if (await embeddings.GetModelKeyAsync(ct, force: true) != modelKey)
                    throw new SemanticUnavailableException("Embedding-модель изменилась; партия будет повторена.");
                await using (var transaction = await CatalogService.LockAsync(db, ct))
                {
                    await LockDocumentAsync(source.Id, ct);
                    var fresh = await LoadSourceAsync(source.Id, ct);
                    if (fresh is null || Fingerprint(Input(fresh)) != fingerprint) continue;
                    var index = await db.Set<DocumentSearchIndex>().SingleOrDefaultAsync(x => x.DocumentId == source.Id, ct);
                    if (index is null) { index = new DocumentSearchIndex { DocumentId = source.Id }; db.Add(index); }
                    index.Model = modelKey;
                    index.Fingerprint = fingerprint;
                    index.EmbeddingJson = JsonSerializer.Serialize(vector);
                    index.IndexedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    changed++;
                }
            }
            catch (SemanticUnavailableException) { modelKey = null; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Neither prompts nor OCR text nor upstream exception bodies may reach the logs.
                logger.LogWarning("Workspace indexing failed for {DocumentId}; {ErrorType}", document.Id, ex.GetType().Name);
            }
            finally { db.ChangeTracker.Clear(); }
        }
        state.BatchFinished(changed);
        return documents.Count;
    }
    public Guid? LastDocumentId { get; private set; }

    private async Task LockDocumentAsync(Guid id, CancellationToken ct) =>
        _ = await db.Documents.FromSqlInterpolated($"""SELECT * FROM "Documents" WHERE "Id" = {id} FOR UPDATE""")
            .AsNoTracking().Select(x => x.Id).ToListAsync(ct);


}

public sealed class WorkspaceIndexingHostedService(IServiceScopeFactory scopes, ILogger<WorkspaceIndexingHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Guid? cursor = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = 30;
            try
            {
                using var scope = scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<WorkspaceIndexingService>();
                var options = scope.ServiceProvider.GetRequiredService<LocalEmbeddingClient>().Options;
                if (scope.ServiceProvider.GetRequiredService<WorkspaceIndexState>().IndexStatus.RebuildRequested) cursor = null;
                var count = await service.IndexBatchAsync(cursor, stoppingToken);
                cursor = count == options.BatchSize ? service.LastDocumentId : null;
                delay = cursor.HasValue ? 1 : options.IntervalSeconds;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning("Workspace indexing batch failed; {ErrorType}. Retrying later.", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
