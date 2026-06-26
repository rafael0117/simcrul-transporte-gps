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
               string.Equals(session.GetString("Rol"), "Pasajero", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOperatorAuthenticated(ISession session)
    {
        return IsAuthenticated(session) && !IsPassengerAuthenticated(session);
    }
}
