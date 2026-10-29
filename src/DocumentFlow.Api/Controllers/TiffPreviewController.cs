using DocumentFlow.Application.Documents.Files;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Infrastructure.Processing.TextExtraction;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

// Resolve stored files only through their Document Id; no caller-supplied paths or temporary PNG files.
[ApiController]
[Route("api/documents/{id:guid}/preview/pages")]
public sealed class TiffPreviewController(DocumentFileService files, TiffRasterizer rasterizer) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPages(Guid id, CancellationToken cancellationToken)
    {
        var file = await files.GetAsync(id, cancellationToken);
        if (file is null) return NotFound();
        await using var stream = file.Content;
        if (!IsTiff(file.ContentType)) return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        try
        {
            using var image = rasterizer.Load(stream, cancellationToken);
            return Ok(new TiffPreviewPagesResponse(image.PageCount));
        }
        catch (TextExtractionException exception)
        {
            return UnprocessableEntity(PreviewProblem(exception.Message));
        }
    }

    [HttpGet("{page:int}")]
    [Produces("image/png")]
    public async Task<IActionResult> GetPage(Guid id, int page, CancellationToken cancellationToken)
    {
        if (page < 1) return BadRequest(PreviewProblem("Номер страницы должен быть положительным.", StatusCodes.Status400BadRequest));
        var file = await files.GetAsync(id, cancellationToken);
        if (file is null) return NotFound();
        await using var stream = file.Content;
        if (!IsTiff(file.ContentType)) return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        try
        {
            using var image = rasterizer.Load(stream, cancellationToken);
            if (page > image.PageCount) return NotFound();
            await using var png = await image.RenderPngAsync(page, cancellationToken);
            return File(png.ToArray(), "image/png");
        }
        catch (TextExtractionException exception)
        {
            return UnprocessableEntity(PreviewProblem(exception.Message));
        }
    }

    private static bool IsTiff(string type) =>
        string.Equals(type, "image/tiff", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "image/tif", StringComparison.OrdinalIgnoreCase);

    private static ProblemDetails PreviewProblem(string detail, int status = StatusCodes.Status422UnprocessableEntity) => new()
    {
        Title = "Не удалось сформировать предварительный просмотр TIFF",
        Detail = detail,
        Status = status
    };
}

public sealed record TiffPreviewPagesResponse(int PageCount);
