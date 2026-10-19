using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskFlow.Api.Features.Tasks;
using TaskFlow.Api.Features.Users;
using TaskStatus = TaskFlow.Api.Features.Tasks.TaskStatus;

namespace TaskFlow.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTimeOffset, string> UtcDateTimeOffsetConverter = new(
        value => value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        value => DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(150).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(2000);
            entity.Property(task => task.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(task => task.CreatedAtUtc)
                .HasConversion(UtcDateTimeOffsetConverter)
                .HasColumnType("TEXT")
                .IsRequired();
            entity.Property(task => task.UpdatedAtUtc)
                .HasConversion(UtcDateTimeOffsetConverter)
                .HasColumnType("TEXT")
                .IsRequired();
            entity.HasIndex(task => task.Status);
            entity.HasIndex(task => new { task.CreatedAtUtc, task.Id });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).UseCollation("NOCASE").IsRequired();
            entity.Property(user => user.IsActive).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => new { user.DisplayName, user.Id });
        });
    }
}
