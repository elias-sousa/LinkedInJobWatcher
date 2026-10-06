using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Infrastructure.Persistence;

/// <summary>Plain, JSON-serializable snapshot of a <see cref="ScoredItem"/>, stored so a failed e-mail send can be retried later without re-scraping or re-scoring.</summary>
public sealed class StoredScoredItem
{
    public SourceType Source { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public bool SimplifiedApplication { get; set; }
    public string FullText { get; set; } = string.Empty;
    public string RelativeDateText { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorHeadline { get; set; }
    public int Score { get; set; }
    public List<string> Reasons { get; set; } = [];
    public ContactInfo? Contact { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static StoredScoredItem FromScoredItem(ScoredItem scored) => new()
    {
        Source = scored.Item.Source,
        ExternalId = scored.Item.ExternalId,
        Url = scored.Item.Url,
        Title = scored.Item.Title,
        Company = scored.Item.Company,
        Location = (scored.Item as JobItem)?.Location,
        SimplifiedApplication = (scored.Item as JobItem)?.SimplifiedApplication ?? false,
        FullText = scored.Item.FullText,
        RelativeDateText = scored.Item.RelativeDateText,
        PublishedAt = scored.Item.PublishedAt,
        AuthorName = scored.Item.AuthorName,
        AuthorHeadline = scored.Item.AuthorHeadline,
        Score = scored.Score,
        Reasons = [..scored.Reasons],
        Contact = scored.Contact,
        ContentHash = scored.ContentHash
    };

    public ScoredItem ToScoredItem()
    {
        ICandidateItem item = Source == SourceType.Job
            ? new JobItem
            {
                ExternalId = ExternalId,
                Url = Url,
                Title = Title,
                Company = Company,
                Location = Location,
                SimplifiedApplication = SimplifiedApplication,
                FullText = FullText,
                RelativeDateText = RelativeDateText,
                PublishedAt = PublishedAt
            }
            : new PostItem
            {
                ExternalId = ExternalId,
                Url = Url,
                FullText = FullText,
                RelativeDateText = RelativeDateText,
                PublishedAt = PublishedAt,
                AuthorName = AuthorName,
                AuthorHeadline = AuthorHeadline
            };

        var scored = new ScoredItem { Item = item, Score = Score, Contact = Contact, ContentHash = ContentHash };
        scored.Reasons.AddRange(Reasons);
        return scored;
    }
}
