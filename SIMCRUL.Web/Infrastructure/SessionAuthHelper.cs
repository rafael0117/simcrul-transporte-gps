using SIMCRUL.Common.Constants;
using Microsoft.AspNetCore.Http;

namespace SIMCRUL.Web.Infrastructure;

public static class SessionAuthHelper
{
    public static bool IsAuthenticated(ISession session)
    {
        return !string.IsNullOrWhiteSpace(session.GetString("Token"));
    }

    public static bool IsPassengerAuthenticated(ISession session)
    {
        return IsAuthenticated(session) &&
               string.Equals(session.GetString("Rol"), Roles.Pasajero, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDriverAuthenticated(ISession session)
    {
        return IsAuthenticated(session) &&
               string.Equals(session.GetString("Rol"), Roles.Conductor, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBackofficeAuthenticated(ISession session)
    {
        if (!IsAuthenticated(session))
        {
            return false;
        }

        var role = session.GetString("Rol");
        return string.Equals(role, Roles.Administrador, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, Roles.Supervisor, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, Roles.Operador, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOperatorAuthenticated(ISession session)
    {
        return IsBackofficeAuthenticated(session);
    }
}
