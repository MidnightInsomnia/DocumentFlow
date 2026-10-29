using DocumentFlow.Infrastructure.Identity;

namespace DocumentFlow.Api.Auth;

public static class AdminSafety
{
    public const string LastAdminError = "В системе должен оставаться хотя бы один активный администратор";
    public static bool WouldRemoveLastActiveAdmin(Guid targetId, IEnumerable<ApplicationUser> activeAdmins) =>
        !activeAdmins.Any(user => user.Id != targetId && user.IsActive);
}
