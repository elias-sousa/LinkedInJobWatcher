using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Infrastructure.Backoff;
using LinkedInJobWatcher.Infrastructure.DependencyInjection;
using LinkedInJobWatcher.Infrastructure.LinkedIn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedInJobWatcher.Worker.Cycles;

/// <summary>
/// Runs one search-and-notify cycle: picks the next terms in the rotation, scrapes them,
/// classifies and deduplicates the results, and (unless this is a dry run) sends one e-mail
/// with everything new. All exceptions are caught here so a single bad cycle never takes
/// the whole worker down.
/// </summary>
public sealed class CycleRunner(
    IServiceScopeFactory scopeFactory,
    [FromKeyedServices(SourceKeys.Post)] IJobSource postSource,
    [FromKeyedServices(SourceKeys.Job)] IJobSource jobSource,
    IRelativeDateParser dateParser,
    IJobClassifier classifier,
    INotifier notifier,
    IOptions<LinkedInOptions> linkedInOptions,
    IOptions<ScheduleOptions> scheduleOptions,
    IOptions<EmailOptions> emailOptions,
    TimeProvider timeProvider,
    ILogger<CycleRunner> logger)
{
    public async Task RunOnceAsync(bool dryRun, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var state = await repository.GetRuntimeStateAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (state.BackoffUntil is { } backoffUntil && backoffUntil > now)
        {
            logger.LogInformation("Em backoff até {BackoffUntil:u}; ciclo pulado.", backoffUntil);
            return;
        }

        var searches = SelectSearchesForCycle(state);

        var rawItems = new List<ICandidateItem>();

        try
        {
            foreach (var (source, term) in searches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var items = await source.SearchAsync(term, cancellationToken);
                rawItems.AddRange(items);
            }

            state.ConsecutiveFailures = 0;
        }
        catch (SessionExpiredException ex)
        {
            logger.LogWarning(ex, "Sessão do LinkedIn expirada.");
            await HandleSessionExpiredAsync(repository, state, cancellationToken);
            return;
        }
        catch (BlockedException ex)
        {
            logger.LogWarning(ex, "Bloqueio/CAPTCHA detectado.");
            await HandleBlockedAsync(repository, state, ex.Message, cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado durante o ciclo de busca.");
            return;
        }

        if (!dryRun)
        {
            await repository.SaveRuntimeStateAsync(state, cancellationToken);
        }

        var approved = new List<ScoredItem>();

        foreach (var item in rawItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!dryRun && await repository.WasDiscardedAsync(item.Source, item.ExternalId, cancellationToken))
            {
                continue;
            }

            if (!dateParser.TryParse(item.RelativeDateText, now, out var publishedAt))
            {
                logger.LogInformation("Item {Source}/{ExternalId} descartado: data '{RelativeDate}' não reconhecida.", item.Source, item.ExternalId, item.RelativeDateText);
                if (!dryRun)
                {
                    await repository.SaveDiscardedAsync(item, "Data de publicação não reconhecida", cancellationToken);
                }

                continue;
            }

            item.PublishedAt = publishedAt;

            var scored = classifier.Classify(item);

            if (scored.IsDiscarded)
            {
                logger.LogInformation("Item {Source}/{ExternalId} descartado: {Reason}", item.Source, item.ExternalId, scored.DiscardReason);
                if (!dryRun)
                {
                    await repository.SaveDiscardedAsync(item, scored.DiscardReason!, cancellationToken);
                }

                continue;
            }

            if (!dryRun && await repository.IsDuplicateAsync(item, scored.ContentHash, cancellationToken))
            {
                logger.LogInformation("Item {Source}/{ExternalId} ignorado: duplicado.", item.Source, item.ExternalId);
                continue;
            }

            approved.Add(scored);
        }

        if (dryRun)
        {
            PrintDryRun(approved);
            return;
        }

        foreach (var scored in approved)
        {
            await repository.SaveSeenAsync(scored, scored.ContentHash, cancellationToken);
        }

        var pending = await repository.GetUnnotifiedAsync(cancellationToken);
        if (pending.Count == 0)
        {
            logger.LogInformation("Nenhuma vaga nova neste ciclo.");
            return;
        }

        try
        {
            await notifier.SendNewItemsAsync(pending.Select(p => p.Item).ToList(), cancellationToken);
            await repository.MarkNotifiedAsync(pending.Select(p => p.Id).ToList(), cancellationToken);
            logger.LogInformation("E-mail enviado com {Count} vaga(s) nova(s).", pending.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar e-mail de notificação; será tentado novamente no próximo ciclo.");
        }
    }

    private List<(IJobSource Source, string Term)> SelectSearchesForCycle(RuntimeState state)
    {
        var searches = new List<(IJobSource Source, string Term)>();
        var budget = scheduleOptions.Value.MaxSearchesPerCycle;

        var postTerms = linkedInOptions.Value.PostSearchTerms;
        var jobTerms = linkedInOptions.Value.JobSearchTerms;

        if (budget >= 1 && postTerms.Count > 0)
        {
            var term = postTerms[state.PostTermIndex % postTerms.Count];
            searches.Add((postSource, term));
            state.PostTermIndex = (state.PostTermIndex + 1) % postTerms.Count;
        }

        if (budget >= 2 && jobTerms.Count > 0)
        {
            var term = jobTerms[state.JobTermIndex % jobTerms.Count];
            searches.Add((jobSource, term));
            state.JobTermIndex = (state.JobTermIndex + 1) % jobTerms.Count;
        }

        return searches;
    }

    private async Task HandleSessionExpiredAsync(IJobRepository repository, RuntimeState state, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cooldown = TimeSpan.FromHours(emailOptions.Value.SessionAlertCooldownHours);

        if (state.LastSessionExpiredAlertAt is null || now - state.LastSessionExpiredAlertAt >= cooldown)
        {
            try
            {
                await notifier.SendSessionExpiredAlertAsync(cancellationToken);
                state.LastSessionExpiredAlertAt = now;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar alerta de sessão expirada.");
            }
        }

        await repository.SaveRuntimeStateAsync(state, cancellationToken);
    }

    private async Task HandleBlockedAsync(IJobRepository repository, RuntimeState state, string reason, CancellationToken cancellationToken)
    {
        state.ConsecutiveFailures++;
        var delay = BackoffPolicy.GetDelay(state.ConsecutiveFailures);
        state.BackoffUntil = timeProvider.GetUtcNow() + delay;

        try
        {
            await notifier.SendBlockedAlertAsync(reason, delay, cancellationToken);
            state.LastBlockedAlertAt = timeProvider.GetUtcNow();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar alerta de bloqueio.");
        }

        await repository.SaveRuntimeStateAsync(state, cancellationToken);
    }

    private void PrintDryRun(IReadOnlyList<ScoredItem> approved)
    {
        Console.WriteLine();
        Console.WriteLine($"[dry-run] {approved.Count} item(ns) seriam notificados neste ciclo:");
        Console.WriteLine();

        foreach (var scored in approved.OrderByDescending(s => s.Score))
        {
            Console.WriteLine($"- [{scored.Score} pts] {scored.Item.Title ?? scored.Item.AuthorName ?? "(sem título)"} | {scored.Item.Company} | {scored.Item.Url}");
            foreach (var reason in scored.Reasons)
            {
                Console.WriteLine($"    {reason}");
            }
        }

        if (approved.Count == 0)
        {
            Console.WriteLine("(nenhum item aprovado neste ciclo)");
        }

        Console.WriteLine();
    }
}
