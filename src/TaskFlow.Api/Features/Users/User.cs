namespace TaskFlow.Api.Features.Users;

public sealed class User
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
}
