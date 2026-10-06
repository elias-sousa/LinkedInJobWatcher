using AngleSharp;
using AngleSharp.Dom;
using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

/// <summary>
/// Turns the captured outer HTML of a single post result card into a <see cref="PostItem"/>.
/// Kept independent from Playwright so it can be unit tested against static HTML fixtures.
/// </summary>
public sealed class PostHtmlParser
{
    private static readonly IBrowsingContext Context = AngleSharp.BrowsingContext.New(Configuration.Default);

    public async Task<PostItem?> ParseAsync(string cardOuterHtml, string searchTerm)
    {
        var document = await Context.OpenAsync(req => req.Content(cardOuterHtml));
        var root = document.Body?.FirstElementChild ?? document.Body;
        if (root is null)
        {
            return null;
        }

        var urn = root.Closest($"[{LinkedInSelectors.Posts.ResultCardUrnAttribute}]")?.GetAttribute(LinkedInSelectors.Posts.ResultCardUrnAttribute)
                  ?? root.GetAttribute(LinkedInSelectors.Posts.ResultCardUrnAttribute);

        if (string.IsNullOrWhiteSpace(urn))
        {
            return null;
        }

        var authorName = CleanText(root.QuerySelector(LinkedInSelectors.Posts.AuthorNameLink)?.TextContent);
        var authorHeadline = CleanText(root.QuerySelector(LinkedInSelectors.Posts.AuthorHeadline)?.TextContent);
        var authorProfileUrl = root.QuerySelector(LinkedInSelectors.Posts.AuthorProfileLink)?.GetAttribute("href");
        var relativeDate = CleanText(root.QuerySelector(LinkedInSelectors.Posts.RelativeDate)?.TextContent) ?? string.Empty;
        var fullText = CleanText(root.QuerySelector(LinkedInSelectors.Posts.TextContainer)?.TextContent) ?? string.Empty;
        var permalink = root.QuerySelector(LinkedInSelectors.Posts.PostPermalinkAnchor)?.GetAttribute("href");

        var links = ExtractLinks(root);

        return new PostItem
        {
            ExternalId = urn,
            Url = NormalizeUrl(permalink) ?? $"https://www.linkedin.com/feed/update/{urn}/",
            FullText = fullText,
            RelativeDateText = relativeDate,
            AuthorName = authorName,
            AuthorHeadline = authorHeadline,
            AuthorProfileUrl = NormalizeUrl(authorProfileUrl),
            LinksInText = links,
            SearchTerm = searchTerm
        };
    }

    private static List<string> ExtractLinks(IElement root)
    {
        var links = new List<string>();
        foreach (var anchor in root.QuerySelectorAll("a"))
        {
            var href = anchor.GetAttribute("href");
            if (!string.IsNullOrWhiteSpace(href) && href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                links.Add(href);
            }
        }

        return links;
    }

    private static string? CleanText(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : string.Join(' ', text.Split(default(char[]?), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : $"https://www.linkedin.com{url}";
    }
}
