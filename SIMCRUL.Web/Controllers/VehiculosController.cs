using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Vehicles;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class VehiculosController : Controller
{
    private readonly ApiClient _apiClient;

    public VehiculosController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.CanManageVehicles(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var vehicles = await _apiClient.GetAsync<List<VehicleManagementDto>>("Vehiculos") ?? [];
        return View(vehicles);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        if (!SessionAuthHelper.CanManageVehicles(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new VehicleManagementDto());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(VehicleManagementDto model)
    {
        if (!SessionAuthHelper.CanManageVehicles(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _apiClient.PostAsync<VehicleManagementDto, object>("Vehiculos", model);
            TempData["SuccessMessage"] = "Vehiculo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        if (!SessionAuthHelper.CanManageVehicles(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var vehicle = await _apiClient.GetAsync<VehicleManagementDto>($"Vehiculos/{id}");
        if (vehicle == null)
        {
            TempData["ErrorMessage"] = "Vehiculo no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View(vehicle);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(VehicleManagementDto model)
    {
        if (!SessionAuthHelper.CanManageVehicles(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _apiClient.PutAsync<VehicleManagementDto, object>($"Vehiculos/{model.IdVehiculo}", model);
            TempData["SuccessMessage"] = "Vehiculo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (!SessionAuthHelper.CanManageVehicles(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var deleted = await _apiClient.DeleteAsync($"Vehiculos/{id}");
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
            ? "Vehiculo desactivado correctamente."
            : "No se pudo desactivar el vehiculo.";
        return RedirectToAction(nameof(Index));
    }
}
