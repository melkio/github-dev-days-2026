using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Persistence;

namespace TaskFlow.Api.Features.Tasks;

public sealed class TaskService(
    AppDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<TaskService> logger)
{
    public async Task<IReadOnlyList<TaskDto>> ListAsync(
        TaskStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tasks.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        return await query
            .OrderByDescending(task => task.CreatedAtUtc)
            .ThenBy(task => task.Id)
            .Select(task => ToDto(task))
            .ToListAsync(cancellationToken);
    }

    public Task<TaskDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Id == id)
            .Select(task => ToDto(task))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TaskDto> CreateAsync(
        string title,
        string? description,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Status = TaskStatus.Todo,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created task {TaskId}", task.Id);
        return ToDto(task);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        string title,
        string? description,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (task is null)
        {
            return false;
        }

        task.Title = title;
        task.Description = description;
        task.UpdatedAtUtc = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated task {TaskId}", task.Id);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(
        Guid id,
        TaskStatus status,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (task is null)
        {
            return false;
        }

        if (task.Status != status)
        {
            task.Status = status;
            task.UpdatedAtUtc = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Changed task {TaskId} status to {Status}", task.Id, status);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (task is null)
        {
            return false;
        }

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted task {TaskId}", task.Id);
        return true;
    }

    private static TaskDto ToDto(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.CreatedAtUtc,
            task.UpdatedAtUtc);
}
