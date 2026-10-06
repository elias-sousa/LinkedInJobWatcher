using System.Text.Json;
using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Core.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinkedInJobWatcher.Infrastructure.Persistence;

public sealed class SqliteJobRepository(JobWatcherDbContext dbContext, IOptions<StorageOptions> storageOptions, TimeProvider timeProvider) : IJobRepository
{
    private readonly StorageOptions _storageOptions = storageOptions.Value;

    public async Task<bool> IsDuplicateAsync(ICandidateItem item, string contentHash, CancellationToken cancellationToken)
    {
        var sourceKey = item.Source.ToString();

        var byExternalId = await dbContext.SeenItems
            .AnyAsync(e => e.Source == sourceKey && e.ExternalId == item.ExternalId, cancellationToken);
        if (byExternalId)
        {
            return true;
        }

        var byContentHash = await dbContext.SeenItems
            .AnyAsync(e => e.ContentHash == contentHash, cancellationToken);
        if (byContentHash)
        {
            return true;
        }

        var companyTitleKey = BuildCompanyTitleKey(item);
        if (!string.IsNullOrEmpty(companyTitleKey))
        {
            var windowStart = (timeProvider.GetUtcNow() - TimeSpan.FromDays(_storageOptions.DuplicateCompanyTitleWindowDays)).UtcDateTime;
            var byCompanyTitle = await dbContext.SeenItems
                .AnyAsync(e => e.CompanyTitleKey == companyTitleKey && e.FirstSeenAtUtc >= windowStart, cancellationToken);
            if (byCompanyTitle)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<Guid> SaveSeenAsync(ScoredItem scored, string contentHash, CancellationToken cancellationToken)
    {
        var entity = new SeenItemEntity
        {
            Id = Guid.NewGuid(),
            Source = scored.Item.Source.ToString(),
            ExternalId = scored.Item.ExternalId,
            ContentHash = contentHash,
            CompanyTitleKey = BuildCompanyTitleKey(scored.Item),
            Url = scored.Item.Url,
            Title = scored.Item.Title,
            Company = scored.Item.Company,
            Score = scored.Score,
            PublishedAtUtc = (scored.Item.PublishedAt ?? timeProvider.GetUtcNow()).UtcDateTime,
            FirstSeenAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            PayloadJson = JsonSerializer.Serialize(StoredScoredItem.FromScoredItem(scored))
        };

        dbContext.SeenItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task SaveDiscardedAsync(ICandidateItem item, string reason, CancellationToken cancellationToken)
    {
        dbContext.DiscardedItems.Add(new DiscardedItemEntity
        {
            Id = Guid.NewGuid(),
            Source = item.Source.ToString(),
            ExternalId = item.ExternalId,
            Reason = reason,
            Url = item.Url,
            FirstSeenAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> WasDiscardedAsync(SourceType source, string externalId, CancellationToken cancellationToken)
    {
        var sourceKey = source.ToString();
        return dbContext.DiscardedItems.AnyAsync(e => e.Source == sourceKey && e.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<UnnotifiedEntry>> GetUnnotifiedAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.SeenItems
            .Where(e => e.NotifiedAtUtc == null)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new UnnotifiedEntry(row.Id, JsonSerializer.Deserialize<StoredScoredItem>(row.PayloadJson)!.ToScoredItem()))
            .ToList();
    }

    public async Task MarkNotifiedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SeenItems
            .Where(e => ids.Contains(e.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.NotifiedAtUtc, now), cancellationToken);
    }

    public async Task<int> PurgeOlderThanAsync(TimeSpan retention, CancellationToken cancellationToken)
    {
        var cutoff = (timeProvider.GetUtcNow() - retention).UtcDateTime;

        var seenDeleted = await dbContext.SeenItems
            .Where(e => e.FirstSeenAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var discardedDeleted = await dbContext.DiscardedItems
            .Where(e => e.FirstSeenAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        return seenDeleted + discardedDeleted;
    }

    public async Task<RuntimeState> GetRuntimeStateAsync(CancellationToken cancellationToken)
    {
        var entity = await dbContext.RuntimeStates.FirstOrDefaultAsync(e => e.Id == 1, cancellationToken);
        if (entity is null)
        {
            return new RuntimeState();
        }

        return new RuntimeState
        {
            PostTermIndex = entity.PostTermIndex,
            JobTermIndex = entity.JobTermIndex,
            ConsecutiveFailures = entity.ConsecutiveFailures,
            BackoffUntil = entity.BackoffUntil,
            LastSessionExpiredAlertAt = entity.LastSessionExpiredAlertAt,
            LastBlockedAlertAt = entity.LastBlockedAlertAt
        };
    }

    public async Task SaveRuntimeStateAsync(RuntimeState state, CancellationToken cancellationToken)
    {
        var entity = await dbContext.RuntimeStates.FirstOrDefaultAsync(e => e.Id == 1, cancellationToken);
        if (entity is null)
        {
            entity = new RuntimeStateEntity { Id = 1 };
            dbContext.RuntimeStates.Add(entity);
        }

        entity.PostTermIndex = state.PostTermIndex;
        entity.JobTermIndex = state.JobTermIndex;
        entity.ConsecutiveFailures = state.ConsecutiveFailures;
        entity.BackoffUntil = state.BackoffUntil;
        entity.LastSessionExpiredAlertAt = state.LastSessionExpiredAlertAt;
        entity.LastBlockedAlertAt = state.LastBlockedAlertAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildCompanyTitleKey(ICandidateItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Company) || string.IsNullOrWhiteSpace(item.Title))
        {
            return string.Empty;
        }

        return $"{TextNormalizer.Normalize(item.Company)}|{TextNormalizer.Normalize(item.Title)}";
    }
}
