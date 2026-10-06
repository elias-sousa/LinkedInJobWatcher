namespace LinkedInJobWatcher.Core.Models;

/// <summary>
/// Small piece of state that must survive a restart of the worker: which search term
/// comes next in the rotation, and whether we are currently backing off after a block/captcha.
/// </summary>
public sealed class RuntimeState
{
    public int PostTermIndex { get; set; }

    public int JobTermIndex { get; set; }

    public int ConsecutiveFailures { get; set; }

    public DateTimeOffset? BackoffUntil { get; set; }

    public DateTimeOffset? LastSessionExpiredAlertAt { get; set; }

    public DateTimeOffset? LastBlockedAlertAt { get; set; }
}
