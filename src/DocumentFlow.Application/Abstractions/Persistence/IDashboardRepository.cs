using DocumentFlow.Application.Dashboard;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDashboardRepository
{
    Task<OperationsDashboardData> GetOperationsSummaryAsync(
        DateTime todayStartUtcInclusive,
        DateTime tomorrowStartUtcExclusive,
        CancellationToken cancellationToken);

    Task<DashboardAggregateData> GetAggregateDataAsync(
        DateTime recentFromUtcInclusive,
        DateTime recentToUtcExclusive,
        CancellationToken cancellationToken);
}
