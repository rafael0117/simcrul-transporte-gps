using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class IncidenciasController : Controller
{
    private readonly ApiClient _apiClient;

    public IncidenciasController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Login", "Home");
        }

        var incidents = await _apiClient.GetAsync<List<IncidentDto>>("Incidencias") ?? [];
        return View(incidents);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Login", "Home");
        }

        var incident = await _apiClient.GetAsync<IncidentDto>($"Incidencias/{id}");
        if (incident == null)
        {
            TempData["ErrorMessage"] = "Incidencia no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(incident);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!SessionAuthHelper.IsDriverAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Vehicles = await _apiClient.GetAsync<List<VehicleOptionDto>>("maintenance/catalog/vehicles") ?? [];
        return View(new IncidentDto { FechaReporte = DateTime.Now });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(IncidentDto model)
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
            await _apiClient.PostAsync<IncidentDto, object>("Incidencias", model);
            TempData["SuccessMessage"] = "Incidencia registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ActualizarEstado(int id, string estado)
    {
        if (!SessionAuthHelper.IsChiefAuthenticated(HttpContext.Session) &&
            !SessionAuthHelper.IsTechnicianAuthenticated(HttpContext.Session) &&
            !SessionAuthHelper.IsAdminAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        try
        {
            await _apiClient.PutAsync<IncidentStatusUpdateDto, object>($"Incidencias/{id}/estado", new IncidentStatusUpdateDto { Estado = estado });
            TempData["SuccessMessage"] = "Estado de incidencia actualizado.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
