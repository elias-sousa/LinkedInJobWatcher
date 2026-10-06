namespace LinkedInJobWatcher.Core.Models;

public sealed class ScoredItem
{
    public required ICandidateItem Item { get; init; }

    public int Score { get; set; }

    public List<string> Reasons { get; } = [];

    public ContactInfo? Contact { get; set; }

    public string? DiscardReason { get; set; }

    public bool IsDiscarded => DiscardReason is not null;

    public string ContentHash { get; set; } = string.Empty;
}
