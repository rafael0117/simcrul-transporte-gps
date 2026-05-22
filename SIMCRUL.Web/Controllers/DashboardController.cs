using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class DashboardController : Controller
{
    private readonly ApiClient _apiClient;

    public DashboardController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    private bool IsAuthorized()
    {
        return HttpContext.Session.GetString("Token") != null;
    }

    public IActionResult Index()
    {
        if (!IsAuthorized()) return RedirectToAction("Login", "Home");
        return View();
    }

    public IActionResult Simulator()
    {
        if (!IsAuthorized()) return RedirectToAction("Login", "Home");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(DateTime? dateFrom, DateTime? dateTo)
    {
        if (!IsAuthorized()) return RedirectToAction("Login", "Home");

        var fromStr = dateFrom?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
        var toStr = dateTo?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

        var bytes = await _apiClient.GetBytesAsync($"Report/alerts-pdf?dateFrom={fromStr}&dateTo={toStr}");
        if (bytes == null) return NotFound("No se pudo generar el reporte PDF.");

        return File(bytes, "application/pdf", $"ReporteAlertas_{fromStr}_{toStr}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExcel(DateTime? dateFrom, DateTime? dateTo)
    {
        if (!IsAuthorized()) return RedirectToAction("Login", "Home");

        var fromStr = dateFrom?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
        var toStr = dateTo?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

        var bytes = await _apiClient.GetBytesAsync($"Report/trips-excel?dateFrom={fromStr}&dateTo={toStr}");
        if (bytes == null) return NotFound("No se pudo generar el reporte Excel.");

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ReporteViajes_{fromStr}_{toStr}.xlsx");
    }
}
