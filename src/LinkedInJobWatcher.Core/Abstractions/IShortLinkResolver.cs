namespace LinkedInJobWatcher.Core.Abstractions;

/// <summary>Best-effort resolution of shortened links (lnkd.in, bit.ly, ...) to their final destination.</summary>
public interface IShortLinkResolver
{
    Task<string> ResolveAsync(string url, CancellationToken cancellationToken);
}
