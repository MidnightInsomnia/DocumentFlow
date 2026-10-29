namespace DocumentFlow.Api.Responses.Dashboard;

public sealed record DashboardResponse(
    int TotalDocuments,
    DashboardStatusCountsResponse Statuses,
    DashboardTypeCountsResponse Types,
    DashboardDepartmentCountsResponse Departments,
    int AutomaticallyCompleted,
    int ManuallyCompleted,
    decimal AutomationRate,
    decimal ManualReviewRate,
    decimal FailureRate,
    int DocumentsLast7Days,
    IReadOnlyList<DailyDocumentCountResponse> DailyDocuments,
    int ArchivedDocuments);

public sealed record DashboardStatusCountsResponse(
    int PendingProcessing,
    int Processing,
    int NeedsReview,
    int Completed,
    int Failed);

public sealed record DashboardTypeCountsResponse(
    int Unknown,
    int Invoice,
    int Contract,
    int Act);

public sealed record DashboardDepartmentCountsResponse(
    int Unassigned,
    int Accounting,
    int Legal);

public sealed record DailyDocumentCountResponse(DateOnly Date, int Count);
