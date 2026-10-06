using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Core.Text;
using Microsoft.Extensions.Options;

namespace LinkedInJobWatcher.Core.Scoring;

/// <summary>
/// Deterministic regex/keyword classifier driven entirely by <see cref="FilterOptions"/>.
/// Assumes <see cref="ICandidateItem.PublishedAt"/> has already been resolved by
/// <see cref="IRelativeDateParser"/> before this is called; a null value is treated
/// upstream as "discard, could not determine the date" and never reaches here.
/// </summary>
public sealed class DeterministicJobClassifier(IOptions<FilterOptions> filterOptions, IContactExtractor contactExtractor, TimeProvider timeProvider)
    : IJobClassifier
{
    private readonly FilterOptions _options = filterOptions.Value;

    public ScoredItem Classify(ICandidateItem item)
    {
        var normalizedText = TextNormalizer.Normalize(item.FullText);
        var result = new ScoredItem { Item = item };

        if (item.PublishedAt is null)
        {
            result.DiscardReason = "Data de publicação não reconhecida";
            return result;
        }

        var age = timeProvider.GetUtcNow() - item.PublishedAt.Value;
        if (age.TotalDays > _options.MaxAgeDays)
        {
            result.DiscardReason = $"Publicado há mais de {_options.MaxAgeDays} dia(s)";
            return result;
        }

        var hasDotNet = ContainsAny(normalizedText, _options.DotNetKeywords);
        if (!hasDotNet)
        {
            result.DiscardReason = "Nenhuma tecnologia .NET mencionada";
            return result;
        }

        if (item.Source == SourceType.Post && !ContainsAny(normalizedText, _options.JobIndicatorKeywords))
        {
            result.DiscardReason = "Post não parece ser uma vaga/oportunidade";
            return result;
        }

        var weights = _options.Weights;
        var score = 0;

        score += weights.DotNetFocus;
        result.Reasons.Add($"Foco em .NET/C# (+{weights.DotNetFocus})");

        if (ContainsAny(normalizedText, _options.RemoteKeywords))
        {
            score += weights.Remote;
            result.Reasons.Add($"Remoto/home office (+{weights.Remote})");
        }

        if (ContainsAny(normalizedText, _options.SeniorKeywords))
        {
            score += weights.Senior;
            result.Reasons.Add($"Nível sênior (+{weights.Senior})");
        }

        var stackHits = _options.StackKeywords.Count(keyword => normalizedText.Contains(TextNormalizer.Normalize(keyword)));
        if (stackHits > 0)
        {
            var stackPoints = Math.Min(stackHits * weights.StackItemEach, weights.StackMax);
            score += stackPoints;
            result.Reasons.Add($"Tecnologias do perfil: {stackHits} (+{stackPoints})");
        }

        if (ContainsAny(normalizedText, _options.FinanceKeywords))
        {
            score += weights.Finance;
            result.Reasons.Add($"Domínio financeiro (+{weights.Finance})");
        }

        var contact = contactExtractor.Extract(item.FullText, item.AuthorName, item.AuthorHeadline);
        result.Contact = contact;
        if (contact.HasDirectContact)
        {
            score += weights.DirectContact;
            result.Reasons.Add($"Contato direto disponível (+{weights.DirectContact})");
        }

        var isHybrid = ContainsAny(normalizedText, _options.HybridKeywords);
        var isOnSite = ContainsAny(normalizedText, _options.OnSiteKeywords);
        var isInRio = ContainsAny(normalizedText, _options.RioDeJaneiroKeywords);

        if (isOnSite)
        {
            score += weights.OnSitePenalty;
            result.Reasons.Add($"Presencial obrigatório ({weights.OnSitePenalty})");
        }
        else if (isHybrid)
        {
            if (isInRio)
            {
                score += weights.HybridInRioPenalty;
                result.Reasons.Add($"Híbrido no Rio de Janeiro ({weights.HybridInRioPenalty})");
            }
            else
            {
                score += weights.HybridOutsideRioPenalty;
                result.Reasons.Add($"Híbrido fora do RJ ({weights.HybridOutsideRioPenalty})");
            }
        }

        if (ContainsAny(normalizedText, _options.JuniorLevelKeywords) && !ContainsAny(normalizedText, _options.SeniorKeywords))
        {
            score += weights.JuniorLevelPenalty;
            result.Reasons.Add($"Nível júnior/pleno/estágio ({weights.JuniorLevelPenalty})");
        }

        if (ContainsAny(normalizedText, _options.NonDotNetFocusKeywords))
        {
            score += weights.NonDotNetFocusPenalty;
            result.Reasons.Add($"Foco principal fora de .NET backend ({weights.NonDotNetFocusPenalty})");
        }

        result.Score = Math.Clamp(score, 0, 100);
        result.ContentHash = ContentHasher.ComputeHash(item.FullText);

        if (result.Score < _options.MinScore)
        {
            result.DiscardReason = $"Pontuação {result.Score} abaixo do mínimo ({_options.MinScore})";
        }

        return result;
    }

    private static bool ContainsAny(string normalizedText, IEnumerable<string> keywords)
        => keywords.Any(keyword => normalizedText.Contains(TextNormalizer.Normalize(keyword)));
}
