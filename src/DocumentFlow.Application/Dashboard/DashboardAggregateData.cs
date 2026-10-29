using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Dashboard;

public sealed record DashboardAggregateData(
    IReadOnlyDictionary<DocumentStatus, int> Statuses,
    IReadOnlyDictionary<DocumentType, int> Types,
    IReadOnlyDictionary<Department, int> Departments,
    int AutomaticallyCompleted,
    int ManuallyCompleted,
    IReadOnlyDictionary<DateOnly, int> DailyDocuments,
    int ArchivedDocuments = 0);
