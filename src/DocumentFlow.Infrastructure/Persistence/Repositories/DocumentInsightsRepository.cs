using System.Globalization;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Documents.Read;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentInsightsRepository(DocumentFlowDbContext dbContext) : IDocumentInsightsRepository
{
    public async Task<ConsistencyCheckResult?> GetConsistencyAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var related = await GetRelatedAsync(documentId, cancellationToken);
        if (related is null) return null;
        var warnings = new List<ConsistencyWarning>();
        var rules = new List<string>();
        if (related.Type != DocumentType.Act)
            return new ConsistencyCheckResult(documentId, warnings, "notApplicable",
                "Сопоставление даты и суммы с договором применяется только к актам.", rules);
        if (string.IsNullOrWhiteSpace(related.ReferenceNumber))
            return new ConsistencyCheckResult(documentId, warnings, "insufficientData",
                "Не указан номер договора. Проверка связи, даты и суммы не выполнена.", rules);

        rules.Add("contractReferenceLookup");
        if (related.Items.Count == 0)
        {
            warnings.Add(new ConsistencyWarning("RelatedContractNotFound",
                "Кандидат в связанный договор с указанным номером не найден в системе.", null,
                related.ReferenceNumber, null));
            return new ConsistencyCheckResult(documentId, warnings, "insufficientData",
                "Не найден договор-кандидат; сравнить дату и сумму невозможно.", rules);
        }
        if (related.Items.Count > 1)
        {
            warnings.Add(new ConsistencyWarning("RelatedContractAmbiguous",
                "Найдено несколько договоров с указанным номером. Связь не подтверждена.", null,
                related.ReferenceNumber, null));
            return new ConsistencyCheckResult(documentId, warnings, "insufficientData",
                "Нельзя однозначно выбрать договор по одному номеру.", rules);
        }
        var contract = related.Items[0];
        warnings.Add(new ConsistencyWarning("RelatedContractCandidate",
            "Совпадение номера — только кандидат на связь с договором, не подтверждённая связь.",
            contract.Id, related.ReferenceNumber, contract.Number));
        var act = await dbContext.Documents.AsNoTracking()
            .Where(document => document.Id == documentId)
            .Select(document => new
            {
                Date = document.Fields.Where(field => field.Name == DocumentFieldNames.Date)
                    .Select(field => field.Value).FirstOrDefault(),
                Amount = document.Fields.Where(field => field.Name == DocumentFieldNames.Amount)
                    .Select(field => field.Value).FirstOrDefault()
            }).SingleAsync(cancellationToken);
        var missing = new List<string>();
        if (TryCanonicalDate(act.Date, out var actDate) && TryCanonicalDate(contract.Date, out var contractDate))
        {
            rules.Add("actDateNotBeforeContractDate");
            if (actDate < contractDate)
                warnings.Add(new ConsistencyWarning("ActDateBeforeContractDate",
                    "Дата акта раньше даты договора-кандидата.", contract.Id, act.Date, contract.Date));
        }
        else missing.Add("сопоставимые даты акта и договора");
        if (TryPositiveAmount(act.Amount, out var actAmount) && TryPositiveAmount(contract.Amount, out var contractAmount))
        {
            rules.Add("actAmountNotAboveContractAmount");
            if (actAmount > contractAmount)
                warnings.Add(new ConsistencyWarning("ActAmountExceedsContractAmount",
                    "Сумма акта превышает сумму договора-кандидата.", contract.Id, act.Amount, contract.Amount));
        }
        else missing.Add("положительные суммы акта и договора");
        return new ConsistencyCheckResult(documentId, warnings,
            missing.Count == 0 ? "checked" : "insufficientData",
            missing.Count == 0
                ? "Проверены перечисленные правила для кандидата по номеру; юридическая связь не подтверждалась."
                : $"Проверка неполная. Отсутствуют или некорректны: {string.Join(", ", missing)}.", rules);
    }

    private static bool TryCanonicalDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date);

    public async Task<PotentialDuplicateResult?> GetPotentialDuplicatesAsync(
        Guid documentId, CancellationToken cancellationToken)
    {
        var source = await dbContext.Documents.AsNoTracking()
            .Where(document => document.Id == documentId)
            .Select(document => new
            {
                document.Type,
                SupplierINN = document.Fields.Where(field => field.Name == "SupplierINN")
                    .Select(field => field.Value).FirstOrDefault(),
                LegacyINN = document.Fields.Where(field => field.Name == DocumentFieldNames.Inn)
                    .Select(field => field.Value).FirstOrDefault(),
                Number = document.Fields.Where(field => field.Name == DocumentFieldNames.Number)
                    .Select(field => field.Value).FirstOrDefault(),
                Date = document.Fields.Where(field => field.Name == DocumentFieldNames.Date)
                    .Select(field => field.Value).FirstOrDefault(),
                Amount = document.Fields.Where(field => field.Name == DocumentFieldNames.Amount)
                    .Select(field => field.Value).FirstOrDefault()
            }).SingleOrDefaultAsync(cancellationToken);
        if (source is null) return null;
        if (source.Type != DocumentType.Invoice)
            return new PotentialDuplicateResult(documentId, false, null,
                "Правило поиска бизнес-дубликатов определено только для счетов (Invoice).", source.Amount, []);

        var number = source.Number?.Trim();
        var supplierInn = source.SupplierINN ?? source.LegacyINN;
        if (supplierInn is not { Length: 10 or 12 } || !supplierInn.All(char.IsAsciiDigit) ||
            string.IsNullOrWhiteSpace(number) || source.Date is null ||
            !DateOnly.TryParseExact(source.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            return new PotentialDuplicateResult(documentId, true, null,
                "Для проверки нужны ИНН поставщика из 10 или 12 цифр, номер и дата в формате ГГГГ-ММ-ДД.", source.Amount, []);

        var key = new BusinessDuplicateMatchKey(supplierInn, number, source.Date);
        var escapedNumber = EscapeLike(number);
        var candidates = await dbContext.Documents.AsNoTracking()
            .Where(document => document.Id != documentId && document.Type == DocumentType.Invoice &&
                (document.Fields.Any(field => field.Name == "SupplierINN" && field.Value == key.Inn) ||
                 (!document.Fields.Any(field => field.Name == "SupplierINN" && field.Value != null) &&
                  document.Fields.Any(field => field.Name == DocumentFieldNames.Inn && field.Value == key.Inn))) &&
                document.Fields.Any(field => field.Name == DocumentFieldNames.Date && field.Value == key.Date) &&
                document.Fields.Any(field => field.Name == DocumentFieldNames.Number && field.Value != null &&
                    EF.Functions.ILike(field.Value.Trim(), escapedNumber, "\\")))
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenBy(document => document.Id)
            .Select(document => new PotentialDuplicateItem(
                document.Id, document.OriginalFileName, document.Type, document.Status,
                document.Fields.Where(field => field.Name == DocumentFieldNames.Number)
                    .Select(field => field.Value).FirstOrDefault(),
                document.Fields.Where(field => field.Name == DocumentFieldNames.Date)
                    .Select(field => field.Value).FirstOrDefault(),
                document.Fields.Where(field => field.Name == "SupplierINN")
                    .Select(field => field.Value).FirstOrDefault() ??
                document.Fields.Where(field => field.Name == DocumentFieldNames.Inn)
                    .Select(field => field.Value).FirstOrDefault(),
                document.Fields.Where(field => field.Name == DocumentFieldNames.Amount)
                    .Select(field => field.Value).FirstOrDefault(),
                document.IsArchived, document.CreatedAtUtc, null))
            .ToListAsync(cancellationToken);
        var sourceAmountValid = TryPositiveAmount(source.Amount, out var sourceAmount);
        var items = candidates.Select(candidate => candidate with
        {
            AmountDiffers = sourceAmountValid && TryPositiveAmount(candidate.Amount, out var candidateAmount)
                ? sourceAmount != candidateAmount : null
        }).ToList();
        return new PotentialDuplicateResult(documentId, true, key,
            "Совпадают ИНН поставщика, номер и дата: это возможный бизнес-дубликат, а не доказанное совпадение файлов.",
            source.Amount, items);
    }

    private static bool TryPositiveAmount(string? text, out decimal amount) =>
        decimal.TryParse(text is null ? null : DocumentFlow.Application.Documents.Processing.DocumentFieldSchema.NormalizeAmount(text), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out amount) && amount > 0;

    public async Task<CounterpartyCard?> GetCounterpartyAsync(
        string inn, int page, int pageSize, CancellationToken cancellationToken)
    {
        var documents = dbContext.Documents.AsNoTracking().Where(document =>
            document.Fields.Any(field =>
                (field.Name == DocumentFieldNames.Inn || field.Name == "CustomerINN" ||
                 field.Name == "ContractorINN" || field.Name == "SupplierINN" || field.Name == "BuyerINN") &&
                field.Value == inn));
        var count = await documents.CountAsync(cancellationToken);
        if (count == 0) return null;

        var types = await documents.GroupBy(document => document.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var statuses = await documents.GroupBy(document => document.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var countsByType = Enum.GetValues<DocumentType>()
            .ToDictionary(type => type, type => types.FirstOrDefault(row => row.Type == type)?.Count ?? 0);
        var countsByStatus = Enum.GetValues<DocumentStatus>()
            .ToDictionary(status => status, status => statuses.FirstOrDefault(row => row.Status == status)?.Count ?? 0);

        // KPP has no role-specific field: only attribute it when the document has a single
        // identifiable INN, otherwise it could belong to the other party.
        var knownKpps = await dbContext.DocumentFields.AsNoTracking()
            .Where(field => field.Name == DocumentFieldNames.Kpp && field.Value != null &&
                field.Document.Fields.Any(innField =>
                    (innField.Name == DocumentFieldNames.Inn || innField.Name == "CustomerINN" ||
                     innField.Name == "ContractorINN" || innField.Name == "SupplierINN" ||
                     innField.Name == "BuyerINN") && innField.Value == inn) &&
                !field.Document.Fields.Any(innField =>
                    (innField.Name == DocumentFieldNames.Inn || innField.Name == "CustomerINN" ||
                     innField.Name == "ContractorINN" || innField.Name == "SupplierINN" ||
                     innField.Name == "BuyerINN") && innField.Value != null && innField.Value != inn))
            .Select(field => field.Value!.Trim())
            .Where(value => value != "")
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync(cancellationToken);

        // All input values are parameters; invalid/legacy Amount strings are never cast.
        var amount = await dbContext.Database.SqlQuery<CounterpartyAmountAggregate>($$"""
            SELECT COALESCE(SUM(CASE
                WHEN f."Value" ~ '^[+]?([0-9]{1,29}(\.[0-9]{0,29})?|\.[0-9]{1,29})$' THEN
                    CASE WHEN f."Value"::numeric > 0 AND f."Value"::numeric <= 79228162514264337593543950335 THEN f."Value"::numeric ELSE 0 END
                ELSE 0 END), 0) AS "Total",
              COALESCE(SUM(CASE
                WHEN f."Value" ~ '^[+]?([0-9]{1,29}(\.[0-9]{0,29})?|\.[0-9]{1,29})$' THEN
                    CASE WHEN f."Value"::numeric > 0 AND f."Value"::numeric <= 79228162514264337593543950335 THEN 1 ELSE 0 END
                ELSE 0 END), 0)::integer AS "Count"
            FROM "DocumentFields" f
            WHERE f."Name" = 'Amount' AND EXISTS (
              SELECT 1 FROM "DocumentFields" i
              WHERE i."DocumentId" = f."DocumentId" AND i."Name" IN
                ('INN', 'CustomerINN', 'ContractorINN', 'SupplierINN', 'BuyerINN') AND i."Value" = {{inn}})
            """).SingleAsync(cancellationToken);

        var ordered = documents.OrderByDescending(document => document.CreatedAtUtc)
            .ThenBy(document => document.Id);
        var latest = await ProjectCounterpartyDocuments(ordered.Take(5)).ToListAsync(cancellationToken);
        var items = page == 1 && pageSize <= 5
            ? latest.Take(pageSize).ToList()
            : await ProjectCounterpartyDocuments(ordered.Skip((page - 1) * pageSize)
                .Take(pageSize)).ToListAsync(cancellationToken);

        return new CounterpartyCard(inn, knownKpps, count, countsByType, countsByStatus,
            countsByStatus[DocumentStatus.NeedsReview], amount.Total, amount.Count,
            latest, items, page, pageSize);
    }

    private static IQueryable<CounterpartyDocumentItem> ProjectCounterpartyDocuments(IQueryable<Document> documents) =>
        documents.Select(document => new CounterpartyDocumentItem(
            document.Id, document.OriginalFileName, document.Type, document.Status,
            document.Fields.Where(field => field.Name == DocumentFieldNames.Number)
                .Select(field => field.Value).FirstOrDefault(),
            document.Fields.Any(field =>
                (field.Name == DocumentFieldNames.Inn || field.Name == "CustomerINN" ||
                 field.Name == "ContractorINN" || field.Name == "SupplierINN" ||
                 field.Name == "BuyerINN") && field.Value != null &&
                document.Fields.Any(other =>
                    (other.Name == DocumentFieldNames.Inn || other.Name == "CustomerINN" ||
                     other.Name == "ContractorINN" || other.Name == "SupplierINN" ||
                     other.Name == "BuyerINN") && other.Value != null && other.Value != field.Value))
                ? null : document.Fields.Where(field => field.Name == DocumentFieldNames.Kpp)
                    .Select(field => field.Value).FirstOrDefault(),
            document.Fields.Where(field => field.Name == DocumentFieldNames.Amount)
                .Select(field => field.Value).FirstOrDefault(),
            document.CreatedAtUtc, document.IsArchived,
            document.Fields.Where(field => field.Name == "CustomerINN").Select(field => field.Value).FirstOrDefault(),
            document.Fields.Where(field => field.Name == "ContractorINN").Select(field => field.Value).FirstOrDefault(),
            document.Fields.Where(field => field.Name == "SupplierINN").Select(field => field.Value).FirstOrDefault(),
            document.Fields.Where(field => field.Name == "BuyerINN").Select(field => field.Value).FirstOrDefault()));

    public sealed class CounterpartyAmountAggregate
    {
        public decimal Total { get; set; }
        public int Count { get; set; }
    }

    public async Task<RelatedDocumentsResult?> GetRelatedAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var source = await dbContext.Documents.AsNoTracking()
            .Where(document => document.Id == documentId)
            .Select(document => new
            {
                document.Type,
                ReferenceNumber = document.Fields
                    .Where(field => field.Name == (document.Type == DocumentType.Act
                        ? DocumentFieldNames.RelatedContractNumber : DocumentFieldNames.Number))
                    .Select(field => field.Value)
                    .FirstOrDefault()
            }).SingleOrDefaultAsync(cancellationToken);
        if (source is null) return null;

        var reference = source.ReferenceNumber?.Trim();
        if (source.Type is not (DocumentType.Act or DocumentType.Contract) ||
            string.IsNullOrWhiteSpace(reference))
            return new RelatedDocumentsResult(documentId, source.Type, reference, []);

        var candidateType = source.Type == DocumentType.Act ? DocumentType.Contract : DocumentType.Act;
        var candidateField = source.Type == DocumentType.Act
            ? DocumentFieldNames.Number : DocumentFieldNames.RelatedContractNumber;
        var exactPattern = EscapeLike(reference);
        var items = await dbContext.Documents.AsNoTracking()
            .Where(document => document.Type == candidateType && document.Fields.Any(field =>
                field.Name == candidateField && field.Value != null &&
                EF.Functions.ILike(field.Value.Trim(), exactPattern, "\\")))
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenBy(document => document.Id)
            .Select(document => new RelatedDocumentItem(
                document.Id,
                document.OriginalFileName,
                document.Type,
                document.Status,
                document.Fields.Where(field => field.Name == DocumentFieldNames.Number)
                    .Select(field => field.Value).FirstOrDefault(),
                document.Fields.Where(field => field.Name == DocumentFieldNames.Date)
                    .Select(field => field.Value).FirstOrDefault(),
                document.Fields.Where(field => field.Name == DocumentFieldNames.Amount)
                    .Select(field => field.Value).FirstOrDefault(),
                document.IsArchived))
            .ToListAsync(cancellationToken);
        return new RelatedDocumentsResult(documentId, source.Type, reference, items);
    }

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
