using LinkedInJobWatcher.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

/// <summary>Small helpers to keep the browsing pattern closer to a human's: random pauses and gradual scrolling.</summary>
public sealed class HumanBehavior(IOptions<LinkedInOptions> options)
{
    private readonly Random _random = new();
    private readonly LinkedInOptions _options = options.Value;

    public async Task RandomDelayAsync(CancellationToken cancellationToken = default)
    {
        var delayMs = _random.Next(_options.MinDelayMillisecondsBetweenActions, _options.MaxDelayMillisecondsBetweenActions + 1);
        await Task.Delay(delayMs, cancellationToken);
    }

    /// <summary>Scrolls the page down in small steps, pausing between each, until no new content loads or the item cap is reached.</summary>
    public async Task ScrollGraduallyAsync(IPage page, Func<Task<int>> countLoadedItems, int maxItems, int maxSteps = 15, CancellationToken cancellationToken = default)
    {
        var previousCount = -1;

        for (var step = 0; step < maxSteps; step++)
        {
            var currentCount = await countLoadedItems();
            if (currentCount >= maxItems || (currentCount == previousCount && step > 2))
            {
                break;
            }

            previousCount = currentCount;

            await page.Mouse.WheelAsync(0, _random.Next(400, 900));
            await RandomDelayAsync(cancellationToken);
        }
    }
}
