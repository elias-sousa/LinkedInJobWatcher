using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

public sealed class LinkedInJobSource(
    LinkedInSessionManager sessionManager,
    HumanBehavior humanBehavior,
    JobHtmlParser parser,
    IOptions<LinkedInOptions> options,
    ILogger<LinkedInJobSource> logger) : IJobSource
{
    private readonly LinkedInOptions _options = options.Value;

    public SourceType Source => SourceType.Job;

    public async Task<IReadOnlyList<ICandidateItem>> SearchAsync(string term, CancellationToken cancellationToken)
    {
        var context = await sessionManager.GetOrCreateContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            var url = BuildSearchUrl(term);
            logger.LogInformation("Buscando vagas para o termo '{Term}'", term);
            await sessionManager.GoToAsync(page, url);
            await humanBehavior.RandomDelayAsync(cancellationToken);

            await humanBehavior.ScrollGraduallyAsync(
                page,
                () => page.Locator(LinkedInSelectors.Jobs.ResultCard).CountAsync(),
                _options.MaxItemsPerSearch,
                cancellationToken: cancellationToken);

            var cards = page.Locator(LinkedInSelectors.Jobs.ResultCard);
            var count = Math.Min(await cards.CountAsync(), _options.MaxItemsPerSearch);

            var results = new List<ICandidateItem>(count);

            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var card = cards.Nth(i);
                var cardHtml = await card.EvaluateAsync<string>("el => el.outerHTML");
                var job = await parser.ParseCardAsync(cardHtml, term);
                if (job is null)
                {
                    continue;
                }

                try
                {
                    await card.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                    await humanBehavior.RandomDelayAsync(cancellationToken);
                    await page.Locator(LinkedInSelectors.Jobs.DetailsPane).First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

                    var detailsHtml = await page.Locator(LinkedInSelectors.Jobs.DetailsPane).First.EvaluateAsync<string>("el => el.outerHTML");
                    await parser.EnrichWithDetailsAsync(job, detailsHtml);
                }
                catch (TimeoutException)
                {
                    logger.LogDebug("Não foi possível abrir o painel de detalhes da vaga {JobId}; usando apenas os dados do card.", job.ExternalId);
                }

                results.Add(job);
            }

            logger.LogInformation("{Count} vagas capturadas para o termo '{Term}'", results.Count, term);
            return results;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private string BuildSearchUrl(string term)
    {
        var encodedTerm = Uri.EscapeDataString(term);
        return string.Format(
            LinkedInSelectors.Jobs.SearchUrlTemplate,
            encodedTerm,
            _options.JobTimePostedFilter,
            _options.JobWorkplaceTypeFilter,
            _options.JobExperienceLevelFilter);
    }
}
