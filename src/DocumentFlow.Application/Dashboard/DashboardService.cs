using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Dashboard;

public sealed class DashboardService(
    IDashboardRepository repository,
    TimeProvider timeProvider)
{
    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var firstDay = today.AddDays(-6);
        var fromUtc = DateTime.SpecifyKind(
            firstDay.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(
            today.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var data = await repository.GetAggregateDataAsync(
            fromUtc,
            toUtc,
            cancellationToken);

        var statuses = new DashboardStatusCounts(
            GetCount(data.Statuses, DocumentStatus.PendingProcessing),
            GetCount(data.Statuses, DocumentStatus.Processing),
            GetCount(data.Statuses, DocumentStatus.NeedsReview),
            GetCount(data.Statuses, DocumentStatus.Completed),
            GetCount(data.Statuses, DocumentStatus.Failed));
        var types = new DashboardTypeCounts(
            GetCount(data.Types, DocumentType.Unknown),
            GetCount(data.Types, DocumentType.Invoice),
            GetCount(data.Types, DocumentType.Contract),
            GetCount(data.Types, DocumentType.Act));
        var departments = new DashboardDepartmentCounts(
            GetCount(data.Departments, Department.Unassigned),
            GetCount(data.Departments, Department.Accounting),
            GetCount(data.Departments, Department.Legal));

        var dailyDocuments = Enumerable.Range(0, 7)
            .Select(offset => firstDay.AddDays(offset))
            .Select(date => new DailyDocumentCount(
                date,
                data.DailyDocuments.GetValueOrDefault(date)))
            .ToList();
        var completed = data.AutomaticallyCompleted + data.ManuallyCompleted;
        var totalDocuments = data.Statuses.Values.Sum();

        return new DashboardSummary(
            totalDocuments,
            statuses,
            types,
            departments,
            data.AutomaticallyCompleted,
            data.ManuallyCompleted,
            Percentage(data.AutomaticallyCompleted, completed),
            Percentage(data.ManuallyCompleted, completed),
            Percentage(statuses.Failed, totalDocuments),
            dailyDocuments.Sum(item => item.Count),
            dailyDocuments,
            data.ArchivedDocuments);
    }

    private static int GetCount<T>(IReadOnlyDictionary<T, int> counts, T key)
        where T : notnull => counts.GetValueOrDefault(key);

    private static decimal Percentage(int numerator, int denominator) =>
        denominator == 0
            ? 0m
            : Math.Round(
                numerator * 100m / denominator,
                1,
                MidpointRounding.AwayFromZero);
}
