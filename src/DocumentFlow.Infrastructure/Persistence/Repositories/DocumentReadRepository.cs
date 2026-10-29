using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Documents.Read;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentReadRepository(DocumentFlowDbContext dbContext)
    : IDocumentReadRepository
{
    public async Task<PagedDocumentResult> GetPageAsync(
        DocumentListQuery query,
        CancellationToken cancellationToken)
    {
        var documents = ApplyFilters(dbContext.Documents.AsNoTracking(), query);
        var pattern = query.Search is null ? null : $"%{EscapeLikePattern(query.Search)}%";
        var amountVariants = query.Search is null ? [] : AmountSearchVariants(query.Search);

        var totalCount = await documents.CountAsync(cancellationToken);
        var orderedDocuments = ApplyOrdering(documents, query.SortBy, query.SortDirection);
        var pageRows = await orderedDocuments
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(document => new
            {
                document.Id,
                document.OriginalFileName,
                document.Status,
                document.Type,
                document.ClassificationConfidence,
                document.TargetDepartment,
                document.CreatedAtUtc,
                document.ProcessedAtUtc,
                document.IsArchived,
                Number = document.Fields.Where(field => field.Name == DocumentFieldNames.Number).Select(field => field.Value).FirstOrDefault(),
                Date = document.Fields.Where(field => field.Name == DocumentFieldNames.Date).Select(field => field.Value).FirstOrDefault(),
                Inn = document.Type == DocumentFlow.Domain.Enums.DocumentType.Invoice
                    ? document.Fields.Where(field => field.Name == "SupplierINN").Select(field => field.Value).FirstOrDefault()
                      ?? document.Fields.Where(field => field.Name == DocumentFieldNames.Inn).Select(field => field.Value).FirstOrDefault()
                    : document.Type == DocumentFlow.Domain.Enums.DocumentType.Act || document.Type == DocumentFlow.Domain.Enums.DocumentType.Contract
                        ? document.Fields.Where(field => field.Name == "ContractorINN").Select(field => field.Value).FirstOrDefault()
                        : null,
                CustomerINN = document.Fields.Where(field => field.Name == "CustomerINN").Select(field => field.Value).FirstOrDefault(),
                ContractorINN = document.Fields.Where(field => field.Name == "ContractorINN").Select(field => field.Value).FirstOrDefault(),
                SupplierINN = document.Fields.Where(field => field.Name == "SupplierINN").Select(field => field.Value).FirstOrDefault(),
                BuyerINN = document.Fields.Where(field => field.Name == "BuyerINN").Select(field => field.Value).FirstOrDefault(),
                Amount = document.Fields.Where(field => field.Name == DocumentFieldNames.Amount).Select(field => field.Value).FirstOrDefault(),
                FileNameMatches = pattern != null && EF.Functions.ILike(document.OriginalFileName, pattern, "\\"),
                MatchedField = pattern == null ? null : document.Fields
                    .Where(field => field.Value != null &&
                        (((field.Name == DocumentFieldNames.Number || field.Name == DocumentFieldNames.Inn ||
                           field.Name == "CustomerINN" || field.Name == "ContractorINN" ||
                           field.Name == "SupplierINN" || field.Name == "BuyerINN" ||
                           field.Name == DocumentFieldNames.Kpp || field.Name == DocumentFieldNames.RelatedContractNumber) &&
                          EF.Functions.ILike(field.Value, pattern, "\\")) ||
                         (field.Name == DocumentFieldNames.Amount &&
                          (amountVariants.Length != 0
                              ? amountVariants.Contains(field.Value)
                              : EF.Functions.ILike(field.Value, pattern, "\\")))))
                    .OrderBy(field => field.Name == DocumentFieldNames.Number ? 0 :
                        field.Name == DocumentFieldNames.Inn ? 1 :
                        field.Name == DocumentFieldNames.Kpp ? 2 :
                        field.Name == DocumentFieldNames.Amount ? 3 : 4)
                    .Select(field => field.Name + " = " + field.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = pageRows.Select(row => new DocumentListItem(
            row.Id, row.OriginalFileName, row.Status, row.Type,
            row.ClassificationConfidence, row.TargetDepartment, row.CreatedAtUtc,
            row.ProcessedAtUtc, row.IsArchived,
            pattern is null ? null : row.FileNameMatches
                ? $"Имя файла: {row.OriginalFileName}"
                : row.MatchedField is not null ? $"Поле {row.MatchedField}" : "Текст документа",
            row.Number, row.Date, row.Inn, row.Amount,
            row.CustomerINN, row.ContractorINN, row.SupplierINN, row.BuyerINN
        )).ToList();

        return new PagedDocumentResult(
            query.Page,
            query.PageSize,
            totalCount,
            items);
    }

    public static IQueryable<Document> ApplyFilters(IQueryable<Document> documents, DocumentListQuery query)
    {

        documents = query.Archive switch
        {
            DocumentArchiveFilter.Active => documents.Where(document => !document.IsArchived),
            DocumentArchiveFilter.Archived => documents.Where(document => document.IsArchived),
            DocumentArchiveFilter.All => documents,
            _ => throw new ArgumentOutOfRangeException(nameof(query))
        };

        if (query.Status.HasValue)
        {
            documents = documents.Where(document => document.Status == query.Status.Value);
        }

        if (query.Type.HasValue)
        {
            documents = documents.Where(document => document.Type == query.Type.Value);
        }

        if (query.Department.HasValue)
        {
            documents = documents.Where(document =>
                document.TargetDepartment == query.Department.Value);
        }

        var pattern = query.Search is null ? null : $"%{EscapeLikePattern(query.Search)}%";
        var amountVariants = query.Search is null ? [] : AmountSearchVariants(query.Search);
        if (pattern is not null)
        {
            documents = documents.Where(document =>
                EF.Functions.ILike(document.OriginalFileName, pattern, "\\") ||
                (document.ExtractedText != null &&
                 EF.Functions.ILike(document.ExtractedText, pattern, "\\")) ||
                document.Fields.Any(field =>
                    (field.Name == DocumentFieldNames.Number || field.Name == DocumentFieldNames.Inn ||
                     field.Name == "CustomerINN" || field.Name == "ContractorINN" ||
                     field.Name == "SupplierINN" || field.Name == "BuyerINN" ||
                     field.Name == DocumentFieldNames.Kpp || field.Name == DocumentFieldNames.RelatedContractNumber) &&
                    field.Value != null &&
                    EF.Functions.ILike(field.Value, pattern, "\\")) ||
                document.Fields.Any(field => field.Name == DocumentFieldNames.Amount &&
                    field.Value != null &&
                    (amountVariants.Length != 0
                        ? amountVariants.Contains(field.Value)
                        : EF.Functions.ILike(field.Value, pattern, "\\"))));
        }

        if (query.CreatedFromUtcInclusive.HasValue)
        {
            documents = documents.Where(document =>
                document.CreatedAtUtc >= query.CreatedFromUtcInclusive.Value);
        }

        if (query.CreatedToUtcExclusive.HasValue)
        {
            documents = documents.Where(document =>
                document.CreatedAtUtc < query.CreatedToUtcExclusive.Value);
        }

        return documents;
    }

    public static IOrderedQueryable<Document> ApplyOrdering(
        IQueryable<Document> documents,
        DocumentSortField sortBy,
        DocumentSortDirection direction)
    {
        var ascending = direction == DocumentSortDirection.Ascending;

        return (sortBy, ascending) switch
        {
            (DocumentSortField.FileName, true) => documents
                .OrderBy(document => document.OriginalFileName)
                .ThenBy(document => document.Id),
            (DocumentSortField.FileName, false) => documents
                .OrderByDescending(document => document.OriginalFileName)
                .ThenByDescending(document => document.Id),
            (DocumentSortField.Status, true) => documents
                .OrderBy(document => document.Status)
                .ThenBy(document => document.Id),
            (DocumentSortField.Status, false) => documents
                .OrderByDescending(document => document.Status)
                .ThenByDescending(document => document.Id),
            (DocumentSortField.Type, true) => documents
                .OrderBy(document => document.Type)
                .ThenBy(document => document.Id),
            (DocumentSortField.Type, false) => documents
                .OrderByDescending(document => document.Type)
                .ThenByDescending(document => document.Id),
            (_, true) => documents
                .OrderBy(document => document.CreatedAtUtc)
                .ThenBy(document => document.Id),
            _ => documents
                .OrderByDescending(document => document.CreatedAtUtc)
                .ThenByDescending(document => document.Id)
        };
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static string[] AmountSearchVariants(string search)
    {
        var normalized = DocumentFlow.Application.Documents.Processing.DocumentFieldSchema.NormalizeAmount(search);
        if (!Regex.IsMatch(normalized, @"^[0-9]+(\.[0-9]{1,2})?$", RegexOptions.CultureInvariant) ||
            !decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount))
            return [];

        return new[] { amount.ToString("0.##", CultureInfo.InvariantCulture),
                amount.ToString("0.0", CultureInfo.InvariantCulture),
                amount.ToString("0.00", CultureInfo.InvariantCulture) }
            .SelectMany(value => new[] { value, value.Replace('.', ',') })
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public Task<DocumentDetails?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == id)
            .Select(document => new DocumentDetails(
                document.Id,
                document.OriginalFileName,
                document.ContentType,
                document.FileSizeBytes,
                document.Status,
                document.Type,
                document.ClassificationConfidence,
                document.TargetDepartment,
                document.ExtractedText,
                document.ProcessingError,
                document.CreatedAtUtc,
                document.ProcessingStartedAtUtc,
                document.ProcessedAtUtc,
                document.Fields
                    .OrderBy(field => field.Name)
                    .Select(field => new DocumentFieldDetails(
                        field.Id,
                        field.Name,
                        field.Value,
                        field.Confidence,
                        field.WasManuallyEdited,
                        field.CreatedAtUtc,
                        field.UpdatedAtUtc))
                    .ToList(),
                document.Events
                    .OrderBy(documentEvent => documentEvent.CreatedAtUtc)
                    .ThenBy(documentEvent => documentEvent.Type)
                    .ThenBy(documentEvent => documentEvent.Id)
                    .Select(documentEvent => new DocumentEventDetails(
                        documentEvent.Type,
                        documentEvent.Message,
                        documentEvent.CreatedAtUtc,
                        documentEvent.ActorUserId,
                        documentEvent.ActorDisplayName))
                    .ToList(),
                document.IsArchived,
                document.ArchivedAtUtc,
                document.ArchivedByDisplayName,
                document.Version.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
