namespace TaskFlow.Api.Features.Users;

public sealed record UserDto(Guid Id, string DisplayName, string Email, bool IsActive);
