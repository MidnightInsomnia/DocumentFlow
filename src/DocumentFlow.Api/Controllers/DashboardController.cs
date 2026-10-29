using DocumentFlow.Api.Responses.Dashboard;
using DocumentFlow.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardService dashboardService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(
        CancellationToken cancellationToken)
    {
        var summary = await dashboardService.GetSummaryAsync(cancellationToken);

        return Ok(new DashboardResponse(
            summary.TotalDocuments,
            new DashboardStatusCountsResponse(
                summary.Statuses.PendingProcessing,
                summary.Statuses.Processing,
                summary.Statuses.NeedsReview,
                summary.Statuses.Completed,
                summary.Statuses.Failed),
            new DashboardTypeCountsResponse(
                summary.Types.Unknown,
                summary.Types.Invoice,
                summary.Types.Contract,
                summary.Types.Act),
            new DashboardDepartmentCountsResponse(
                summary.Departments.Unassigned,
                summary.Departments.Accounting,
                summary.Departments.Legal),
            summary.AutomaticallyCompleted,
            summary.ManuallyCompleted,
            summary.AutomationRate,
            summary.ManualReviewRate,
            summary.FailureRate,
            summary.DocumentsLast7Days,
            summary.DailyDocuments
                .Select(item => new DailyDocumentCountResponse(item.Date, item.Count))
                .ToList(),
            summary.ArchivedDocuments));
    }
}
