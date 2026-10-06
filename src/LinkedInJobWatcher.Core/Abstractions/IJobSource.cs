using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Core.Abstractions;

/// <summary>A read-only source of candidate items (LinkedIn posts or LinkedIn jobs) for one search term.</summary>
public interface IJobSource
{
    SourceType Source { get; }

    Task<IReadOnlyList<ICandidateItem>> SearchAsync(string term, CancellationToken cancellationToken);
}
