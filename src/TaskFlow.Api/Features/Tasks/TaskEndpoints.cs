using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Features.Tasks;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tasks").WithTags("Tasks");
        group.MapGet("", ListAsync);
        group.MapGet("/{id}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id}", UpdateAsync);
        group.MapPatch("/{id}/status", UpdateStatusAsync);
        group.MapDelete("/{id}", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        string? status,
        TaskService taskService,
        CancellationToken cancellationToken)
    {
        TaskStatus? parsedStatus = null;
        if (status is not null)
        {
            if (!TryParseStatus(status, out var value))
            {
                return ValidationError("status", "Status must be Todo, InProgress, or Done.");
            }

            parsedStatus = value;
        }

        var tasks = await taskService.ListAsync(parsedStatus, cancellationToken);
        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetAsync(
        string id,
        TaskService taskService,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(id, out var taskId))
        {
            return ValidationError("id", "A valid task GUID is required.");
        }

        var task = await taskService.GetAsync(taskId, cancellationToken);
        return task is null
            ? NotFound("Task")
            : Results.Ok(task);
    }

    private static async Task<IResult> CreateAsync(
        CreateTaskRequest? request,
        TaskService taskService,
        CancellationToken cancellationToken)
    {
        var title = request?.Title;
        var description = request?.Description;
        var errors = ValidateTaskInput(title, description);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var task = await taskService.CreateAsync(
            title!.Trim(),
            NormalizeDescription(description),
            cancellationToken);
        return Results.Created($"/api/tasks/{task.Id:D}", task);
    }

    private static async Task<IResult> UpdateAsync(
        string id,
        UpdateTaskRequest? request,
        TaskService taskService,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(id, out var taskId))
        {
            return ValidationError("id", "A valid task GUID is required.");
        }

        var title = request?.Title;
        var description = request?.Description;
        var errors = ValidateTaskInput(title, description);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var updated = await taskService.UpdateAsync(
            taskId,
            title!.Trim(),
            NormalizeDescription(description),
            cancellationToken);
        return updated ? Results.NoContent() : NotFound("Task");
    }

    private static async Task<IResult> UpdateStatusAsync(
        string id,
        UpdateTaskStatusRequest? request,
        TaskService taskService,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(id, out var taskId))
        {
            return ValidationError("id", "A valid task GUID is required.");
        }

        var requestedStatus = request?.Status;
        if (requestedStatus is null || !TryParseStatus(requestedStatus, out var status))
        {
            return ValidationError("status", "Status must be Todo, InProgress, or Done.");
        }

        var updated = await taskService.UpdateStatusAsync(taskId, status, cancellationToken);
        return updated ? Results.NoContent() : NotFound("Task");
    }

    private static async Task<IResult> DeleteAsync(
        string id,
        TaskService taskService,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(id, out var taskId))
        {
            return ValidationError("id", "A valid task GUID is required.");
        }

        var deleted = await taskService.DeleteAsync(taskId, cancellationToken);
        return deleted ? Results.NoContent() : NotFound("Task");
    }

    private static Dictionary<string, string[]> ValidateTaskInput(string? title, string? description)
    {
        var errors = new Dictionary<string, string[]>();
        if (title is null)
        {
            errors["title"] = ["Title is required."];
        }
        else if (title.Trim().Length is < 1 or > 150)
        {
            errors["title"] = ["Title must contain between 1 and 150 characters."];
        }

        if (description?.Length > 2000)
        {
            errors["description"] = ["Description must be 2000 characters or fewer."];
        }

        return errors;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrEmpty(description) ? null : description;

    private static bool TryParseId(string id, out Guid parsedId) =>
        Guid.TryParseExact(id, "D", out parsedId);

    private static bool TryParseStatus(string status, out TaskStatus parsedStatus)
    {
        var matchingName = Enum.GetNames<TaskStatus>()
            .FirstOrDefault(name => name.Equals(status, StringComparison.OrdinalIgnoreCase));
        return Enum.TryParse(matchingName, out parsedStatus);
    }

    private static IResult ValidationError(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static IResult NotFound(string entity) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"{entity} was not found.");
}
