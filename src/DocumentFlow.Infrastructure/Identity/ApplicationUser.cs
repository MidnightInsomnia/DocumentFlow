using Microsoft.AspNetCore.Identity;

namespace DocumentFlow.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public static bool IsValid(string? role) => role is Admin or Operator;
}
