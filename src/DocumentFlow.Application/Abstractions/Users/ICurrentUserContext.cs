namespace DocumentFlow.Application.Abstractions.Users;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
