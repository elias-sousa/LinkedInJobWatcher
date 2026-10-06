using System.Security.Cryptography;
using System.Text;

namespace LinkedInJobWatcher.Core.Text;

public static class ContentHasher
{
    /// <summary>SHA-256 of the normalized (lowercase, accent-free, URL-free) text, as a lowercase hex string.</summary>
    public static string ComputeHash(string rawText)
    {
        var normalized = TextNormalizer.NormalizeForHashing(rawText);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
