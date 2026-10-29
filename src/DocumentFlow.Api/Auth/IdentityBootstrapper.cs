using DocumentFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DocumentFlow.Api.Auth;

public sealed class IdentityBootstrapper(
    RoleManager<IdentityRole<Guid>> roles,
    UserManager<ApplicationUser> users,
    ILogger<IdentityBootstrapper> logger,
    IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        foreach (var role in new[] { ApplicationRoles.Admin, ApplicationRoles.Operator })
        {
            if (await roles.RoleExistsAsync(role)) continue;
            var result = await roles.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded) throw new InvalidOperationException($"Could not create the {role} role: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }

        var admins = await users.GetUsersInRoleAsync(ApplicationRoles.Admin);
        if (admins.Any(u => u.IsActive)) return;
        if (admins.Count > 0)
            throw new InvalidOperationException("No active administrator exists. Reactivate an existing administrator; bootstrap will not overwrite it.");

        var name = configuration["Auth:BootstrapAdmin:UserName"];
        var displayName = configuration["Auth:BootstrapAdmin:DisplayName"];
        var email = configuration["Auth:BootstrapAdmin:Email"];
        var password = configuration["Auth:BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(displayName) ||
            displayName.Length > 200 || string.IsNullOrWhiteSpace(password))
        {
            logger.LogCritical("No administrator exists; configure Auth:BootstrapAdmin user name, display name, and password.");
            throw new InvalidOperationException("No active administrator exists and bootstrap credentials are not configured.");
        }
        var existing = await users.FindByNameAsync(name);
        if (existing is not null)
        {
            logger.LogCritical("Bootstrap administrator already exists but is not an active Admin; refusing to overwrite credentials.");
            throw new InvalidOperationException("Bootstrap administrator conflicts with an existing user.");
        }
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = name, Email = email,
            DisplayName = displayName, IsActive = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogCritical("Bootstrap administrator creation failed: {Codes}", string.Join(",", created.Errors.Select(e => e.Code)));
            throw new InvalidOperationException("Bootstrap administrator creation failed.");
        }
        var assigned = await users.AddToRoleAsync(user, ApplicationRoles.Admin);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            logger.LogCritical("Bootstrap administrator role assignment failed: {Codes}", string.Join(",", assigned.Errors.Select(e => e.Code)));
            throw new InvalidOperationException("Bootstrap administrator role assignment failed.");
        }
        logger.LogInformation("Initial administrator created.");
    }
}
