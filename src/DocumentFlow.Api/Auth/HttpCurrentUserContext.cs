using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DocumentFlow.Application.Abstractions.Users;

namespace DocumentFlow.Api.Auth;

public sealed class HttpCurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
    public string? UserName => Principal?.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
    public string? DisplayName => Principal?.FindFirstValue("display_name");
    public string? Role => Principal?.FindFirstValue("role");
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
