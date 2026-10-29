using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Dashboard;

public sealed record OperationsDashboardData(
    int PendingProcessing,
    int Processing,
    int NeedsReview,
    int Failed,
    int CompletedToday,
    int TotalActive,
    int ArchivedTotal,
    IReadOnlyList<RecentNeedsReviewData> RecentNeedsReview,
    IReadOnlyList<RecentFailedData> RecentFailed);

public sealed record RecentNeedsReviewData(Guid Id, string FileName, DocumentType Type, DateTime CreatedAtUtc);
public sealed record RecentFailedData(Guid Id, string FileName, DateTime CreatedAtUtc, string? Reason);
