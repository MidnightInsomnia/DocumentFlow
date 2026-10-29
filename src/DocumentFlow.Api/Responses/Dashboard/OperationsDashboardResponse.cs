namespace DocumentFlow.Api.Responses.Dashboard;

public sealed record OperationsDashboardResponse(
    int PendingProcessing,
    int Processing,
    int NeedsReview,
    int Failed,
    int CompletedToday,
    int TotalActive,
    int ArchivedTotal,
    IReadOnlyList<RecentNeedsReviewResponse> RecentNeedsReview,
    IReadOnlyList<RecentFailedResponse> RecentFailed);

public sealed record RecentNeedsReviewResponse(Guid Id, string FileName, string Type, DateTime CreatedAtUtc);
public sealed record RecentFailedResponse(Guid Id, string FileName, DateTime CreatedAtUtc, string Reason);
