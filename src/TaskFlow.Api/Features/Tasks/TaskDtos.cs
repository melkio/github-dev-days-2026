namespace TaskFlow.Api.Features.Tasks;

public sealed record CreateTaskRequest(string? Title, string? Description);

public sealed record UpdateTaskRequest(string? Title, string? Description);

public sealed record UpdateTaskStatusRequest(string? Status);

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
