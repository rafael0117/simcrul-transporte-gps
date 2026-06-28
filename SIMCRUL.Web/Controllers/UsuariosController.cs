using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Web.Infrastructure;

namespace SIMCRUL.Web.Controllers;

public class UsuariosController : Controller
{
    private bool IsAuthorized()
    {
        return SessionAuthHelper.IsBackofficeAuthenticated(HttpContext.Session);
    }

    public IActionResult Index()
    {
        if (!IsAuthorized())
        {
            if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
            {
                return RedirectToAction("Index", "Passenger");
            }

            if (SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
            {
                return RedirectToAction("Tracking", "Conductores");
            }

            return RedirectToAction("Login", "Home");
        }

        return View();
    }
}
