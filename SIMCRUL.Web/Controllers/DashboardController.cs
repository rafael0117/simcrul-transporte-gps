using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class DashboardController : Controller
{
    private readonly ApiClient _apiClient;

    public DashboardController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Login", "Home");
        }

        var summary = await _apiClient.GetAsync<MaintenanceDashboardDto>("maintenance/dashboard/summary")
                      ?? new MaintenanceDashboardDto();

        ViewBag.UserRole = HttpContext.Session.GetString("Rol");
        ViewBag.DisplayName = $"{HttpContext.Session.GetString("Nombres")} {HttpContext.Session.GetString("Apellidos")}".Trim();
        return View(summary);
    }
}
