using System.Security.Claims;

namespace BizCore.Services;

public interface IUserPermissionService
{
    bool HasMenuAccess(ClaimsPrincipal? user, string permissionCode);
    bool HasPermission(ClaimsPrincipal? user, string permissionCode);
}
