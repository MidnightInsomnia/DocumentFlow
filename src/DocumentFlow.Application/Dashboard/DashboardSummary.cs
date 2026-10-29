namespace DocumentFlow.Application.Dashboard;

public sealed record DashboardSummary(
    int TotalDocuments,
    DashboardStatusCounts Statuses,
    DashboardTypeCounts Types,
    DashboardDepartmentCounts Departments,
    int AutomaticallyCompleted,
    int ManuallyCompleted,
    decimal AutomationRate,
    decimal ManualReviewRate,
    decimal FailureRate,
    int DocumentsLast7Days,
    IReadOnlyList<DailyDocumentCount> DailyDocuments,
    int ArchivedDocuments = 0);

public sealed record DashboardStatusCounts(
    int PendingProcessing,
    int Processing,
    int NeedsReview,
    int Completed,
    int Failed);

public sealed record DashboardTypeCounts(
    int Unknown,
    int Invoice,
    int Contract,
    int Act);

public sealed record DashboardDepartmentCounts(
    int Unassigned,
    int Accounting,
    int Legal);

public sealed record DailyDocumentCount(DateOnly Date, int Count);
