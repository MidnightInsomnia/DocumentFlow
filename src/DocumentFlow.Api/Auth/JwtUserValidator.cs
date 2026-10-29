using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DocumentFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DocumentFlow.Api.Auth;

public static class JwtUserValidator
{
    public static async Task<bool> IsValidAsync(UserManager<ApplicationUser> manager, ClaimsPrincipal? principal)
    {
        if (!Guid.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)) return false;
        var user = await manager.FindByIdAsync(id.ToString());
        var stamp = principal!.FindFirstValue("security_stamp");
        var role = principal.FindFirstValue("role");
        return user is { IsActive: true } && !string.IsNullOrEmpty(stamp) &&
               string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal) &&
               ApplicationRoles.IsValid(role) && await manager.IsInRoleAsync(user, role!);
    }
}
