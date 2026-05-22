using Microsoft.AspNetCore.Mvc;

namespace SIMCRUL.Web.Controllers;

public class PassengerController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
