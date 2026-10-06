namespace LinkedInJobWatcher.Core.Models;

public sealed class JobItem : ICandidateItem
{
    public SourceType Source => SourceType.Job;

    public required string ExternalId { get; set; }

    public required string Url { get; set; }

    public string? Title { get; set; }

    public string? Company { get; set; }

    public string? Location { get; set; }

    public WorkMode WorkMode { get; set; } = WorkMode.Unknown;

    public bool SimplifiedApplication { get; set; }

    public required string FullText { get; set; }

    public required string RelativeDateText { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? AuthorName => RecruiterName;

    public string? AuthorHeadline => null;

    public string? AuthorProfileUrl => null;

    public string? RecruiterName { get; set; }

    public string SearchTerm { get; set; } = string.Empty;
}
