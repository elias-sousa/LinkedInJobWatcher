using LinkedInJobWatcher.Core.Abstractions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Infrastructure.DependencyInjection;
using LinkedInJobWatcher.Infrastructure.LinkedIn;
using LinkedInJobWatcher.Infrastructure.Persistence;
using LinkedInJobWatcher.Worker.Cycles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "run";

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddLinkedInJobWatcherInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CycleRunner>();
builder.Services.AddHostedService<SchedulerBackgroundService>();

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

using var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JobWatcherDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

switch (command)
{
    case "login":
        await RunLoginAsync(host.Services);
        return;

    case "once":
        await RunCycleOnceAsync(host.Services, dryRun: false);
        return;

    case "dry-run":
        await RunCycleOnceAsync(host.Services, dryRun: true);
        return;

    case "test-email":
        await RunTestEmailAsync(host.Services);
        return;

    case "run":
        await host.RunAsync();
        return;

    default:
        Console.WriteLine($"Comando desconhecido: '{command}'.");
        Console.WriteLine("Comandos disponíveis: login | run | once | dry-run | test-email");
        return;
}

static async Task RunLoginAsync(IServiceProvider services)
{
    var sessionManager = services.GetRequiredService<LinkedInSessionManager>();
    await sessionManager.RunInteractiveLoginAsync();
    await sessionManager.DisposeAsync();
}

static async Task RunCycleOnceAsync(IServiceProvider services, bool dryRun)
{
    var cycleRunner = services.GetRequiredService<CycleRunner>();
    await cycleRunner.RunOnceAsync(dryRun, CancellationToken.None);

    var sessionManager = services.GetService<LinkedInSessionManager>();
    if (sessionManager is not null)
    {
        await sessionManager.DisposeAsync();
    }
}

static async Task RunTestEmailAsync(IServiceProvider services)
{
    var notifier = services.GetRequiredService<INotifier>();
    await notifier.SendTestEmailAsync(CancellationToken.None);
    Console.WriteLine("E-mail de teste enviado.");
}
