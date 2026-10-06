using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Core.Abstractions;

public interface IJobRepository
{
    /// <summary>True if the same external id was already seen, or an equivalent item (same content hash,
    /// or same company+normalized title) was seen within the configured dedup window.</summary>
    Task<bool> IsDuplicateAsync(ICandidateItem item, string contentHash, CancellationToken cancellationToken);

    /// <summary>Persists an approved item as "seen" (not yet notified) and returns its row id.</summary>
    Task<Guid> SaveSeenAsync(ScoredItem scored, string contentHash, CancellationToken cancellationToken);

    Task SaveDiscardedAsync(ICandidateItem item, string reason, CancellationToken cancellationToken);

    Task<bool> WasDiscardedAsync(SourceType source, string externalId, CancellationToken cancellationToken);

    /// <summary>Items saved as "seen" but not yet notified (e.g. the last send attempt failed) - retried on the next cycle.</summary>
    Task<IReadOnlyList<UnnotifiedEntry>> GetUnnotifiedAsync(CancellationToken cancellationToken);

    /// <summary>Marks rows as notified. Call only after the notification e-mail was sent successfully.</summary>
    Task MarkNotifiedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<int> PurgeOlderThanAsync(TimeSpan retention, CancellationToken cancellationToken);

    Task<RuntimeState> GetRuntimeStateAsync(CancellationToken cancellationToken);

    Task SaveRuntimeStateAsync(RuntimeState state, CancellationToken cancellationToken);
}
