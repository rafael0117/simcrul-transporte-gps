using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class EstadisticasController : Controller
{
    private readonly ApiClient _apiClient;

    public EstadisticasController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.CanViewStats(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var summary = await _apiClient.GetAsync<MaintenanceDashboardDto>("maintenance/dashboard/summary")
                      ?? new MaintenanceDashboardDto();
        return View(summary);
    }

    public async Task<IActionResult> Exportar(DateTime? fechaDesde, DateTime? fechaHasta)
    {
        if (!SessionAuthHelper.CanViewStats(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var query = $"maintenance/dashboard/export?fechaDesde={fechaDesde:yyyy-MM-dd}&fechaHasta={fechaHasta:yyyy-MM-dd}";
        var bytes = await _apiClient.GetBytesAsync(query);
        if (bytes == null)
        {
            TempData["ErrorMessage"] = "No se pudo exportar el reporte.";
            return RedirectToAction(nameof(Index));
        }

        return File(bytes, "text/csv", $"reporte-mantenimiento-{DateTime.Now:yyyyMMddHHmmss}.csv");
    }
}
