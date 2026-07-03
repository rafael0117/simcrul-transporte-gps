using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class HistorialController : Controller
{
    private readonly ApiClient _apiClient;

    public HistorialController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.CanViewHistory(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var history = await _apiClient.GetAsync<List<MaintenanceHistoryItemDto>>("maintenance/dashboard/history") ?? [];
        return View(history);
    }
}
