using DocumentFlow.Api.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Operator")]
[Route("api/dashboard/health")]
public sealed class DashboardHealthController(DashboardHealthService health) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var snapshot = await health.GetAsync(cancellationToken);
        // Project a fresh, minimal payload after the shared cache lookup. Never cache role-specific JSON.
        return User.IsInRole("Admin") ? Ok(snapshot) : Ok(new { snapshot.Overall });
    }
}
