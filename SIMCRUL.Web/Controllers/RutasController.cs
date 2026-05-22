using Microsoft.AspNetCore.Mvc;

namespace SIMCRUL.Web.Controllers;

public class RutasController : Controller
{
    private bool IsAuthorized()
    {
        return HttpContext.Session.GetString("Token") != null;
    }

    public IActionResult Index()
    {
        if (!IsAuthorized()) return RedirectToAction("Login", "Home");
        return View();
    }
}
