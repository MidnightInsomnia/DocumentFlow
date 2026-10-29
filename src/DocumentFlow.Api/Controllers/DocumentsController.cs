using DocumentFlow.Api.Requests.Documents;
using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Upload;
using DocumentFlow.Application.Documents.Read;
using DocumentFlow.Application.Documents.Files;
using DocumentFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    DocumentUploadService uploadService,
    DocumentReadService readService,
    DocumentFileService fileService,
    ILogger<DocumentsController>? logger = null)
    : ControllerBase
{
    private readonly ILogger<DocumentsController> _logger = logger ?? NullLogger<DocumentsController>.Instance;
    [HttpGet]
    [ProducesResponseType<DocumentListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? department = null,
        [FromQuery] string? search = null,
        [FromQuery] string? createdFrom = null,
        [FromQuery] string? createdTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? archive = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
        {
            return BadRequestProblem(
                "Document query validation failed",
                "Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        if (!TryParseOptionalEnum(status, out DocumentStatus? parsedStatus) ||
            !TryParseOptionalEnum(type, out DocumentType? parsedType) ||
            !TryParseOptionalEnum(department, out Department? parsedDepartment))
        {
            return BadRequestProblem(
                "Document query validation failed",
                "One or more enum filters have invalid values.");
        }

        var normalizedSearch = search?.Trim();
        if (normalizedSearch?.Length > 100)
        {
            return BadRequestProblem(
                "Document query validation failed",
                "Search must not exceed 100 characters.");
        }

        if (normalizedSearch?.Length == 0)
        {
            normalizedSearch = null;
        }

        if (!TryParseOptionalDate(createdFrom, out var parsedCreatedFrom) ||
            !TryParseOptionalDate(createdTo, out var parsedCreatedTo))
        {
            return BadRequestProblem(
                "Document query validation failed",
                "createdFrom and createdTo must use the yyyy-MM-dd format.");
        }

        if (parsedCreatedFrom > parsedCreatedTo)
        {
            return BadRequestProblem(
                "Document query validation failed",
                "createdFrom must be less than or equal to createdTo.");
        }

        if (!TryParseSortField(sortBy, out var parsedSortBy) ||
            !TryParseSortDirection(sortDirection, out var parsedSortDirection))
        {
            return BadRequestProblem(
                "Document query validation failed",
                "sortBy and sortDirection have invalid values.");
        }

        if (parsedCreatedTo == DateOnly.MaxValue)
        {
            return BadRequestProblem(
                "Document query validation failed",
                "createdTo is outside the supported date range.");
        }

        if ((archive is null && Request.Query.ContainsKey("archive")) ||
            !TryParseArchiveFilter(archive, out var parsedArchive))
        {
            return BadRequestProblem("Document query validation failed", "archive must be active, archived, or all.");
        }

        var result = await readService.GetPageAsync(
            new DocumentListQuery(
                page,
                pageSize,
                parsedStatus,
                parsedType,
                parsedDepartment,
                normalizedSearch,
                ToUtcStart(parsedCreatedFrom),
                parsedCreatedTo.HasValue
                    ? ToUtcStart(parsedCreatedTo.Value.AddDays(1))
                    : null,
                parsedSortBy,
                parsedSortDirection,
                parsedArchive),
            cancellationToken);

        return Ok(new DocumentListResponse(
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.Items.Select(item => new DocumentListItemResponse(
                item.Id,
                item.FileName,
                item.Status.ToString(),
                item.Type.ToString(),
                item.ClassificationConfidence,
                item.TargetDepartment.ToString(),
                item.CreatedAtUtc,
                item.ProcessedAtUtc,
                item.IsArchived,
                item.MatchReason, item.Number, item.Date, item.Inn, item.Amount,
                item.CustomerINN, item.ContractorINN, item.SupplierINN, item.BuyerINN)).ToList()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await readService.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        return Ok(new DocumentDetailsResponse(
            document.Id,
            document.FileName,
            document.ContentType,
            document.FileSizeBytes,
            document.Status.ToString(),
            document.Type.ToString(),
            document.ClassificationConfidence,
            document.TargetDepartment.ToString(),
            document.ExtractedText,
            document.ProcessingError,
            document.CreatedAtUtc,
            document.ProcessingStartedAtUtc,
            document.ProcessedAtUtc,
            document.Fields.Select(field => new DocumentFieldResponse(
                field.Id,
                field.Name,
                field.Value,
                field.Confidence,
                field.WasManuallyEdited,
                field.CreatedAtUtc,
                field.UpdatedAtUtc)).ToList(),
            document.Events.Select(documentEvent => new DocumentEventResponse(
                documentEvent.Type.ToString(),
                documentEvent.Message,
                documentEvent.CreatedAtUtc,
                documentEvent.ActorUserId,
                documentEvent.ActorDisplayName)).ToList(),
            document.IsArchived,
            document.ArchivedAtUtc,
            document.ArchivedByDisplayName, document.Version));
    }

    [HttpGet("{id:guid}/file")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await fileService.GetAsync(id, cancellationToken);
        if (file is null)
        {
            return DocumentFileNotFound();
        }

        Response.Headers.ContentDisposition =
            $"inline; filename*=UTF-8''{Uri.EscapeDataString(file.FileName)}";

        return File(
            file.Content,
            file.ContentType,
            enableRangeProcessing: true);
    }

    [HttpGet("{id:guid}/file/download")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await fileService.GetAsync(id, cancellationToken);
        if (file is null)
        {
            return DocumentFileNotFound();
        }

        return File(
            file.Content,
            file.ContentType,
            file.FileName,
            enableRangeProcessing: true);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadDocumentResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointDescription("Exact binary duplicate detected: 409 ProblemDetails with code document_duplicate and existingDocument.")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Document upload validation failed",
                Detail = "A file is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            await using var content = request.File.OpenReadStream();
            var result = await uploadService.UploadAsync(
                new UploadDocumentCommand(
                    content,
                    request.File.FileName,
                    request.File.ContentType,
                    request.File.Length),
                cancellationToken);

            return Accepted(new UploadDocumentResponse(
                result.Id,
                result.OriginalFileName,
                result.Status.ToString(),
                result.CreatedAtUtc));
        }
        catch (DocumentUploadValidationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Document upload validation failed",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (DuplicateDocumentException exception)
        {
            return Conflict(DuplicateProblem(exception));
        }
    }

    private static ProblemDetails DuplicateProblem(DuplicateDocumentException exception)
    {
        var problem = new ProblemDetails
        {
            Title = "Duplicate document",
            Detail = exception.Message,
            Status = StatusCodes.Status409Conflict
        };
        problem.Extensions["code"] = "document_duplicate";
        problem.Extensions["existingDocument"] = new
        {
            id = exception.ExistingDocumentId,
            fileName = exception.ExistingFileName,
            contentHash = exception.ContentHash,
            isArchived = exception.ExistingIsArchived
        };
        return problem;
    }

    [HttpPost("batch")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<BatchUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [EndpointDescription("Each file is uploaded independently. Exact binary duplicates return per-item status duplicate, not an HTTP batch failure.")]
    public async Task<IActionResult> UploadBatch(
        [FromForm] UploadDocumentBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
            return BadRequestProblem("Document upload validation failed", "At least one file is required.");

        var items = new List<BatchUploadItemResponse>(request.Files.Count);
        foreach (var file in request.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var content = file.OpenReadStream();
                var result = await uploadService.UploadAsync(new UploadDocumentCommand(
                    content, file.FileName, file.ContentType, file.Length), cancellationToken);
                items.Add(new BatchUploadItemResponse(file.FileName, "uploaded", DocumentId: result.Id));
            }
            catch (DuplicateDocumentException duplicate)
            {
                items.Add(new BatchUploadItemResponse(file.FileName, "duplicate",
                    ExistingDocumentId: duplicate.ExistingDocumentId,
                    ExistingFileName: duplicate.ExistingFileName,
                    ExistingIsArchived: duplicate.ExistingIsArchived));
            }
            catch (DocumentUploadValidationException validation)
            {
                items.Add(new BatchUploadItemResponse(file.FileName, "failed", Error: validation.Message));
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Batch file upload failed. FileName={FileName}.", file.FileName);
                items.Add(new BatchUploadItemResponse(file.FileName, "failed", Error: "Ошибка загрузки файла."));
            }
        }
        return Ok(new BatchUploadResponse(items));
    }

    private BadRequestObjectResult BadRequestProblem(string title, string detail) =>
        BadRequest(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });

    private NotFoundObjectResult DocumentFileNotFound() =>
        NotFound(new ProblemDetails
        {
            Title = "Document file not found",
            Detail = "Document file was not found.",
            Status = StatusCodes.Status404NotFound
        });

    private static bool TryParseOptionalEnum<TEnum>(
        string? value,
        out TEnum? result)
        where TEnum : struct, Enum
    {
        if (value is null)
        {
            result = null;
            return true;
        }

        if (Enum.GetNames<TEnum>().Any(name => string.Equals(
                name,
                value,
                StringComparison.OrdinalIgnoreCase)) &&
            Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryParseOptionalDate(string? value, out DateOnly? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryParseArchiveFilter(string? value, out DocumentArchiveFilter result)
    {
        result = DocumentArchiveFilter.Active;
        if (value is null || string.Equals(value, "active", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(value, "archived", StringComparison.OrdinalIgnoreCase))
        {
            result = DocumentArchiveFilter.Archived;
            return true;
        }
        if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
        {
            result = DocumentArchiveFilter.All;
            return true;
        }
        return false;
    }

    private static bool TryParseSortField(string? value, out DocumentSortField result)
    {
        result = DocumentSortField.CreatedAt;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.ToLowerInvariant() switch
        {
            "createdat" => true,
            "filename" => SetSortField(DocumentSortField.FileName, out result),
            "status" => SetSortField(DocumentSortField.Status, out result),
            "type" => SetSortField(DocumentSortField.Type, out result),
            _ => false
        };
    }

    private static bool TryParseSortDirection(
        string? value,
        out DocumentSortDirection result)
    {
        result = DocumentSortDirection.Descending;
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase))
        {
            result = DocumentSortDirection.Ascending;
            return true;
        }

        return false;
    }

    private static bool SetSortField(
        DocumentSortField value,
        out DocumentSortField result)
    {
        result = value;
        return true;
    }

    private static DateTime? ToUtcStart(DateOnly? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
        : null;
}
