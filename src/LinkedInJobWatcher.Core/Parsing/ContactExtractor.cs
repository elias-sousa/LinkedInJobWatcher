using System.Text.RegularExpressions;
using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Core.Text;

namespace LinkedInJobWatcher.Core.Parsing;

public sealed partial class ContactExtractor : IContactExtractor
{
    private static readonly string[] ExampleEmailDomains =
    [
        "example.com", "exemplo.com", "empresa.com", "dominio.com", "seudominio.com", "email.com"
    ];

    private static readonly string[] RecruiterHeadlineKeywords =
    [
        "recrutador", "recruiter", "talent acquisition", "rh ", "people", "headhunter", "recursos humanos"
    ];

    private static readonly (string Platform, string Domain)[] KnownPlatforms =
    [
        ("Gupy", "gupy.io"),
        ("Kenoby", "kenoby.com"),
        ("Solides", "solides.com.br"),
        ("Greenhouse", "greenhouse.io"),
        ("Greenhouse", "grnh.se"),
        ("Lever", "lever.co"),
        ("Workable", "workable.com"),
        ("Pandapé", "pandape.com.br"),
        ("InHire", "inhire.com"),
        ("Google Forms", "forms.gle"),
        ("Google Forms", "docs.google.com/forms"),
        ("Typeform", "typeform.com")
    ];

    private static readonly string[] ShortenerDomains = ["lnkd.in", "bit.ly"];

    [GeneratedRegex(@"[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"https?://[^\s,;]+|www\.[^\s,;]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    // Note: the prefix alternatives use an inline (?i:...) scope instead of RegexOptions.IgnoreCase on the
    // whole pattern, because the captured name must stay case-SENSITIVE (each word has to start uppercase)
    // or a trailing lowercase word (e.g. "... Fulana até sexta") would get swallowed into the match.
    [GeneratedRegex(@"\b(?i:envi(?:ar|e)(?:\s+(?:o|seu))?(?:\s+(?:curr[íi]culo|cv))?\s+para|falar\s+com|encaminhar\s+para|contato\s*[:])\s+([A-ZÀ-Ü][\wÀ-ÿ'’\.]+(?:\s+[A-ZÀ-Ü][\wÀ-ÿ'’\.]+){0,2})")]
    private static partial Regex ContactNameRegex();

    [GeneratedRegex(@"assunto\s*[:\-]\s*(.+?)(?:[\.\n]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex SubjectHintRegex();

    [GeneratedRegex(@"c[óo]digo\s*(?:da\s*vaga)?\s*[:\-]\s*([A-Za-z0-9\-/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex JobCodeRegex();

    [GeneratedRegex(@"r\$\s?[\d.,]+(?:\s*(?:a|at[ée]|-)\s*r?\$?\s?[\d.,]+)?", RegexOptions.IgnoreCase)]
    private static partial Regex SalaryRegex();

    public ContactInfo Extract(string fullText, string? authorName, string? authorHeadline)
    {
        var text = fullText ?? string.Empty;

        var email = ExtractEmail(text);
        var (applicationUrl, platform) = ExtractApplicationLink(text);
        var contactName = ExtractContactName(text, authorName, authorHeadline);
        var subjectHint = ExtractSubjectHint(text);
        var employmentType = ExtractEmploymentType(text);
        var salaryRange = ExtractSalaryRange(text);

        return new ContactInfo
        {
            Email = email,
            ApplicationUrl = applicationUrl,
            ApplicationPlatform = platform,
            ContactName = contactName,
            SubjectHint = subjectHint,
            EmploymentType = employmentType,
            SalaryRange = salaryRange
        };
    }

    private static string? ExtractEmail(string text)
    {
        var deobfuscated = Deobfuscate(text);
        foreach (Match match in EmailRegex().Matches(deobfuscated))
        {
            var candidate = match.Value.ToLowerInvariant();
            if (!ExampleEmailDomains.Any(domain => candidate.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string Deobfuscate(string text)
    {
        var result = text;
        result = Regex.Replace(result, @"\s*\[\s*at\s*\]\s*|\s*\(\s*at\s*\)\s*", "@", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*\(\s*arroba\s*\)\s*|\s*\[\s*arroba\s*\]\s*", "@", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s+arroba\s+", "@", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*\(\s*dot\s*\)\s*|\s*\[\s*dot\s*\]\s*", ".", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*\(\s*ponto\s*\)\s*|\s*\[\s*ponto\s*\]\s*", ".", RegexOptions.IgnoreCase);
        return result;
    }

    private static (string? Url, string? Platform) ExtractApplicationLink(string text)
    {
        foreach (Match match in UrlRegex().Matches(text))
        {
            var url = match.Value.TrimEnd('.', ',', ')', ';');

            foreach (var (platform, domain) in KnownPlatforms)
            {
                if (url.Contains(domain, StringComparison.OrdinalIgnoreCase))
                {
                    return (url, platform);
                }
            }
        }

        // No known ATS link found: fall back to a shortener if present (caller may resolve it later),
        // or to the first generic URL as the company/application link.
        foreach (Match match in UrlRegex().Matches(text))
        {
            var url = match.Value.TrimEnd('.', ',', ')', ';');
            if (ShortenerDomains.Any(domain => url.Contains(domain, StringComparison.OrdinalIgnoreCase)))
            {
                return (url, "Link encurtado");
            }
        }

        var first = UrlRegex().Match(text);
        return first.Success ? (first.Value.TrimEnd('.', ',', ')', ';'), "Site da empresa") : (null, null);
    }

    private static string? ExtractContactName(string text, string? authorName, string? authorHeadline)
    {
        var match = ContactNameRegex().Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        if (!string.IsNullOrWhiteSpace(authorHeadline) && !string.IsNullOrWhiteSpace(authorName))
        {
            var normalizedHeadline = TextNormalizer.Normalize(authorHeadline);
            if (RecruiterHeadlineKeywords.Any(keyword => normalizedHeadline.Contains(TextNormalizer.Normalize(keyword))))
            {
                return authorName;
            }
        }

        return null;
    }

    private static string? ExtractSubjectHint(string text)
    {
        var match = SubjectHintRegex().Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        var code = JobCodeRegex().Match(text);
        return code.Success ? code.Groups[1].Value.Trim() : null;
    }

    private static EmploymentType ExtractEmploymentType(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        var hasClt = Regex.IsMatch(normalized, @"\bclt\b");
        var hasPj = Regex.IsMatch(normalized, @"\bpj\b");
        var hasCooperado = normalized.Contains("cooperado");

        if (hasClt && hasPj)
        {
            return EmploymentType.Clt; // listed as neutral, informational only; CLT takes precedence when both are mentioned
        }

        if (hasClt)
        {
            return EmploymentType.Clt;
        }

        if (hasPj)
        {
            return EmploymentType.Pj;
        }

        return hasCooperado ? EmploymentType.Cooperado : EmploymentType.Unknown;
    }

    private static string? ExtractSalaryRange(string text)
    {
        var match = SalaryRegex().Match(text);
        return match.Success ? match.Value.Trim() : null;
    }
}
