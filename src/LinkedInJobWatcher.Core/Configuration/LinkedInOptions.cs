namespace LinkedInJobWatcher.Core.Configuration;

public sealed class LinkedInOptions
{
    public const string SectionName = "LinkedIn";

    /// <summary>Folder for the dedicated Firefox profile used by Playwright (never the user's real Firefox profile).</summary>
    public string BrowserProfileDir { get; set; } = "./browser-profile";

    public bool Headless { get; set; } = true;

    public int MaxItemsPerSearch { get; set; } = 30;

    /// <summary>"past-24h" or "past-week". Used as a fallback when a search yields too few posts.</summary>
    public string DatePosted { get; set; } = "past-24h";

    public string DatePostedFallback { get; set; } = "past-week";

    public List<string> PostSearchTerms { get; set; } =
    [
        "vaga desenvolvedor .net senior remoto",
        "vaga c# senior home office",
        "hiring senior .net developer remote",
        "oportunidade backend .net senior"
    ];

    public List<string> JobSearchTerms { get; set; } =
    [
        ".NET Senior",
        "C# Senior",
        "Desenvolvedor Backend .NET"
    ];

    /// <summary>f_TPR value for the jobs search (r172800 = last 48h).</summary>
    public string JobTimePostedFilter { get; set; } = "r172800";

    /// <summary>f_WT value for the jobs search (2 = remote).</summary>
    public string JobWorkplaceTypeFilter { get; set; } = "2";

    /// <summary>f_E value for the jobs search (4 = mid-senior level).</summary>
    public string JobExperienceLevelFilter { get; set; } = "4";

    public int MinDelayMillisecondsBetweenActions { get; set; } = 1000;

    public int MaxDelayMillisecondsBetweenActions { get; set; } = 4000;
}
