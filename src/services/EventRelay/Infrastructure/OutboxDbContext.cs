using Microsoft.EntityFrameworkCore;

namespace EventRelay.Infrastructure;

/// <summary>
/// Database context for reading from Outbox table.
/// Read-only except for marking events as Published.
/// </summary>
public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("Outbox");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Payload)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.Published)
                .IsRequired();

            entity.HasIndex(e => new { e.Published, e.CreatedAt })
                .HasDatabaseName("IX_Outbox_Published_CreatedAt");
        });
    }
}
