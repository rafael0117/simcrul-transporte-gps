using Microsoft.AspNetCore.Mvc;

namespace SIMCRUL.Web.Controllers;

public class UbicacionController : Controller
{
    private readonly IConfiguration _configuration;

    public UbicacionController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Compartir()
    {
        ViewBag.ApiBaseUrl = _configuration["ApiSettings:BrowserBaseUrl"] ?? "/api/";
        ViewBag.Imei = "MOBILE-DEMO-001";
        ViewBag.RouteName = "Demo profesor - Quisquis a Calcuchimac";
        return View();
    }
}
