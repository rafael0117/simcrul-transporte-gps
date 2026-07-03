using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class OrdenesTrabajoController : Controller
{
    private readonly ApiClient _apiClient;

    public OrdenesTrabajoController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Login", "Home");
        }

        var orders = await _apiClient.GetAsync<List<WorkOrderDto>>("OrdenesTrabajo") ?? [];
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!SessionAuthHelper.IsChiefAuthenticated(HttpContext.Session) && !SessionAuthHelper.IsAdminAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Vehicles = await _apiClient.GetAsync<List<VehicleOptionDto>>("maintenance/catalog/vehicles") ?? [];
        ViewBag.Technicians = await _apiClient.GetAsync<List<UserOptionDto>>("maintenance/catalog/technicians") ?? [];
        ViewBag.Incidents = await _apiClient.GetAsync<List<IncidentDto>>("Incidencias") ?? [];
        return View(new WorkOrderDto { FechaProgramada = DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(WorkOrderDto model)
    {
        if (!SessionAuthHelper.IsChiefAuthenticated(HttpContext.Session) && !SessionAuthHelper.IsAdminAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Vehicles = await _apiClient.GetAsync<List<VehicleOptionDto>>("maintenance/catalog/vehicles") ?? [];
        ViewBag.Technicians = await _apiClient.GetAsync<List<UserOptionDto>>("maintenance/catalog/technicians") ?? [];
        ViewBag.Incidents = await _apiClient.GetAsync<List<IncidentDto>>("Incidencias") ?? [];
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _apiClient.PostAsync<WorkOrderDto, object>("OrdenesTrabajo", model);
            TempData["SuccessMessage"] = "Orden de trabajo creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Asignar(int id)
    {
        if (!SessionAuthHelper.IsChiefAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.OrderId = id;
        ViewBag.Technicians = await _apiClient.GetAsync<List<UserOptionDto>>("maintenance/catalog/technicians") ?? [];
        return View(new WorkOrderAssignmentDto { FechaProgramada = DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Asignar(int id, WorkOrderAssignmentDto model)
    {
        if (!SessionAuthHelper.IsChiefAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.OrderId = id;
        ViewBag.Technicians = await _apiClient.GetAsync<List<UserOptionDto>>("maintenance/catalog/technicians") ?? [];
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _apiClient.PutAsync<WorkOrderAssignmentDto, object>($"OrdenesTrabajo/{id}/asignar", model);
            TempData["SuccessMessage"] = "Tecnico asignado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Ejecutar(int id)
    {
        if (!SessionAuthHelper.CanExecuteOrders(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new MaintenanceExecutionDto
        {
            IdOrdenTrabajo = id,
            FechaInicio = DateTime.Now,
            FechaFin = DateTime.Now.AddHours(1),
            Repuestos = [new SparePartDto()]
        });
    }

    [HttpPost]
    public async Task<IActionResult> Ejecutar(MaintenanceExecutionDto model)
    {
        if (!SessionAuthHelper.CanExecuteOrders(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        model.Repuestos = model.Repuestos
            .Where(r => !string.IsNullOrWhiteSpace(r.CodigoRepuesto) || !string.IsNullOrWhiteSpace(r.NombreRepuesto))
            .ToList();

        if (!ModelState.IsValid)
        {
            if (model.Repuestos.Count == 0)
            {
                model.Repuestos.Add(new SparePartDto());
            }
            return View(model);
        }

        try
        {
            await _apiClient.PostAsync<MaintenanceExecutionDto, object>($"OrdenesTrabajo/{model.IdOrdenTrabajo}/ejecutar", model);
            TempData["SuccessMessage"] = "Mantenimiento ejecutado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            if (model.Repuestos.Count == 0)
            {
                model.Repuestos.Add(new SparePartDto());
            }
            return View(model);
        }
    }
}
