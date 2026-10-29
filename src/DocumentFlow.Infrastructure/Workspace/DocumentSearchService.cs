using System.Text.Json;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Workspace;

public sealed class DocumentSearchQuery
{
    public string? Q { get; set; }
    public string Mode { get; set; } = "text";
    public string? Status { get; set; }
    public string? Type { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? KindId { get; set; }
    // Generic INN filter: any party, including the unassigned legacy INN field.
    public string? Inn { get; set; }
    public string Archive { get; set; } = "active";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record SearchDocumentItem(Guid Id, string FileName, string Status, string Type,
    decimal? ClassificationConfidence, string TargetDepartment, DateTime CreatedAtUtc, DateTime? ProcessedAtUtc,
    bool IsArchived, string? MatchReason, string? Number, string? Date, string? Inn, string? Amount,
    string? KindName, string? DepartmentName, double? Score, string Snippet,
    string? CustomerINN = null, string? ContractorINN = null, string? SupplierINN = null, string? BuyerINN = null);
public sealed record SearchDocumentResponse(int Page, int PageSize, int TotalCount, IReadOnlyList<SearchDocumentItem> Items,
    bool SemanticAvailable, int IndexedCount, string? Message);
public sealed record CounterpartySearchItem(string Inn, int DocumentCount)
{
    public string InnScope => "anyParty";
}
public sealed record CounterpartySearchResponse(IReadOnlyList<CounterpartySearchItem> Items);
public sealed record SearchIndexStatusResponse(bool Enabled, string Model, string? ModelFingerprint, bool SemanticAvailable,
    int IndexedCount, int TotalIndexedCount, int EligibleDocumentCount, DateTime? LastBatchAtUtc, int IndexedInLastBatch,
    bool RebuildRequested, string? Message);

public sealed class DocumentSearchService(DocumentFlowDbContext db, LocalEmbeddingClient embeddings, WorkspaceIndexState state)
{
    private sealed class Row
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public DocumentStatus Status { get; set; }
        public DocumentType Type { get; set; }
        public decimal? Confidence { get; set; }
        public Department TargetDepartment { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public bool IsArchived { get; set; }
        public string Text { get; set; } = "";
        public string? Number { get; set; }
        public string? Date { get; set; }
        public string? Inn { get; set; }
        public string? CustomerINN { get; set; }
        public string? ContractorINN { get; set; }
        public string? SupplierINN { get; set; }
        public string? BuyerINN { get; set; }
        public string? Amount { get; set; }
        public string? KindName { get; set; }
        public string? DepartmentName { get; set; }
    }
    private static string Like(string value) => "%" + value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    private static T? ParseEnum<T>(string? value) where T : struct, Enum
    {
        if (value is null) return null;
        if (!Enum.GetNames<T>().Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)) || !Enum.TryParse<T>(value, true, out var parsed))
            throw new WorkspaceException(400, "Некорректный фильтр типа или статуса.");
        return parsed;
    }
    private IQueryable<Row> Filter(DocumentSearchQuery query, bool includeTextQuery)
    {
        var status = ParseEnum<DocumentStatus>(query.Status);
        var type = ParseEnum<DocumentType>(query.Type);
        var documents = db.Documents.AsNoTracking().AsQueryable();
        if (status.HasValue) documents = documents.Where(x => x.Status == status);
        if (type.HasValue) documents = documents.Where(x => x.Type == type);
        if (query.Archive == "active") documents = documents.Where(x => !x.IsArchived);
        if (query.Archive == "archived") documents = documents.Where(x => x.IsArchived);
        var joined = from d in documents
                     from w in db.Set<DocumentWorkspace>().Where(x => x.DocumentId == d.Id).DefaultIfEmpty()
                     from k in db.Set<DocumentKind>().Where(x => x.Id == w.KindId).DefaultIfEmpty()
                     from p in db.Set<OrganizationDepartment>().Where(x => x.Id == w.DepartmentId).DefaultIfEmpty()
                     select new { Document = d, Workspace = w, Kind = k, Department = p };
        if (query.KindId.HasValue) joined = joined.Where(x => x.Workspace.KindId == query.KindId);
        if (query.DepartmentId.HasValue) joined = joined.Where(x => x.Workspace.DepartmentId == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.Inn))
        {
            var inn = Like(query.Inn);
            joined = joined.Where(x => x.Document.Fields.Any(f =>
                (f.Name == DocumentFieldNames.Inn || f.Name == "CustomerINN" || f.Name == "ContractorINN" ||
                 f.Name == "SupplierINN" || f.Name == "BuyerINN") &&
                f.Value != null && EF.Functions.ILike(f.Value, inn, "\\")));
        }
        if (includeTextQuery && !string.IsNullOrWhiteSpace(query.Q))
        {
            var q = Like(query.Q);
            joined = joined.Where(x => EF.Functions.ILike(x.Document.OriginalFileName, q, "\\") ||
                (x.Document.ExtractedText != null && EF.Functions.ILike(x.Document.ExtractedText, q, "\\")) ||
                (x.Kind != null && EF.Functions.ILike(x.Kind.Name, q, "\\")) ||
                (x.Department != null && EF.Functions.ILike(x.Department.Name, q, "\\")) ||
                x.Document.Fields.Any(f => f.Value != null && EF.Functions.ILike(f.Value, q, "\\")));
        }
        return joined.Select(x => new Row
        {
            Id = x.Document.Id, FileName = x.Document.OriginalFileName, Status = x.Document.Status, Type = x.Document.Type,
            Confidence = x.Document.ClassificationConfidence, TargetDepartment = x.Document.TargetDepartment,
            CreatedAtUtc = x.Document.CreatedAtUtc, ProcessedAtUtc = x.Document.ProcessedAtUtc, IsArchived = x.Document.IsArchived,
            Text = (x.Document.ExtractedText ?? "").Substring(0, Math.Min((x.Document.ExtractedText ?? "").Length, 4000)),
            Number = x.Document.Fields.Where(f => f.Name == DocumentFieldNames.Number).Select(f => f.Value).FirstOrDefault(),
            Date = x.Document.Fields.Where(f => f.Name == DocumentFieldNames.Date).Select(f => f.Value).FirstOrDefault(),
            Inn = x.Document.Type == DocumentType.Invoice
                ? x.Document.Fields.Where(f => f.Name == "SupplierINN").Select(f => f.Value).FirstOrDefault()
                  ?? x.Document.Fields.Where(f => f.Name == DocumentFieldNames.Inn).Select(f => f.Value).FirstOrDefault()
                : x.Document.Type == DocumentType.Act || x.Document.Type == DocumentType.Contract
                    ? x.Document.Fields.Where(f => f.Name == "ContractorINN").Select(f => f.Value).FirstOrDefault()
                    : null,
            CustomerINN = x.Document.Fields.Where(f => f.Name == "CustomerINN").Select(f => f.Value).FirstOrDefault(),
            ContractorINN = x.Document.Fields.Where(f => f.Name == "ContractorINN").Select(f => f.Value).FirstOrDefault(),
            SupplierINN = x.Document.Fields.Where(f => f.Name == "SupplierINN").Select(f => f.Value).FirstOrDefault(),
            BuyerINN = x.Document.Fields.Where(f => f.Name == "BuyerINN").Select(f => f.Value).FirstOrDefault(),
            Amount = x.Document.Fields.Where(f => f.Name == DocumentFieldNames.Amount).Select(f => f.Value).FirstOrDefault(),
            KindName = x.Kind == null ? null : x.Kind.Name, DepartmentName = x.Department == null ? null : x.Department.Name
        });
    }
    private static void Validate(DocumentSearchQuery query)
    {
        query.Q = query.Q?.Trim();
        query.Inn = query.Inn?.Trim();
        query.Mode = query.Mode?.Trim().ToLowerInvariant() ?? "";
        query.Archive = query.Archive?.Trim().ToLowerInvariant() ?? "";
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || query.Page > int.MaxValue / query.PageSize)
            throw new WorkspaceException(400, "page должен быть >= 1, pageSize — от 1 до 100.");
        if (query.Q?.Length > 500) throw new WorkspaceException(400, "q не должен превышать 500 символов.");
        if (query.Mode is not ("text" or "semantic")) throw new WorkspaceException(400, "mode должен быть text или semantic.");
        if (query.Archive is not ("active" or "archived" or "all")) throw new WorkspaceException(400, "archive должен быть active, archived или all.");
        if (!string.IsNullOrEmpty(query.Inn) && (query.Inn.Length > 12 || !query.Inn.All(char.IsAsciiDigit)))
            throw new WorkspaceException(400, "inn должен содержать от 1 до 12 цифр.");
        if (query.KindId.HasValue) CatalogService.ValidId(query.KindId.Value);
        if (query.DepartmentId.HasValue) CatalogService.ValidId(query.DepartmentId.Value);
        if (query.Mode == "semantic" && (string.IsNullOrWhiteSpace(query.Q) || (long)query.Page * query.PageSize > 10000))
            throw new WorkspaceException(400, "Для semantic необходим q, глубина выдачи ограничена 10000 результатами. Уточните фильтры.");
    }
    public async Task<SearchDocumentResponse> SearchAsync(DocumentSearchQuery query, CancellationToken ct)
    {
        Validate(query);
        // Validate enum filters before contacting Ollama.
        var filtered = Filter(query, query.Mode == "text");
        var available = embeddings.Options.Enabled && state.ModelStatus.Available;
        var cachedKey = state.ModelStatus.Key;
        if (query.Mode == "text")
        {
            var count = await filtered.CountAsync(ct);
            var rows = await filtered.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
            var indexed = cachedKey is null ? 0 : await db.Set<DocumentSearchIndex>().CountAsync(x => x.Model == cachedKey, ct);
            return new(query.Page, query.PageSize, count, rows.Select(x => Item(x, null, query.Q)).ToArray(),
                available, indexed, available ? null : state.ModelStatus.Message ?? "Семантическая модель ещё не проверена; текстовый поиск доступен.");
        }
        var modelKey = await embeddings.GetModelKeyAsync(ct);
        var vector = (await embeddings.EmbedAsync(new[] { query.Q! }, ct))[0];
        // A model retag/replacement must never silently mix vector spaces.
        if (await embeddings.GetModelKeyAsync(ct, force: true) != modelKey)
            throw new SemanticUnavailableException("Embedding-модель изменилась во время запроса. Повторите после переиндексации.");
        var limit = query.Page * query.PageSize;
        var best = new PriorityQueue<(Guid Id, double Score), (double Score, Guid Id)>();
        var indexQuery = SemanticIndex(filtered, modelKey);
        var total = 0;
        await foreach (var index in indexQuery.AsAsyncEnumerable().WithCancellation(ct))
        {
            float[]? candidate;
            try { candidate = JsonSerializer.Deserialize<float[]>(index.EmbeddingJson); }
            catch (JsonException) { continue; }
            if (!LocalEmbeddingClient.ValidVector(candidate) || candidate!.Length != vector.Length) continue;
            var score = Cosine(vector, candidate);
            total++;
            best.Enqueue((index.Id, score), (score, index.Id));
            if (best.Count > limit) best.Dequeue();
        }
        var page = best.UnorderedItems.Select(x => x.Element).OrderByDescending(x => x.Score).ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray();
        var ids = page.Select(x => x.Id).ToArray();
        var matches = await filtered.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var items = page.Where(x => matches.ContainsKey(x.Id)).Select(x => Item(matches[x.Id], x.Score, query.Q)).ToArray();
        var indexedCount = await db.Set<DocumentSearchIndex>().CountAsync(x => x.Model == modelKey, ct);
        var eligibleCount = await filtered.CountAsync(ct);
        return new(query.Page, query.PageSize, total, items, true, indexedCount,
            total < eligibleCount ? "Выдача содержит только проиндексированные документы текущей версии модели; фоновая индексация продолжается." : null);
    }
    private sealed record IndexVector(Guid Id, string EmbeddingJson);
    private IQueryable<IndexVector> SemanticIndex(IQueryable<Row> filtered, string modelKey) =>
        from row in filtered
        join index in db.Set<DocumentSearchIndex>().AsNoTracking() on row.Id equals index.DocumentId
        where index.Model == modelKey && row.Status != DocumentStatus.PendingProcessing && row.Status != DocumentStatus.Processing &&
            db.Documents.Any(d => d.Id == row.Id &&
                (d.ProcessedAtUtc == null || d.ProcessedAtUtc <= index.IndexedAtUtc) &&
                (d.ProcessingStartedAtUtc == null || d.ProcessingStartedAtUtc <= index.IndexedAtUtc) &&
                !d.Fields.Any(f => (f.UpdatedAtUtc ?? f.CreatedAtUtc) > index.IndexedAtUtc)) &&
            !db.Set<DocumentWorkspace>().Any(w => w.DocumentId == row.Id && w.LastUpdatedAtUtc > index.IndexedAtUtc)
        select new IndexVector(row.Id, index.EmbeddingJson);

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++) { dot += (double)a[i] * b[i]; na += (double)a[i] * a[i]; nb += (double)b[i] * b[i]; }
        return Math.Clamp(dot / Math.Sqrt(na * nb), -1, 1);
    }
    private static SearchDocumentItem Item(Row r, double? score, string? q)
    {
        var source = string.Join(" · ", new[] { r.Number, r.Date, r.Inn, r.CustomerINN, r.ContractorINN,
            r.SupplierINN, r.BuyerINN, r.Amount, r.Text }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var position = string.IsNullOrWhiteSpace(q) ? 0 : source.IndexOf(q, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(0, position - 80);
        var snippet = (start > 0 ? "…" : "") + source.Substring(start, Math.Min(400, source.Length - start));
        return new(r.Id, r.FileName, r.Status.ToString(), r.Type.ToString(), r.Confidence, r.TargetDepartment.ToString(),
            r.CreatedAtUtc, r.ProcessedAtUtc, r.IsArchived, score.HasValue ? "Косинусная близость локальных embeddings; не вероятность." : null,
            r.Number, r.Date, r.Inn, r.Amount, r.KindName, r.DepartmentName, score, snippet,
            r.CustomerINN, r.ContractorINN, r.SupplierINN, r.BuyerINN);
    }
    public async Task<CounterpartySearchResponse> CounterpartiesAsync(string? q, CancellationToken ct)
    {
        q = q?.Trim();
        if (!string.IsNullOrEmpty(q) && (q.Length > 12 || !q.All(char.IsAsciiDigit)))
            throw new WorkspaceException(400, "q контрагента должен содержать не более 12 цифр ИНН.");
        // Group once by INN, not by field row: an invoice may carry both SupplierINN and legacy INN.
        var fields = db.DocumentFields.AsNoTracking().Where(x =>
            (x.Name == DocumentFieldNames.Inn || x.Name == "CustomerINN" || x.Name == "ContractorINN" ||
             x.Name == "SupplierINN" || x.Name == "BuyerINN") && x.Value != null && x.Value != "");
        if (!string.IsNullOrEmpty(q)) fields = fields.Where(x => x.Value!.Contains(q));
        var rows = await fields.GroupBy(x => x.Value!)
            .Select(g => new { Inn = g.Key, DocumentCount = g.Select(f => f.DocumentId).Distinct().Count() })
            .OrderByDescending(x => x.DocumentCount).ThenBy(x => x.Inn).Take(50).ToListAsync(ct);
        return new(rows.Select(x => new CounterpartySearchItem(x.Inn, x.DocumentCount)).ToArray());
    }
    public async Task<SearchIndexStatusResponse> StatusAsync(CancellationToken ct)
    {
        string? message = null;
        string? key = null;
        try { key = await embeddings.GetModelKeyAsync(ct); }
        catch (SemanticUnavailableException ex) { message = ex.Message; }
        var batch = state.IndexStatus;
        return new(embeddings.Options.Enabled, embeddings.Options.Model, key, key is not null,
            key is null ? 0 : await db.Set<DocumentSearchIndex>().CountAsync(x => x.Model == key, ct),
            await db.Set<DocumentSearchIndex>().CountAsync(ct),
            await db.Documents.CountAsync(x => (x.ExtractedText != null || x.Fields.Any(f => f.Value != null && f.Value != "")) && x.Status != DocumentStatus.PendingProcessing && x.Status != DocumentStatus.Processing, ct),
            batch.LastBatchAtUtc, batch.IndexedInLastBatch, batch.RebuildRequested, message);
    }
    public async Task<object> ReindexAsync(bool rebuild, CancellationToken ct)
    {
        // Fail before deleting a valid index if the requested local model is unavailable.
        await embeddings.GetModelKeyAsync(ct, force: true);
        if (rebuild) state.RequestRebuild();
        return new { accepted = true, rebuild, message = rebuild ? "Полная переиндексация поставлена в очередь. Индекс очищается фоновым обработчиком." : "Инкрементальная индексация работает в фоне; неизменённые embeddings переиспользуются." };
    }
}
