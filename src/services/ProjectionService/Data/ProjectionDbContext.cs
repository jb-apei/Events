using Microsoft.EntityFrameworkCore;
using ProjectionService.Projections;

namespace ProjectionService.Data;

/// <summary>
/// DbContext for read model projections and inbox table.
/// This uses a separate database from the write model (transactional DB).
/// </summary>
public class ProjectionDbContext : DbContext
{
    public ProjectionDbContext(DbContextOptions<ProjectionDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Inbox table for event idempotency tracking.
    /// </summary>
    public DbSet<InboxMessage> Inbox { get; set; } = null!;

    /// <summary>
    /// Read model projection for Prospect list queries.
    /// </summary>
    public DbSet<ProspectSummary> ProspectSummary { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Inbox
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.EventType);
        });

        // Configure ProspectSummary
        modelBuilder.Entity<ProspectSummary>(entity =>
        {
            entity.HasKey(e => e.ProspectId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UpdatedAt);

            // Configure optimistic concurrency
            entity.Property(e => e.Version)
                .IsConcurrencyToken();
        });
    }
}
