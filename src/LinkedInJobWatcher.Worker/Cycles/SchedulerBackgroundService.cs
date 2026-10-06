using LinkedInJobWatcher.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedInJobWatcher.Worker.Cycles;

/// <summary>Runs <see cref="CycleRunner"/> forever, respecting the configured execution window, interval and jitter.</summary>
public sealed class SchedulerBackgroundService(
    CycleRunner cycleRunner,
    IOptions<ScheduleOptions> scheduleOptions,
    TimeProvider timeProvider,
    ILogger<SchedulerBackgroundService> logger) : BackgroundService
{
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsWithinWindow(timeProvider.GetUtcNow()))
            {
                var wait = TimeUntilNextWindow(timeProvider.GetUtcNow());
                logger.LogInformation("Fora da janela de execução; aguardando {Wait}.", wait);
                await DelaySafeAsync(wait, stoppingToken);
                continue;
            }

            try
            {
                await cycleRunner.RunOnceAsync(dryRun: false, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo encerrou com um erro não tratado; continuando no próximo ciclo.");
            }

            var options = scheduleOptions.Value;
            var jitterMinutes = _random.Next(-options.JitterMinutes, options.JitterMinutes + 1);
            var delay = TimeSpan.FromMinutes(Math.Max(1, options.IntervalMinutes + jitterMinutes));
            logger.LogInformation("Próximo ciclo em {Delay}.", delay);
            await DelaySafeAsync(delay, stoppingToken);
        }
    }

    private bool IsWithinWindow(DateTimeOffset nowUtc)
    {
        var local = nowUtc.ToLocalTime();
        var options = scheduleOptions.Value;

        if (!options.ActiveDays.Contains(local.DayOfWeek))
        {
            return false;
        }

        return local.Hour >= options.WindowStartHour && local.Hour < options.WindowEndHour;
    }

    private TimeSpan TimeUntilNextWindow(DateTimeOffset nowUtc)
    {
        var local = nowUtc.ToLocalTime();
        var options = scheduleOptions.Value;

        for (var daysAhead = 0; daysAhead <= 7; daysAhead++)
        {
            var candidateDate = local.Date.AddDays(daysAhead);
            var candidateDayOfWeek = candidateDate.DayOfWeek;

            if (!options.ActiveDays.Contains(candidateDayOfWeek))
            {
                continue;
            }

            var windowStart = new DateTimeOffset(candidateDate, local.Offset).AddHours(options.WindowStartHour);
            if (windowStart > local)
            {
                return windowStart - local;
            }
        }

        return TimeSpan.FromHours(1);
    }

    private static async Task DelaySafeAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}
