using System.Data;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Api.Auth;
using DocumentFlow.Infrastructure.Identity;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController(UserManager<ApplicationUser> users, DocumentFlowDbContext db,
    ICurrentUserContext currentUser, ILogger<AdminUsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var literal = search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{literal}%";
            query = query.Where(u => EF.Functions.ILike(u.UserName!, pattern, "\\") ||
                                     EF.Functions.ILike(u.DisplayName, pattern, "\\"));
        }
        var totalCount = await query.CountAsync();
        var offset = ((long)page - 1) * pageSize;
        var items = offset > int.MaxValue ? new List<UserView>() : await query
            .OrderBy(u => u.UserName).ThenBy(u => u.Id)
            .Skip((int)offset).Take(pageSize)
            .Select(u => new UserView(u.Id, u.UserName!, u.Email, u.DisplayName, u.IsActive,
                db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, role => role.Id, (_, role) => role.Name!)
                    .FirstOrDefault() ?? string.Empty, u.CreatedAtUtc))
            .ToListAsync();
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (!ApplicationRoles.IsValid(request.Role) || string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = request.UserName, Email = request.Email,
            DisplayName = request.DisplayName, IsActive = true
        };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return BadRequest(IdentityApiErrors.From(created));
        var assigned = await users.AddToRoleAsync(user, request.Role);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            return BadRequest(IdentityApiErrors.From(assigned));
        }
        logger.LogInformation(new EventId(1001, "AdminUserCreated"),
            "Admin user created ActorId {ActorId} TargetUserId {TargetUserId}", currentUser.UserId, user.Id);
        return Created($"/api/admin/users/{user.Id}", View(user, request.Role));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, SetRoleRequest request)
    {
        if (!ApplicationRoles.IsValid(request.Role)) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await LockAdministrators();
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        var current = await users.GetRolesAsync(user);
        if (current.Count == 1 && current[0] == request.Role) return Ok(View(user, request.Role));
        if (user.IsActive && current.Contains(ApplicationRoles.Admin) && request.Role != ApplicationRoles.Admin &&
            AdminSafety.WouldRemoveLastActiveAdmin(id, await users.GetUsersInRoleAsync(ApplicationRoles.Admin)))
            return Conflict(new { message = AdminSafety.LastAdminError });
        if (current.Count > 0)
        {
            var removed = await users.RemoveFromRolesAsync(user, current);
            if (!removed.Succeeded) return BadRequest(IdentityApiErrors.From(removed));
        }
        var added = await users.AddToRoleAsync(user, request.Role);
        if (!added.Succeeded) return BadRequest(IdentityApiErrors.From(added));
        await users.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync();
        logger.LogInformation(new EventId(1002, "AdminUserRoleChanged"),
            "Admin user role changed ActorId {ActorId} TargetUserId {TargetUserId}", currentUser.UserId, user.Id);
        return Ok(View(user, request.Role));
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetActiveRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await LockAdministrators();
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        var role = (await users.GetRolesAsync(user)).SingleOrDefault() ?? string.Empty;
        if (user.IsActive && !request.IsActive && role == ApplicationRoles.Admin &&
            AdminSafety.WouldRemoveLastActiveAdmin(id, await users.GetUsersInRoleAsync(ApplicationRoles.Admin)))
            return Conflict(new { message = AdminSafety.LastAdminError });
        var updatedActive = user.IsActive != request.IsActive;
        if (updatedActive)
        {
            user.IsActive = request.IsActive;
            var updated = await users.UpdateAsync(user);
            if (!updated.Succeeded) return BadRequest(IdentityApiErrors.From(updated));
            await users.UpdateSecurityStampAsync(user);
        }
        await transaction.CommitAsync();
        if (updatedActive)
            logger.LogInformation(new EventId(1003, "AdminUserActiveChanged"),
                "Admin user active state changed ActorId {ActorId} TargetUserId {TargetUserId}", currentUser.UserId, user.Id);
        return Ok(View(user, role));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded) return BadRequest(IdentityApiErrors.From(result));
        await users.UpdateSecurityStampAsync(user);
        logger.LogInformation(new EventId(1004, "AdminUserPasswordReset"),
            "Admin user password reset ActorId {ActorId} TargetUserId {TargetUserId}", currentUser.UserId, user.Id);
        return NoContent();
    }

    private Task LockAdministrators() => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(6492247145231)");
    private static UserView View(ApplicationUser user, string role) =>
        new(user.Id, user.UserName!, user.Email, user.DisplayName, user.IsActive, role, user.CreatedAtUtc);
}

public sealed record CreateUserRequest(string UserName, string? Email, string DisplayName, string Password, string Role);
public sealed record SetRoleRequest(string Role);
public sealed record SetActiveRequest(bool IsActive);
public sealed record ResetPasswordRequest(string NewPassword);
