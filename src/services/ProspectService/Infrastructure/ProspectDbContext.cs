using Microsoft.EntityFrameworkCore;
using ProspectService.Domain;

namespace ProspectService.Infrastructure;

/// <summary>
/// Database context for ProspectService write model.
/// Includes Prospects table and Outbox table for transactional outbox pattern.
/// </summary>
public class ProspectDbContext : DbContext
{
    public ProspectDbContext(DbContextOptions<ProspectDbContext> options)
        : base(options)
    {
    }

    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Prospect entity
        modelBuilder.Entity<Prospect>(entity =>
        {
            entity.ToTable("Prospects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Phone)
                .HasMaxLength(50);

            entity.Property(e => e.Source)
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Notes)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();
        });

        // Configure Outbox entity
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("Outbox");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EventId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.EventId)
                .IsUnique();

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Payload)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.Published)
                .IsRequired();

            entity.Property(e => e.PublishedAt);

            entity.HasIndex(e => new { e.Published, e.CreatedAt });
        });
    }
}
