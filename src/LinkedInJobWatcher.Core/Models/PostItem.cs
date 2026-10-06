namespace LinkedInJobWatcher.Core.Models;

public sealed class PostItem : ICandidateItem
{
    public SourceType Source => SourceType.Post;

    public required string ExternalId { get; set; }

    public required string Url { get; set; }

    public string? Title => null;

    public string? Company => null;

    public required string FullText { get; set; }

    public required string RelativeDateText { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? AuthorName { get; set; }

    public string? AuthorHeadline { get; set; }

    public string? AuthorProfileUrl { get; set; }

    public List<string> LinksInText { get; set; } = [];

    public string SearchTerm { get; set; } = string.Empty;
}
