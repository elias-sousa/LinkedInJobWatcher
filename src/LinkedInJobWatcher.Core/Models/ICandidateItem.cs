namespace LinkedInJobWatcher.Core.Models;

/// <summary>
/// Common shape for anything the scrapers can produce (a feed post or a job listing)
/// so scoring, deduplication and notification can work against a single abstraction.
/// </summary>
public interface ICandidateItem
{
    SourceType Source { get; }

    /// <summary>LinkedIn URN (post) or job id (job), used as the dedup key together with <see cref="Source"/>.</summary>
    string ExternalId { get; }

    string Url { get; }

    string? Title { get; }

    string? Company { get; }

    string FullText { get; }

    /// <summary>Relative date text exactly as shown by LinkedIn, e.g. "3h", "há 2 dias", "Reposted 1d".</summary>
    string RelativeDateText { get; }

    /// <summary>Resolved by <see cref="Abstractions.IRelativeDateParser"/> before classification. Null means "could not be resolved".</summary>
    DateTimeOffset? PublishedAt { get; set; }

    string? AuthorName { get; }

    string? AuthorHeadline { get; }

    string? AuthorProfileUrl { get; }
}
