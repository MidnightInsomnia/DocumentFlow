using DocumentFlow.Application.Abstractions.Users;

namespace DocumentFlow.Worker;

public sealed class SystemCurrentUserContext : ICurrentUserContext
{
    public Guid? UserId => null;
    public string? UserName => null;
    public string? DisplayName => null;
    public string? Role => null;
    public bool IsAuthenticated => false;
}
