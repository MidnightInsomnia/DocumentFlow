using DocumentFlow.Api.Responses.Dashboard;
using DocumentFlow.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard/summary")]
public sealed class OperationsDashboardController(IDashboardRepository repository, TimeProvider timeProvider)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<OperationsDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationsDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        // Boundaries are explicitly UTC; ProcessedAtUtc, not upload time, defines completion.
        var todayUtc = timeProvider.GetUtcNow().UtcDateTime.Date;
        var result = await repository.GetOperationsSummaryAsync(todayUtc, todayUtc.AddDays(1), cancellationToken);
        return Ok(new OperationsDashboardResponse(
            result.PendingProcessing,
            result.Processing,
            result.NeedsReview,
            result.Failed,
            result.CompletedToday,
            result.TotalActive,
            result.ArchivedTotal,
            result.RecentNeedsReview.Select(item => new RecentNeedsReviewResponse(
                item.Id, item.FileName, item.Type.ToString(), item.CreatedAtUtc)).ToList(),
            result.RecentFailed.Select(item => new RecentFailedResponse(
                item.Id, item.FileName, item.CreatedAtUtc, SafeFailureReason.Format(item.Reason))).ToList()));
    }
}
