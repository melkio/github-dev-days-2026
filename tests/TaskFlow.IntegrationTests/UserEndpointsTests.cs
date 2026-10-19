using System.Net;
using System.Net.Http.Json;
using TaskFlow.Api.Features.Users;
using TaskFlow.IntegrationTests.Infrastructure;

namespace TaskFlow.IntegrationTests;

public sealed class UserEndpointsTests : ApiTestBase
{
    [Fact]
    public async Task GetUsers_ReturnsSeededUsersIncludingInactive()
    {
        var users = await Client.GetFromJsonAsync<UserDto[]>("/api/users");

        Assert.NotNull(users);
        Assert.Equal(4, users.Length);
        Assert.Contains(users, user => !user.IsActive);
        Assert.Equal(
            users.OrderBy(user => user.DisplayName).ThenBy(user => user.Id).Select(user => user.Id),
            users.Select(user => user.Id));
    }

    [Fact]
    public async Task GetUser_WhenMissing_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/users/30000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "{\"status\":\"Healthy\"}",
            await response.Content.ReadAsStringAsync());
    }
}
