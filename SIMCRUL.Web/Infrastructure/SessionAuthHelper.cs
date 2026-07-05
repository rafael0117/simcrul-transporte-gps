using Microsoft.AspNetCore.Http;
using SIMCRUL.Common.Constants;

namespace SIMCRUL.Web.Infrastructure;

public static class SessionAuthHelper
{
    public static bool IsAuthenticated(ISession session)
    {
        return !string.IsNullOrWhiteSpace(session.GetString("Token"));
    }

    public static string GetRole(ISession session)
    {
        return session.GetString("Rol") ?? string.Empty;
    }

    public static bool IsAdminAuthenticated(ISession session)
    {
        return IsAuthenticated(session) &&
               string.Equals(GetRole(session), Roles.Administrador, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsChiefAuthenticated(ISession session)
    {
        return IsAuthenticated(session) &&
               string.Equals(GetRole(session), Roles.JefeMantenimiento, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTechnicianAuthenticated(ISession session)
    {
        return IsAuthenticated(session) &&
               string.Equals(GetRole(session), Roles.TecnicoMantenimiento, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDriverAuthenticated(ISession session)
    {
        return IsAuthenticated(session) &&
               string.Equals(GetRole(session), Roles.Conductor, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBackofficeAuthenticated(ISession session)
    {
        return IsAdminAuthenticated(session) ||
               IsChiefAuthenticated(session) ||
               IsTechnicianAuthenticated(session);
    }

    public static bool CanManageVehicles(ISession session) => IsAdminAuthenticated(session);
    public static bool CanManageDrivers(ISession session) => IsAdminAuthenticated(session);
    public static bool CanManagePlans(ISession session) => IsChiefAuthenticated(session);
    public static bool CanViewHistory(ISession session) => IsAdminAuthenticated(session) || IsChiefAuthenticated(session);
    public static bool CanViewStats(ISession session) => IsAdminAuthenticated(session) || IsChiefAuthenticated(session);
    public static bool CanExecuteOrders(ISession session) => IsTechnicianAuthenticated(session) || IsAdminAuthenticated(session);
}
