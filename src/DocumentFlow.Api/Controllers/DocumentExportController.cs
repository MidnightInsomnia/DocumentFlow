using System.Globalization;
using System.Text;
using DocumentFlow.Application.Documents.Read;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using DocumentFlow.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents/export")]
public sealed class DocumentExportController(DocumentFlowDbContext db) : ControllerBase
{
    private const int MaximumRows = 10000;

    // Export is intentionally restricted to Completed results; unconfirmed OCR/review values
    // must never be presented as confirmed business details, even when other statuses are filtered.
    [HttpGet]
    public async Task<IActionResult> Export(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? department = null,
        [FromQuery] string? search = null,
        [FromQuery] string? createdFrom = null,
        [FromQuery] string? createdTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? archive = null,
        [FromQuery] string? inn = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryEnum(status, out DocumentStatus? parsedStatus) ||
            !TryEnum(type, out DocumentType? parsedType) ||
            !TryEnum(department, out Department? parsedDepartment))
            return Invalid("Некорректный фильтр статуса, типа или отдела.");
        var normalizedSearch = search?.Trim();
        if (normalizedSearch?.Length > 100) return Invalid("Поиск не должен превышать 100 символов.");
        if (normalizedSearch?.Length == 0) normalizedSearch = null;
        if (!TryDate(createdFrom, out var from) || !TryDate(createdTo, out var to) ||
            from > to || to == DateOnly.MaxValue)
            return Invalid("Укажите корректный диапазон дат в формате ГГГГ-ММ-ДД.");
        var archiveFilter = archive?.ToLowerInvariant() switch
        {
            null or "active" => DocumentArchiveFilter.Active,
            "archived" => DocumentArchiveFilter.Archived,
            "all" => DocumentArchiveFilter.All,
            _ => (DocumentArchiveFilter)(-1)
        };
        if ((archive is null && Request.Query.ContainsKey("archive")) || (int)archiveFilter < 0)
            return Invalid("archive должен быть active, archived или all.");
        var sortField = sortBy?.ToLowerInvariant() switch
        {
            null or "" or "createdat" => DocumentSortField.CreatedAt,
            "filename" => DocumentSortField.FileName,
            "status" => DocumentSortField.Status,
            "type" => DocumentSortField.Type,
            _ => (DocumentSortField)(-1)
        };
        var direction = sortDirection?.ToLowerInvariant() switch
        {
            null or "" or "desc" => DocumentSortDirection.Descending,
            "asc" => DocumentSortDirection.Ascending,
            _ => (DocumentSortDirection)(-1)
        };
        if ((int)sortField < 0 || (int)direction < 0) return Invalid("Некорректный порядок сортировки.");
        if (inn is not null && (inn.Length is not (10 or 12) || !inn.All(char.IsAsciiDigit)))
            return Invalid("ИНН должен содержать 10 или 12 цифр.");
        var query = new DocumentListQuery(1, MaximumRows, parsedStatus, parsedType, parsedDepartment,
            normalizedSearch, UtcStart(from), to.HasValue ? UtcStart(to.Value.AddDays(1)) : null,
            sortField, direction, archiveFilter);
        var documents = DocumentReadRepository.ApplyFilters(db.Documents.AsNoTracking(), query)
            .Where(item => item.Status == DocumentStatus.Completed);
        if (inn is not null)
            documents = documents.Where(item => item.Fields.Any(field =>
                (field.Name == DocumentFieldNames.Inn || field.Name == "CustomerINN" ||
                 field.Name == "ContractorINN" || field.Name == "SupplierINN" || field.Name == "BuyerINN") &&
                field.Value == inn)); // inn means any party, including legacy rows.
        var rows = await DocumentReadRepository.ApplyOrdering(documents, sortField, direction)
            .Take(MaximumRows + 1)
            .Select(document => new
            {
                document.Id, document.OriginalFileName, document.Status, document.Type, document.TargetDepartment,
                document.CreatedAtUtc, document.ProcessedAtUtc, document.IsArchived,
                Number = document.Fields.Where(field => field.Name == DocumentFieldNames.Number).Select(field => field.Value).FirstOrDefault(),
                Date = document.Fields.Where(field => field.Name == DocumentFieldNames.Date).Select(field => field.Value).FirstOrDefault(),
                Inn = document.Type == DocumentType.Invoice
                    ? document.Fields.Where(field => field.Name == "SupplierINN").Select(field => field.Value).FirstOrDefault()
                      ?? document.Fields.Where(field => field.Name == DocumentFieldNames.Inn).Select(field => field.Value).FirstOrDefault()
                    : document.Type == DocumentType.Act || document.Type == DocumentType.Contract
                        ? document.Fields.Where(field => field.Name == "ContractorINN").Select(field => field.Value).FirstOrDefault()
                        : null,
                CustomerINN = document.Fields.Where(field => field.Name == "CustomerINN").Select(field => field.Value).FirstOrDefault(),
                ContractorINN = document.Fields.Where(field => field.Name == "ContractorINN").Select(field => field.Value).FirstOrDefault(),
                SupplierINN = document.Fields.Where(field => field.Name == "SupplierINN").Select(field => field.Value).FirstOrDefault(),
                BuyerINN = document.Fields.Where(field => field.Name == "BuyerINN").Select(field => field.Value).FirstOrDefault(),
                Amount = document.Fields.Where(field => field.Name == DocumentFieldNames.Amount).Select(field => field.Value).FirstOrDefault(),
                Kpp = document.Fields.Where(field => field.Name == DocumentFieldNames.Kpp).Select(field => field.Value).FirstOrDefault(),
                RelatedContractNumber = document.Fields.Where(field => field.Name == DocumentFieldNames.RelatedContractNumber).Select(field => field.Value).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        if (rows.Count > MaximumRows)
            return Invalid($"Экспорт ограничен {MaximumRows} документами. Уточните фильтры; данные не были обрезаны.");

        var csv = new StringBuilder();
        csv.Append("Id,FileName,Status,Type,Number,Date,INN,Amount,KPP,RelatedContractNumber,TargetDepartment,CreatedAtUtc,ProcessedAtUtc,IsArchived,CustomerINN,ContractorINN,SupplierINN,BuyerINN\r\n");
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new[]
            {
                row.Id.ToString(), row.OriginalFileName, row.Status.ToString(), row.Type.ToString(),
                row.Number, row.Date, row.Inn, row.Amount, row.Kpp, row.RelatedContractNumber,
                row.TargetDepartment.ToString(), row.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                row.ProcessedAtUtc?.ToString("O", CultureInfo.InvariantCulture), row.IsArchived ? "true" : "false",
                row.CustomerINN, row.ContractorINN, row.SupplierINN, row.BuyerINN
            };
            csv.Append(string.Join(",", values.Select(EscapeCsv))).Append("\r\n");
        }
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(csv.ToString())).ToArray();
        Response.Headers.CacheControl = "no-store";
        return File(bytes, "text/csv; charset=utf-8", $"documents-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static string EscapeCsv(string? source)
    {
        var value = source ?? "";
        var offset = 0;
        while (offset < value.Length && (char.IsWhiteSpace(value[offset]) || char.IsControl(value[offset]) || value[offset] == '\uFEFF'))
            offset++;
        // Quote cells for CSV structure, and neutralize formulas independently of CSV escaping.
        if ((offset < value.Length && value[offset] is '=' or '+' or '-' or '@') ||
            (value.Length > 0 && value[0] is '\t' or '\r' or '\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private BadRequestObjectResult Invalid(string detail) => BadRequest(new ProblemDetails
    {
        Title = "Некорректные параметры экспорта", Detail = detail, Status = StatusCodes.Status400BadRequest
    });

    private static bool TryEnum<T>(string? text, out T? result) where T : struct, Enum
    {
        result = null;
        if (text is null) return true;
        if (!Enum.GetNames<T>().Any(name => name.Equals(text, StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse<T>(text, true, out var parsed)) return false;
        result = parsed;
        return true;
    }
    private static bool TryDate(string? text, out DateOnly? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        result = parsed;
        return true;
    }
    private static DateTime? UtcStart(DateOnly? date) => date.HasValue
        ? DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
}
