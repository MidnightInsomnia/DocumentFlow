using DocumentFlow.Infrastructure.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Api.Controllers;

public abstract class WorkspaceControllerBase : ControllerBase
{
    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action, int successStatus = 200)
    {
        try { return StatusCode(successStatus, await action()); }
        catch (WorkspaceException ex) { return Problem(statusCode: ex.StatusCode, title: "Ошибка запроса", detail: ex.Message); }
        catch (SemanticUnavailableException ex)
        {
            var problem = new ProblemDetails { Status = 503, Title = "Семантический поиск недоступен", Detail = ex.Message };
            problem.Extensions["semanticAvailable"] = false;
            problem.Extensions["code"] = "semantic_unavailable";
            return StatusCode(503, problem);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" or "23503" })
        { return Problem(statusCode: 409, title: "Конфликт данных", detail: "Запись уже существует или используется. Обновите данные и повторите запрос."); }
    }
}

[ApiController]
[Authorize]
[Route("api/catalogs")]
public sealed class CatalogsController(CatalogService catalogs) : WorkspaceControllerBase
{
    [HttpGet]
    public Task<IActionResult> Get(CancellationToken ct) => ExecuteAsync(() => catalogs.GetAsync(User.IsInRole("Admin"), ct));
}
