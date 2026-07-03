using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class PlanesMantenimientoController : Controller
{
    private readonly ApiClient _apiClient;

    public PlanesMantenimientoController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.CanManagePlans(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var plans = await _apiClient.GetAsync<List<PreventivePlanDto>>("PlanesMantenimiento") ?? [];
        return View(plans);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!SessionAuthHelper.CanManagePlans(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Vehicles = await _apiClient.GetAsync<List<VehicleOptionDto>>("maintenance/catalog/vehicles") ?? [];
        return View(new PreventivePlanDto { ProximaFechaProgramada = DateTime.Today.AddDays(1) });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(PreventivePlanDto model)
    {
        if (!SessionAuthHelper.CanManagePlans(HttpContext.Session))
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
            await _apiClient.PostAsync<PreventivePlanDto, object>("PlanesMantenimiento", model);
            TempData["SuccessMessage"] = "Plan preventivo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
