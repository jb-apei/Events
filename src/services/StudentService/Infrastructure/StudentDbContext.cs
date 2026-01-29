using Microsoft.EntityFrameworkCore;
using StudentService.Domain;

namespace StudentService.Infrastructure;

/// <summary>
/// Database context for StudentService write model.
/// Includes Students table and Outbox table for transactional outbox pattern.
/// </summary>
public class StudentDbContext : DbContext
{
    public StudentDbContext(DbContextOptions<StudentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
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

            entity.Property(e => e.StudentNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.StudentNumber)
                .IsUnique();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.EnrollmentDate)
                .IsRequired();

            entity.Property(e => e.ExpectedGraduationDate);

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
