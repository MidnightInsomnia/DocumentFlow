using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Dashboard;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository(DocumentFlowDbContext dbContext)
    : IDashboardRepository
{
    public async Task<OperationsDashboardData> GetOperationsSummaryAsync(
        DateTime todayStartUtcInclusive,
        DateTime tomorrowStartUtcExclusive,
        CancellationToken cancellationToken)
    {
        var documents = dbContext.Documents.AsNoTracking();
        var active = documents.Where(document => !document.IsArchived);
        var statuses = await active
            .GroupBy(document => document.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var archivedTotal = await documents.CountAsync(document => document.IsArchived, cancellationToken);
        var completedToday = await active.CountAsync(document =>
            document.Status == DocumentStatus.Completed &&
            document.ProcessedAtUtc >= todayStartUtcInclusive &&
            document.ProcessedAtUtc < tomorrowStartUtcExclusive, cancellationToken);
        var recentNeedsReview = await active
            .Where(document => document.Status == DocumentStatus.NeedsReview)
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenByDescending(document => document.Id)
            .Take(5)
            .Select(document => new RecentNeedsReviewData(
                document.Id, document.OriginalFileName, document.Type, document.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        var recentFailed = await active
            .Where(document => document.Status == DocumentStatus.Failed)
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenByDescending(document => document.Id)
            .Take(5)
            .Select(document => new RecentFailedData(
                document.Id, document.OriginalFileName, document.CreatedAtUtc, document.ProcessingError))
            .ToListAsync(cancellationToken);

        return new OperationsDashboardData(
            statuses.GetValueOrDefault(DocumentStatus.PendingProcessing),
            statuses.GetValueOrDefault(DocumentStatus.Processing),
            statuses.GetValueOrDefault(DocumentStatus.NeedsReview),
            statuses.GetValueOrDefault(DocumentStatus.Failed),
            completedToday,
            statuses.Values.Sum(),
            archivedTotal,
            recentNeedsReview,
            recentFailed);
    }

    public async Task<DashboardAggregateData> GetAggregateDataAsync(
        DateTime recentFromUtcInclusive,
        DateTime recentToUtcExclusive,
        CancellationToken cancellationToken)
    {
        var allDocuments = dbContext.Documents.AsNoTracking();
        var archivedDocuments = await allDocuments.CountAsync(document => document.IsArchived, cancellationToken);
        var documents = allDocuments.Where(document => !document.IsArchived);

        var statuses = await documents
            .GroupBy(document => document.Status)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var types = await documents
            .GroupBy(document => document.Type)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var departments = await documents
            .GroupBy(document => document.TargetDepartment)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var completionCounts = await documents
            .Where(document => document.Status == DocumentStatus.Completed)
            .Select(document => new
            {
                IsManual = document.Events.Any(documentEvent =>
                    documentEvent.Type == DocumentEventType.ManuallyReviewed)
            })
            .GroupBy(item => item.IsManual)
            .Select(group => new { IsManual = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.IsManual, item => item.Count, cancellationToken);

        var dailyDocuments = await documents
            .Where(document =>
                document.CreatedAtUtc >= recentFromUtcInclusive &&
                document.CreatedAtUtc < recentToUtcExclusive)
            .GroupBy(document => document.CreatedAtUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToDictionaryAsync(
                item => DateOnly.FromDateTime(item.Date),
                item => item.Count,
                cancellationToken);

        return new DashboardAggregateData(
            statuses,
            types,
            departments,
            completionCounts.GetValueOrDefault(false),
            completionCounts.GetValueOrDefault(true),
            dailyDocuments,
            archivedDocuments);
    }
}
