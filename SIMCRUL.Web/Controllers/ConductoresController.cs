using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Drivers;
using SIMCRUL.Common.DTOs.Shared;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class ConductoresController : Controller
{
    private readonly ApiClient _apiClient;

    public ConductoresController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var drivers = await _apiClient.GetAsync<List<ConductorManagementDto>>("Conductores") ?? [];
        return View(drivers);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new ConductorManagementDto { FechaVencimientoLicencia = DateTime.Today.AddYears(1) });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(ConductorManagementDto model)
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        try
        {
            await _apiClient.PostAsync<ConductorManagementDto, object>("Conductores", model);
            TempData["SuccessMessage"] = "Conductor registrado correctamente.";
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
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var driver = await _apiClient.GetAsync<ConductorManagementDto>($"Conductores/{id}");
        if (driver == null)
        {
            TempData["ErrorMessage"] = "Conductor no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View(driver);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(ConductorManagementDto model)
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        try
        {
            await _apiClient.PutAsync<ConductorManagementDto, object>($"Conductores/{model.IdConductor}", model);
            TempData["SuccessMessage"] = "Conductor actualizado correctamente.";
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
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var deleted = await _apiClient.DeleteAsync($"Conductores/{id}");
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
            ? "Conductor desactivado correctamente."
            : "No se pudo desactivar el conductor.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Exportar()
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var bytes = await _apiClient.GetBytesAsync("Conductores/export");
        if (bytes == null)
        {
            TempData["ErrorMessage"] = "No se pudo exportar conductores.";
            return RedirectToAction(nameof(Index));
        }

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"conductores-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    public async Task<IActionResult> DescargarPlantilla()
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var bytes = await _apiClient.GetBytesAsync("Conductores/template");
        return File(bytes ?? [], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-conductores.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> Importar(IFormFile file)
    {
        if (!SessionAuthHelper.CanManageDrivers(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Selecciona un archivo Excel para importar.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _apiClient.PostFileAsync<BulkImportResultDto>("Conductores/import", file);
            TempData["SuccessMessage"] = $"Carga procesada: {result?.CreatedRows ?? 0} creados, {result?.SkippedRows ?? 0} omitidos.";
            if (result?.Errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join(" | ", result.Errors.Take(5));
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
