namespace LinkedInJobWatcher.Infrastructure.Persistence;

/// <summary>Single-row table (Id is always 1) holding the state that must survive a restart.</summary>
public sealed class RuntimeStateEntity
{
    public int Id { get; set; } = 1;

    public int PostTermIndex { get; set; }

    public int JobTermIndex { get; set; }

    public int ConsecutiveFailures { get; set; }

    public DateTimeOffset? BackoffUntil { get; set; }

    public DateTimeOffset? LastSessionExpiredAlertAt { get; set; }

    public DateTimeOffset? LastBlockedAlertAt { get; set; }
}
