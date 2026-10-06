namespace LinkedInJobWatcher.Core.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string DatabasePath { get; set; } = "./data/linkedin-job-watcher.db";

    public int RetentionDays { get; set; } = 60;

    public int DuplicateCompanyTitleWindowDays { get; set; } = 7;
}
