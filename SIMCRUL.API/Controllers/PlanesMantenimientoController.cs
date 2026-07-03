using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanesMantenimientoController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public PlanesMantenimientoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.JefeMantenimiento, Roles.Administrador))
        {
            return Forbid();
        }

        var plans = await _context.PlanesMantenimientoPreventivo
            .Include(p => p.Vehiculo)
            .Include(p => p.CreadoPorUsuario)
            .OrderBy(p => p.ProximaFechaProgramada)
            .Select(p => new PreventivePlanDto
            {
                IdPlanMantenimientoPreventivo = p.IdPlanMantenimientoPreventivo,
                IdVehiculo = p.IdVehiculo,
                IdCreadoPorUsuario = p.IdCreadoPorUsuario,
                NombreVehiculo = p.Vehiculo.CodigoInterno + " - " + p.Vehiculo.Placa,
                NombreCreadoPor = (p.CreadoPorUsuario.Nombres + " " + p.CreadoPorUsuario.Apellidos).Trim(),
                FechaRegistro = p.FechaRegistro,
                ProximaFechaProgramada = p.ProximaFechaProgramada,
                FrecuenciaDias = p.FrecuenciaDias,
                FrecuenciaKilometros = p.FrecuenciaKilometros,
                Actividades = p.Actividades,
                Estado = p.Estado,
                Prioridad = p.Prioridad,
                Observaciones = p.Observaciones,
                UltimaEjecucion = p.UltimaEjecucion
            })
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PreventivePlanDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.JefeMantenimiento))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == model.IdVehiculo && v.Estado, cancellationToken);
        if (vehicle == null)
        {
            return BadRequest(new { message = "El vehiculo seleccionado no existe o esta inactivo." });
        }

        var plan = new PlanMantenimientoPreventivo
        {
            IdVehiculo = model.IdVehiculo,
            IdCreadoPorUsuario = GetCurrentUserId(),
            FechaRegistro = DateTime.UtcNow,
            ProximaFechaProgramada = model.ProximaFechaProgramada.ToUniversalTime(),
            FrecuenciaDias = model.FrecuenciaDias,
            FrecuenciaKilometros = model.FrecuenciaKilometros,
            Actividades = model.Actividades.Trim(),
            Estado = model.Estado.Trim().ToUpperInvariant(),
            Prioridad = model.Prioridad.Trim().ToUpperInvariant(),
            Observaciones = NormalizeOptional(model.Observaciones),
            UltimaEjecucion = model.UltimaEjecucion
        };

        _context.PlanesMantenimientoPreventivo.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        var order = await EnsurePreventiveOrderAsync(plan, cancellationToken);
        vehicle.ObservacionesMantenimiento = $"Plan preventivo vigente. Orden generada: {order.NumeroOrden}.";
        if (vehicle.EstadoOperativo == "OPERATIVO")
        {
            vehicle.EstadoOperativo = "EN_MANTENIMIENTO";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            message = $"Plan preventivo registrado y orden {order.NumeroOrden} generada correctamente.",
            idPlan = plan.IdPlanMantenimientoPreventivo
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PreventivePlanDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.JefeMantenimiento))
        {
            return Forbid();
        }

        if (id != model.IdPlanMantenimientoPreventivo)
        {
            return BadRequest(new { message = "El identificador del plan no coincide." });
        }

        var plan = await _context.PlanesMantenimientoPreventivo.FirstOrDefaultAsync(p => p.IdPlanMantenimientoPreventivo == id, cancellationToken);
        if (plan == null)
        {
            return NotFound(new { message = "Plan no encontrado." });
        }

        plan.ProximaFechaProgramada = model.ProximaFechaProgramada.ToUniversalTime();
        plan.FrecuenciaDias = model.FrecuenciaDias;
        plan.FrecuenciaKilometros = model.FrecuenciaKilometros;
        plan.Actividades = model.Actividades.Trim();
        plan.Estado = model.Estado.Trim().ToUpperInvariant();
        plan.Prioridad = model.Prioridad.Trim().ToUpperInvariant();
        plan.Observaciones = NormalizeOptional(model.Observaciones);
        plan.UltimaEjecucion = model.UltimaEjecucion;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Plan preventivo actualizado correctamente." });
    }

    private async Task<OrdenTrabajo> EnsurePreventiveOrderAsync(PlanMantenimientoPreventivo plan, CancellationToken cancellationToken)
    {
        var existing = await _context.OrdenesTrabajo.FirstOrDefaultAsync(
            o => o.IdPlanMantenimientoPreventivo == plan.IdPlanMantenimientoPreventivo && o.Estado != "FINALIZADA",
            cancellationToken);

        if (existing != null)
        {
            existing.FechaProgramada = plan.ProximaFechaProgramada;
            existing.TrabajoSolicitado = plan.Actividades;
            existing.Prioridad = plan.Prioridad;
            return existing;
        }

        var order = new OrdenTrabajo
        {
            IdVehiculo = plan.IdVehiculo,
            IdGeneradoPorUsuario = plan.IdCreadoPorUsuario,
            IdPlanMantenimientoPreventivo = plan.IdPlanMantenimientoPreventivo,
            NumeroOrden = await GenerateOrderNumberAsync(cancellationToken),
            TipoOrden = "PREVENTIVO",
            Prioridad = plan.Prioridad,
            Estado = "GENERADA",
            FechaGeneracion = DateTime.UtcNow,
            FechaProgramada = plan.ProximaFechaProgramada,
            TrabajoSolicitado = plan.Actividades,
            Observaciones = "Orden generada automaticamente desde plan preventivo."
        };

        _context.OrdenesTrabajo.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"OT-{DateTime.UtcNow:yyyyMM}-";
        var count = await _context.OrdenesTrabajo.CountAsync(o => o.NumeroOrden.StartsWith(prefix), cancellationToken);
        return prefix + (count + 1).ToString("0000");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
