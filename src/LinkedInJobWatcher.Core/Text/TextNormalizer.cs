using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LinkedInJobWatcher.Core.Text;

public static partial class TextNormalizer
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"https?://\S+|www\.\S+")]
    private static partial Regex UrlRegex();

    public static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Lowercase, accent-free, with whitespace collapsed. Used throughout matching/scoring.</summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var lower = RemoveDiacritics(input).ToLowerInvariant();
        return WhitespaceRegex().Replace(lower, " ").Trim();
    }

    /// <summary>Normalized text with URLs stripped, used as input for the dedup content hash.</summary>
    public static string NormalizeForHashing(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var withoutUrls = UrlRegex().Replace(input, " ");
        return Normalize(withoutUrls);
    }
}
