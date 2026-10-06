using FluentAssertions;
using LinkedInJobWatcher.Core.Configuration;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinkedInJobWatcher.Tests.Persistence;

public class SqliteJobRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"linkedin-job-watcher-tests-{Guid.NewGuid():N}.db");
    private readonly JobWatcherDbContext _dbContext;
    private readonly SqliteJobRepository _repository;

    public SqliteJobRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<JobWatcherDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=False")
            .Options;

        _dbContext = new JobWatcherDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new SqliteJobRepository(_dbContext, Options.Create(new StorageOptions()), new FakeTimeProvider(Now));
    }

    private static JobItem CreateJob(string externalId, string company = "Acme", string title = "Desenvolvedor .NET Senior") => new()
    {
        ExternalId = externalId,
        Url = $"https://www.linkedin.com/jobs/view/{externalId}/",
        Title = title,
        Company = company,
        FullText = "Vaga para Desenvolvedor .NET Senior remoto",
        RelativeDateText = "1d",
        PublishedAt = Now - TimeSpan.FromHours(10)
    };

    private static ScoredItem CreateScored(JobItem job, int score = 80)
    {
        var scored = new ScoredItem { Item = job, Score = score, ContentHash = "hash-" + job.ExternalId };
        scored.Reasons.Add("teste");
        return scored;
    }

    [Fact]
    public async Task IsDuplicateAsync_SameExternalId_ReturnsTrue()
    {
        var job = CreateJob("job-1");
        var scored = CreateScored(job);
        await _repository.SaveSeenAsync(scored, scored.ContentHash, CancellationToken.None);

        var isDuplicate = await _repository.IsDuplicateAsync(job, "different-hash", CancellationToken.None);

        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_SameContentHashDifferentExternalId_ReturnsTrue()
    {
        var first = CreateJob("job-1");
        var scored = CreateScored(first);
        await _repository.SaveSeenAsync(scored, "shared-hash", CancellationToken.None);

        var repost = CreateJob("job-2");

        var isDuplicate = await _repository.IsDuplicateAsync(repost, "shared-hash", CancellationToken.None);

        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_SameCompanyAndTitleWithinWindow_ReturnsTrue()
    {
        var first = CreateJob("job-1", company: "Acme Tecnologia", title: "Desenvolvedor .NET Senior");
        var scored = CreateScored(first);
        await _repository.SaveSeenAsync(scored, "hash-a", CancellationToken.None);

        var repostedByAnotherPerson = CreateJob("job-2", company: "Acme Tecnologia", title: "Desenvolvedor .NET Senior");

        var isDuplicate = await _repository.IsDuplicateAsync(repostedByAnotherPerson, "hash-b", CancellationToken.None);

        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_DifferentJob_ReturnsFalse()
    {
        var first = CreateJob("job-1", company: "Acme Tecnologia");
        var scored = CreateScored(first);
        await _repository.SaveSeenAsync(scored, "hash-a", CancellationToken.None);

        var different = CreateJob("job-2", company: "Beta Software");

        var isDuplicate = await _repository.IsDuplicateAsync(different, "hash-b", CancellationToken.None);

        isDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnnotifiedAsync_AfterSave_ReturnsItemWithReconstructedScoredItem()
    {
        var job = CreateJob("job-1");
        var scored = CreateScored(job, score: 77);
        await _repository.SaveSeenAsync(scored, scored.ContentHash, CancellationToken.None);

        var pending = await _repository.GetUnnotifiedAsync(CancellationToken.None);

        pending.Should().HaveCount(1);
        pending[0].Item.Score.Should().Be(77);
        pending[0].Item.Item.ExternalId.Should().Be("job-1");
    }

    [Fact]
    public async Task MarkNotifiedAsync_RemovesItemFromUnnotifiedList()
    {
        var job = CreateJob("job-1");
        var scored = CreateScored(job);
        var id = await _repository.SaveSeenAsync(scored, scored.ContentHash, CancellationToken.None);

        await _repository.MarkNotifiedAsync([id], CancellationToken.None);

        var pending = await _repository.GetUnnotifiedAsync(CancellationToken.None);
        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task WasDiscardedAsync_AfterSaveDiscarded_ReturnsTrue()
    {
        var job = CreateJob("job-discarded");
        await _repository.SaveDiscardedAsync(job, "Nível júnior", CancellationToken.None);

        var wasDiscarded = await _repository.WasDiscardedAsync(SourceType.Job, "job-discarded", CancellationToken.None);

        wasDiscarded.Should().BeTrue();
    }

    [Fact]
    public async Task PurgeOlderThanAsync_RemovesOnlyOldRows()
    {
        var oldJob = CreateJob("old-job");
        var oldScored = CreateScored(oldJob);
        await _repository.SaveSeenAsync(oldScored, "hash-old", CancellationToken.None);

        var purged = await _repository.PurgeOlderThanAsync(TimeSpan.FromSeconds(-1), CancellationToken.None);

        purged.Should().Be(1);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; the temp file will be removed by the OS eventually.
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
