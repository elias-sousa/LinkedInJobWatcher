namespace LinkedInJobWatcher.Core.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    /// <summary>STARTTLS (587) vs implicit SSL (465). Set accordingly in appsettings.</summary>
    public bool UseSsl { get; set; } = true;

    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>Never store the real value here - set it via User Secrets or the LinkedInJobWatcher_Email__SmtpPassword env var.</summary>
    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "LinkedIn Job Watcher";

    public string ToAddress { get; set; } = string.Empty;

    /// <summary>Minimum time between two consecutive "session expired" alert emails.</summary>
    public int SessionAlertCooldownHours { get; set; } = 12;
}
