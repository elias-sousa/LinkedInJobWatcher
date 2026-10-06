using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

public sealed class LinkedInPostSource(
    LinkedInSessionManager sessionManager,
    HumanBehavior humanBehavior,
    PostHtmlParser parser,
    IOptions<LinkedInOptions> options,
    ILogger<LinkedInPostSource> logger) : IJobSource
{
    private readonly LinkedInOptions _options = options.Value;

    public SourceType Source => SourceType.Post;

    public async Task<IReadOnlyList<ICandidateItem>> SearchAsync(string term, CancellationToken cancellationToken)
    {
        var context = await sessionManager.GetOrCreateContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            var url = BuildSearchUrl(term, _options.DatePosted);
            logger.LogInformation("Buscando posts para o termo '{Term}'", term);
            await sessionManager.GoToAsync(page, url);
            await humanBehavior.RandomDelayAsync(cancellationToken);

            await humanBehavior.ScrollGraduallyAsync(
                page,
                () => page.Locator(LinkedInSelectors.Posts.ResultCard).CountAsync(),
                _options.MaxItemsPerSearch,
                cancellationToken: cancellationToken);

            var cards = page.Locator(LinkedInSelectors.Posts.ResultCard);
            var count = Math.Min(await cards.CountAsync(), _options.MaxItemsPerSearch);

            var results = new List<ICandidateItem>(count);

            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var card = cards.Nth(i);

                var seeMore = card.Locator(LinkedInSelectors.Posts.SeeMoreButton);
                if (await seeMore.CountAsync() > 0)
                {
                    try
                    {
                        await seeMore.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                        await humanBehavior.RandomDelayAsync(cancellationToken);
                    }
                    catch (TimeoutException)
                    {
                        // "ver mais" not clickable for this card; the text we already have is good enough.
                    }
                }

                var html = await card.EvaluateAsync<string>("el => el.outerHTML");
                var item = await parser.ParseAsync(html, term);
                if (item is not null)
                {
                    results.Add(item);
                }
            }

            logger.LogInformation("{Count} posts capturados para o termo '{Term}'", results.Count, term);
            return results;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static string BuildSearchUrl(string term, string datePosted)
    {
        var encodedTerm = Uri.EscapeDataString(term);
        return string.Format(LinkedInSelectors.Posts.SearchUrlTemplate, encodedTerm, datePosted);
    }
}
