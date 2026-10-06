namespace LinkedInJobWatcher.Infrastructure.Persistence;

public sealed class DiscardedItemEntity
{
    public Guid Id { get; set; }

    public required string Source { get; set; }

    public required string ExternalId { get; set; }

    public required string Reason { get; set; }

    public required string Url { get; set; }

    public DateTime FirstSeenAtUtc { get; set; }
}
