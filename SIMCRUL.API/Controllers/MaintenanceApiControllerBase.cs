using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SIMCRUL.API.Controllers;

public abstract class MaintenanceApiControllerBase : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    protected string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    protected bool UserHasAnyRole(params string[] roles)
    {
        return roles.Any(User.IsInRole);
    }
}
