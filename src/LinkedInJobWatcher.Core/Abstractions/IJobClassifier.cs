using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Core.Abstractions;

/// <summary>
/// Scores (or discards) a candidate item against the profile in <c>Filters</c> configuration.
/// The default implementation is deterministic (regex + normalization). An LLM-backed
/// implementation can be plugged in later behind this same interface (off by default).
/// </summary>
public interface IJobClassifier
{
    ScoredItem Classify(ICandidateItem item);
}
