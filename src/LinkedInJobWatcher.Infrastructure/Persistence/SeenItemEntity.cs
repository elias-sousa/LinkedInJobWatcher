namespace LinkedInJobWatcher.Infrastructure.Persistence;

public sealed class SeenItemEntity
{
    public Guid Id { get; set; }

    public required string Source { get; set; }

    public required string ExternalId { get; set; }

    public required string ContentHash { get; set; }

    /// <summary>Normalized "company|title" used for the 7-day same-job-reposted dedup check (jobs only; empty for posts).</summary>
    public string CompanyTitleKey { get; set; } = string.Empty;

    public required string Url { get; set; }

    public string? Title { get; set; }

    public string? Company { get; set; }

    public int Score { get; set; }

    // Stored as DateTime (UTC), not DateTimeOffset: the EF Core Sqlite provider cannot translate
    // relational comparisons (>=, <) on DateTimeOffset columns, which IsDuplicateAsync/PurgeOlderThanAsync need.
    public DateTime PublishedAtUtc { get; set; }

    public DateTime FirstSeenAtUtc { get; set; }

    public DateTime? NotifiedAtUtc { get; set; }

    /// <summary>JSON snapshot (see <see cref="StoredScoredItem"/>) used to rebuild the e-mail card if the first send attempt fails and must be retried.</summary>
    public string PayloadJson { get; set; } = string.Empty;
}
