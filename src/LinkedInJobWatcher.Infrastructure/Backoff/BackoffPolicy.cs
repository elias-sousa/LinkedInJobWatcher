namespace LinkedInJobWatcher.Infrastructure.Backoff;

/// <summary>Exponential backoff after a block/CAPTCHA/429: 30min, 1h, 2h, 4h, capped at 6h.</summary>
public static class BackoffPolicy
{
    private static readonly TimeSpan[] Steps =
    [
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(4),
        TimeSpan.FromHours(6)
    ];

    public static TimeSpan GetDelay(int consecutiveFailures)
    {
        if (consecutiveFailures <= 0)
        {
            return TimeSpan.Zero;
        }

        var index = Math.Min(consecutiveFailures - 1, Steps.Length - 1);
        return Steps[index];
    }
}
