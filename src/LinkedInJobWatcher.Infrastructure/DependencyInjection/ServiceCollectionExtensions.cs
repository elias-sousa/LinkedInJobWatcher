using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Parsing;
using LinkedInJobWatcher.Core.Scoring;
using LinkedInJobWatcher.Infrastructure.Email;
using LinkedInJobWatcher.Infrastructure.LinkedIn;
using LinkedInJobWatcher.Infrastructure.Net;
using LinkedInJobWatcher.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkedInJobWatcher.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLinkedInJobWatcherInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LinkedInOptions>(configuration.GetSection(LinkedInOptions.SectionName));
        services.Configure<FilterOptions>(configuration.GetSection(FilterOptions.SectionName));
        services.Configure<ScheduleOptions>(configuration.GetSection(ScheduleOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton(TimeProvider.System);

        var storagePath = Path.GetFullPath(configuration[$"{StorageOptions.SectionName}:{nameof(StorageOptions.DatabasePath)}"] ?? "./data/linkedin-job-watcher.db");
        Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);

        services.AddDbContext<JobWatcherDbContext>(dbOptions => dbOptions.UseSqlite($"Data Source={storagePath}"));
        services.AddScoped<IJobRepository, SqliteJobRepository>();

        services.AddSingleton<IRelativeDateParser, RelativeDateParser>();
        services.AddSingleton<IContactExtractor, ContactExtractor>();
        services.AddSingleton<IJobClassifier, DeterministicJobClassifier>();

        services.AddHttpClient(nameof(HttpShortLinkResolver));
        services.AddSingleton<IShortLinkResolver, HttpShortLinkResolver>();

        services.AddSingleton<LinkedInSessionManager>();
        services.AddSingleton<HumanBehavior>();
        services.AddSingleton<PostHtmlParser>();
        services.AddSingleton<JobHtmlParser>();
        services.AddKeyedSingleton<IJobSource, LinkedInPostSource>(SourceKeys.Post);
        services.AddKeyedSingleton<IJobSource, LinkedInJobSource>(SourceKeys.Job);

        services.AddSingleton<INotifier, SmtpNotifier>();

        return services;
    }
}

public static class SourceKeys
{
    public const string Post = "post";
    public const string Job = "job";
}
