using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Web.Infrastructure;

namespace SIMCRUL.Web.Controllers;

public class RutasController : Controller
{
    private bool IsAuthorized()
    {
        return SessionAuthHelper.IsOperatorAuthenticated(HttpContext.Session);
    }

    public IActionResult Index()
    {
        if (!IsAuthorized())
        {
            if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
            {
                return RedirectToAction("Index", "Passenger");
            }

            return RedirectToAction("Login", "Home");
        }

        return View();
    }
}
