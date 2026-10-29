using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DocumentFlow.Api.Auth;
using DocumentFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, JwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
            return Unauthorized(new { message = "Invalid credentials." });
        var user = await users.FindByNameAsync(request.UserName);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials." });
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials." });
        var roles = await users.GetRolesAsync(user);
        var role = roles.SingleOrDefault();
        if (!ApplicationRoles.IsValid(role)) return Unauthorized(new { message = "Invalid credentials." });
        var issued = tokens.CreateWithExpiry(user, role!);
        return Ok(new
        {
            issued.AccessToken,
            issued.ExpiresAtUtc,
            User = new { user.Id, user.UserName, user.DisplayName, Role = role }
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();
        return Ok(new UserView(user.Id, user.UserName!, user.Email, user.DisplayName, user.IsActive,
            User.FindFirstValue("role")!, user.CreatedAtUtc));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return BadRequest(IdentityApiErrors.From(result));
        await users.UpdateSecurityStampAsync(user);
        return NoContent();
    }

    private Task<ApplicationUser?> CurrentUserAsync() => users.FindByIdAsync(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty);
}

public sealed record LoginRequest(string UserName, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record UserView(Guid Id, string UserName, string? Email, string DisplayName, bool IsActive, string Role, DateTime CreatedAtUtc);
