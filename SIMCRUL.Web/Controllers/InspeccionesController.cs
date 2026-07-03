using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class InspeccionesController : Controller
{
    private readonly ApiClient _apiClient;

    public InspeccionesController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var inspections = await _apiClient.GetAsync<List<InspectionDto>>("Inspecciones") ?? [];
        return View(inspections);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Vehicles = await _apiClient.GetAsync<List<VehicleOptionDto>>("maintenance/catalog/vehicles") ?? [];
        return View(new InspectionDto { FechaInspeccion = DateTime.Now });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(InspectionDto model)
    {
        if (!SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Vehicles = await _apiClient.GetAsync<List<VehicleOptionDto>>("maintenance/catalog/vehicles") ?? [];
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _apiClient.PostAsync<InspectionDto, object>("Inspecciones", model);
            TempData["SuccessMessage"] = "Inspeccion registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
