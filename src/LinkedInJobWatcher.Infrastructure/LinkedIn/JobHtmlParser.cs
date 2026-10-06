using AngleSharp;
using AngleSharp.Dom;
using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

/// <summary>
/// Turns the captured outer HTML of a job search result card, plus the details pane HTML
/// loaded after clicking it, into a <see cref="JobItem"/>. Kept independent from Playwright
/// so it can be unit tested against static HTML fixtures.
/// </summary>
public sealed class JobHtmlParser
{
    private static readonly IBrowsingContext Context = AngleSharp.BrowsingContext.New(Configuration.Default);

    public async Task<JobItem?> ParseCardAsync(string cardOuterHtml, string searchTerm)
    {
        var document = await Context.OpenAsync(req => req.Content(cardOuterHtml));
        var root = document.Body?.FirstElementChild ?? document.Body;
        if (root is null)
        {
            return null;
        }

        var jobId = root.Closest($"[{LinkedInSelectors.Jobs.ResultCardIdAttribute}]")?.GetAttribute(LinkedInSelectors.Jobs.ResultCardIdAttribute)
                    ?? root.GetAttribute(LinkedInSelectors.Jobs.ResultCardIdAttribute);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return null;
        }

        var titleAnchor = root.QuerySelector(LinkedInSelectors.Jobs.CardTitle);
        var title = CleanText(titleAnchor?.TextContent ?? titleAnchor?.GetAttribute("aria-label"));
        var company = CleanText(root.QuerySelector(LinkedInSelectors.Jobs.CardCompany)?.TextContent);
        var location = CleanText(root.QuerySelectorAll(LinkedInSelectors.Jobs.CardLocation).FirstOrDefault()?.TextContent);
        var simplified = root.QuerySelector(LinkedInSelectors.Jobs.CardSimplifiedApplyBadge) is not null;
        var relativeDate = CleanText(root.QuerySelector(LinkedInSelectors.Jobs.CardPostedDate)?.TextContent) ?? string.Empty;
        var href = titleAnchor?.GetAttribute("href");

        return new JobItem
        {
            ExternalId = jobId,
            Url = NormalizeUrl(href) ?? $"https://www.linkedin.com/jobs/view/{jobId}/",
            Title = title,
            Company = company,
            Location = location,
            SimplifiedApplication = simplified,
            RelativeDateText = relativeDate,
            FullText = string.Join(" | ", new[] { title, company, location }.Where(s => !string.IsNullOrWhiteSpace(s))),
            SearchTerm = searchTerm
        };
    }

    /// <summary>Enriches a card-level job with the full description loaded in the details pane.</summary>
    public async Task EnrichWithDetailsAsync(JobItem job, string detailsOuterHtml)
    {
        var document = await Context.OpenAsync(req => req.Content(detailsOuterHtml));
        var root = document.Body;
        if (root is null)
        {
            return;
        }

        var description = CleanText(root.QuerySelector(LinkedInSelectors.Jobs.DetailsFullText)?.TextContent);
        var recruiter = CleanText(root.QuerySelector(LinkedInSelectors.Jobs.DetailsRecruiterCard)?.TextContent);

        if (!string.IsNullOrWhiteSpace(description))
        {
            job.FullText = description;
        }

        if (!string.IsNullOrWhiteSpace(recruiter))
        {
            job.RecruiterName = recruiter;
        }
    }

    private static string? CleanText(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : string.Join(' ', text.Split(default(char[]?), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var clean = url.Split('?')[0];
        return clean.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? clean : $"https://www.linkedin.com{clean}";
    }
}
