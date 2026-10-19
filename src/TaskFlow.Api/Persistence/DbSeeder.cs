using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Features.Tasks;
using TaskFlow.Api.Features.Users;
using TaskStatus = TaskFlow.Api.Features.Tasks.TaskStatus;

namespace TaskFlow.Api.Persistence;

public static class DbSeeder
{
    private static readonly User[] SeedUsers =
    [
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            DisplayName = "Alex Chen",
            Email = "alex.chen@example.test",
            IsActive = true
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            DisplayName = "Jordan Rivera",
            Email = "jordan.rivera@example.test",
            IsActive = true
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            DisplayName = "Morgan Patel",
            Email = "morgan.patel@example.test",
            IsActive = false
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            DisplayName = "Sam Taylor",
            Email = "sam.taylor@example.test",
            IsActive = true
        }
    ];

    private static readonly TaskItem[] SeedTasks =
    [
        CreateTask(
            "20000000-0000-0000-0000-000000000001",
            "Fix intermittent login timeout",
            "Trace the token refresh race reported by the staging team.",
            TaskStatus.Todo,
            new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero)),
        CreateTask(
            "20000000-0000-0000-0000-000000000002",
            "Document the release checklist",
            "Capture the verification steps used before each production release.",
            TaskStatus.InProgress,
            new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero)),
        CreateTask(
            "20000000-0000-0000-0000-000000000003",
            "Refactor notification preferences",
            "Remove duplicate preference mapping from the settings handlers.",
            TaskStatus.Done,
            new DateTimeOffset(2026, 9, 5, 15, 30, 0, TimeSpan.Zero)),
        CreateTask(
            "20000000-0000-0000-0000-000000000004",
            "Add API response contract tests",
            "Cover validation and not-found responses for task endpoints.",
            TaskStatus.Todo,
            new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero)),
        CreateTask(
            "20000000-0000-0000-0000-000000000005",
            "Upgrade the build pipeline",
            "Update the CI SDK image and verify the integration test stage.",
            TaskStatus.InProgress,
            new DateTimeOffset(2026, 9, 3, 9, 15, 0, TimeSpan.Zero)),
        CreateTask(
            "20000000-0000-0000-0000-000000000006",
            "Publish the v1.0 release notes",
            "Summarize the shipped API behavior and known limitations.",
            TaskStatus.Done,
            new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero))
    ];

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var userIds = SeedUsers.Select(user => user.Id).ToArray();
        var existingUserIds = await dbContext.Users
            .Where(user => userIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        dbContext.Users.AddRange(SeedUsers.Where(user => !existingUserIds.Contains(user.Id)));

        var taskIds = SeedTasks.Select(task => task.Id).ToArray();
        var existingTaskIds = await dbContext.Tasks
            .Where(task => taskIds.Contains(task.Id))
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
        dbContext.Tasks.AddRange(SeedTasks.Where(task => !existingTaskIds.Contains(task.Id)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TaskItem CreateTask(
        string id,
        string title,
        string description,
        TaskStatus status,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            Id = Guid.Parse(id),
            Title = title,
            Description = description,
            Status = status,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };
}
