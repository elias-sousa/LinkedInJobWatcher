using Microsoft.EntityFrameworkCore;

namespace LinkedInJobWatcher.Infrastructure.Persistence;

public sealed class JobWatcherDbContext(DbContextOptions<JobWatcherDbContext> options) : DbContext(options)
{
    public DbSet<SeenItemEntity> SeenItems => Set<SeenItemEntity>();

    public DbSet<DiscardedItemEntity> DiscardedItems => Set<DiscardedItemEntity>();

    public DbSet<RuntimeStateEntity> RuntimeStates => Set<RuntimeStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SeenItemEntity>(entity =>
        {
            entity.HasIndex(e => new { e.Source, e.ExternalId }).IsUnique();
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.CompanyTitleKey);
            entity.HasIndex(e => e.FirstSeenAtUtc);
        });

        modelBuilder.Entity<DiscardedItemEntity>(entity =>
        {
            entity.HasIndex(e => new { e.Source, e.ExternalId });
            entity.HasIndex(e => e.FirstSeenAtUtc);
        });
    }
}
