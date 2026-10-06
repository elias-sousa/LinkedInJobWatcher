namespace LinkedInJobWatcher.Core.Abstractions;

/// <summary>Converts a LinkedIn relative date string ("3h", "há 2 dias", "Reposted 1d", ...) to an absolute instant.</summary>
public interface IRelativeDateParser
{
    bool TryParse(string relativeText, DateTimeOffset referenceNow, out DateTimeOffset publishedAt);
}
