using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Persistence;

namespace TaskFlow.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").WithTags("Users");
        group.MapGet("", ListAsync);
        group.MapGet("/{id}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Id)
            .Select(user => new UserDto(user.Id, user.DisplayName, user.Email, user.IsActive))
            .ToListAsync(cancellationToken);
        return Results.Ok(users);
    }

    private static async Task<IResult> GetAsync(
        string id,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(id, "D", out var userId))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["id"] = ["A valid user GUID is required."] });
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new UserDto(item.Id, item.DisplayName, item.Email, item.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        return user is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "User was not found.")
            : Results.Ok(user);
    }
}
