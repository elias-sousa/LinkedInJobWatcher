using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Core.Abstractions;

public interface IContactExtractor
{
    ContactInfo Extract(string fullText, string? authorName, string? authorHeadline);
}
