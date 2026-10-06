using System.Text.RegularExpressions;
using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Text;

namespace LinkedInJobWatcher.Core.Parsing;

/// <summary>
/// Parses LinkedIn's relative date strings in Portuguese and English, e.g.:
/// "5 min", "3h", "1d", "2 dias", "1 sem", "5m", "2d", "1w", "há 3 horas", "Reposted 1d", "Edited • 2h".
/// </summary>
public sealed partial class RelativeDateParser : IRelativeDateParser
{
    [GeneratedRegex(@"(?<num>\d+)\s*(?<unit>mo|meses|mes|min|minutos|minuto|m|horas|hora|h|semanas|semana|sem|w|dias|dia|d|y|anos|ano)\b")]
    private static partial Regex RelativeRegex();

    public bool TryParse(string relativeText, DateTimeOffset referenceNow, out DateTimeOffset publishedAt)
    {
        publishedAt = default;

        if (string.IsNullOrWhiteSpace(relativeText))
        {
            return false;
        }

        var normalized = TextNormalizer.Normalize(relativeText);

        // LinkedIn often prefixes with "agora" / "just now" / "now" for very recent items.
        if (normalized.Contains("agora") || normalized is "now" or "just now")
        {
            publishedAt = referenceNow;
            return true;
        }

        var match = RelativeRegex().Match(normalized);
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["num"].Value, out var amount))
        {
            return false;
        }

        var unit = match.Groups["unit"].Value;

        TimeSpan delta = unit switch
        {
            "min" or "minutos" or "minuto" or "m" => TimeSpan.FromMinutes(amount),
            "h" or "hora" or "horas" => TimeSpan.FromHours(amount),
            "d" or "dia" or "dias" => TimeSpan.FromDays(amount),
            "w" or "sem" or "semana" or "semanas" => TimeSpan.FromDays(amount * 7),
            "mo" or "mes" or "meses" => TimeSpan.FromDays(amount * 30),
            "y" or "ano" or "anos" => TimeSpan.FromDays(amount * 365),
            _ => TimeSpan.Zero
        };

        if (delta == TimeSpan.Zero && unit != "min" && amount != 0)
        {
            return false;
        }

        publishedAt = referenceNow - delta;
        return true;
    }
}
