using System.Net;
using System.Net.Http.Json;
using TaskFlow.Api.Features.Tasks;
using TaskFlow.IntegrationTests.Infrastructure;
using TaskStatus = TaskFlow.Api.Features.Tasks.TaskStatus;

namespace TaskFlow.IntegrationTests;

public sealed class TaskEndpointsTests : ApiTestBase
{
    private const string FirstSeedTaskId = "20000000-0000-0000-0000-000000000001";

    [Fact]
    public async Task CreateTask_WithValidData_ReturnsCreatedAndLocation()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "  Prepare demo  ",
                description = "",
                id = Guid.Empty,
                status = "Done",
                createdAtUtc = DateTimeOffset.MinValue,
                updatedAtUtc = DateTimeOffset.MinValue
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.JsonOptions);
        Assert.NotNull(task);
        Assert.Equal($"/api/tasks/{task.Id:D}", location.OriginalString);
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Prepare demo", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero), task.CreatedAtUtc);
        Assert.Equal(task.CreatedAtUtc, task.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateTask_WithNullBody_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(
            "/api/tasks",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithoutTitle_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new { description = "Details" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateTask_WithWhitespaceTitle_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new { title = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTask_WhenExists_ReturnsTask()
    {
        var response = await Client.GetAsync($"/api/tasks/{FirstSeedTaskId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.JsonOptions);
        Assert.NotNull(task);
        Assert.Equal(Guid.Parse(FirstSeedTaskId), task.Id);
        Assert.Equal("Fix intermittent login timeout", task.Title);
    }

    [Fact]
    public async Task GetTask_WhenMissing_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/tasks/30000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("title", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListTasks_WithoutFilter_ReturnsTasksInStableOrder()
    {
        var tasks = await Client.GetFromJsonAsync<TaskDto[]>("/api/tasks", ApiFactory.JsonOptions);

        Assert.NotNull(tasks);
        Assert.Equal(
            Enumerable.Range(1, 6).Select(index =>
                Guid.Parse($"20000000-0000-0000-0000-{index:000000000000}")),
            tasks.Select(task => task.Id));
    }

    [Fact]
    public async Task ListTasks_WithStatusFilter_ReturnsMatchingTasks()
    {
        var tasks = await Client.GetFromJsonAsync<TaskDto[]>(
            "/api/tasks?status=inprogress",
            ApiFactory.JsonOptions);

        Assert.NotNull(tasks);
        Assert.Equal(2, tasks.Length);
        Assert.All(tasks, task => Assert.Equal(TaskStatus.InProgress, task.Status));
    }

    [Fact]
    public async Task ListTasks_WithInvalidStatus_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/tasks?status=blocked");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithValidData_UpdatesTitleAndDescription()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/tasks/{FirstSeedTaskId}",
            new { title = "  Updated title  ", description = (string?)null });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await Client.GetFromJsonAsync<TaskDto>(
            $"/api/tasks/{FirstSeedTaskId}",
            ApiFactory.JsonOptions);
        Assert.NotNull(task);
        Assert.Equal("Updated title", task.Title);
        Assert.Null(task.Description);
    }

    [Fact]
    public async Task UpdateTask_DoesNotChangeStatus()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/tasks/{FirstSeedTaskId}",
            new { title = "Updated title", description = "Updated details" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await Client.GetFromJsonAsync<TaskDto>(
            $"/api/tasks/{FirstSeedTaskId}",
            ApiFactory.JsonOptions);
        Assert.NotNull(task);
        Assert.Equal(TaskStatus.Todo, task.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithValidStatus_UpdatesTask()
    {
        var response = await Client.PatchAsJsonAsync(
            $"/api/tasks/{FirstSeedTaskId}/status",
            new { status = "done" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await Client.GetFromJsonAsync<TaskDto>(
            $"/api/tasks/{FirstSeedTaskId}",
            ApiFactory.JsonOptions);
        Assert.NotNull(task);
        Assert.Equal(TaskStatus.Done, task.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero), task.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var response = await Client.PatchAsJsonAsync(
            $"/api/tasks/{FirstSeedTaskId}/status",
            new { status = "blocked" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_WhenExists_RemovesTask()
    {
        var response = await Client.DeleteAsync($"/api/tasks/{FirstSeedTaskId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/tasks/{FirstSeedTaskId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteTask_WhenMissing_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/tasks/30000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
