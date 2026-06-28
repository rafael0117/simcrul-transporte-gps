using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Web.Infrastructure;

namespace SIMCRUL.Web.Controllers;

public class ConductoresController : Controller
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

            if (SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
            {
                return RedirectToAction(nameof(Tracking));
            }

            return RedirectToAction("Login", "Home");
        }

        return View();
    }

    [HttpGet]
    public IActionResult Tracking()
    {
        if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Passenger");
        }

        if (!SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
        {
            if (SessionAuthHelper.IsBackofficeAuthenticated(HttpContext.Session))
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Login", "Home");
        }

        ViewData["HideChrome"] = true;
        return View();
    }
}
