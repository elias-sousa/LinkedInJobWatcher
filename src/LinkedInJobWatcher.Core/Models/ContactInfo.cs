namespace LinkedInJobWatcher.Core.Models;

public sealed record ContactInfo
{
    public string? Email { get; init; }
    public string? ApplicationUrl { get; init; }
    public string? ApplicationPlatform { get; init; }
    public string? ContactName { get; init; }
    public string? SubjectHint { get; init; }
    public EmploymentType EmploymentType { get; init; } = EmploymentType.Unknown;
    public string? SalaryRange { get; init; }

    public bool HasDirectContact => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(ContactName);
}
