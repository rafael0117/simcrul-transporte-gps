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
public class OrdenesTrabajoController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdenesTrabajoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var query = _context.OrdenesTrabajo
            .Include(o => o.Vehiculo)
            .Include(o => o.GeneradoPorUsuario)
            .Include(o => o.TecnicoUsuario)
            .Include(o => o.IncidenciaMantenimiento)
            .Include(o => o.PlanMantenimientoPreventivo)
            .Include(o => o.MantenimientosEjecutados)
                .ThenInclude(m => m.RepuestosUtilizados)
            .AsQueryable();

        if (User.IsInRole(Roles.TecnicoMantenimiento))
        {
            var userId = GetCurrentUserId();
            query = query.Where(o => o.IdTecnicoUsuario == userId || (o.IdTecnicoUsuario == null && o.Estado == "GENERADA"));
        }

        var orders = await query
            .OrderByDescending(o => o.FechaProgramada)
            .ToListAsync(cancellationToken);

        return Ok(orders.Select(MapToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkOrderDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.JefeMantenimiento, Roles.Administrador))
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

        if (model.IdIncidenciaMantenimiento.HasValue)
        {
            var openOrder = await _context.OrdenesTrabajo.AnyAsync(
                o => o.IdIncidenciaMantenimiento == model.IdIncidenciaMantenimiento && o.Estado != "FINALIZADA",
                cancellationToken);

            if (openOrder)
            {
                return BadRequest(new { message = "La incidencia ya cuenta con una orden de trabajo activa." });
            }
        }

        var order = new OrdenTrabajo
        {
            IdVehiculo = model.IdVehiculo,
            IdGeneradoPorUsuario = GetCurrentUserId(),
            IdTecnicoUsuario = model.IdTecnicoUsuario,
            IdPlanMantenimientoPreventivo = model.IdPlanMantenimientoPreventivo,
            IdIncidenciaMantenimiento = model.IdIncidenciaMantenimiento,
            NumeroOrden = await GenerateOrderNumberAsync(cancellationToken),
            TipoOrden = model.TipoOrden.Trim().ToUpperInvariant(),
            Prioridad = model.Prioridad.Trim().ToUpperInvariant(),
            Estado = model.IdTecnicoUsuario.HasValue ? "ASIGNADA" : "GENERADA",
            FechaGeneracion = DateTime.UtcNow,
            FechaProgramada = model.FechaProgramada.ToUniversalTime(),
            FechaAsignacion = model.IdTecnicoUsuario.HasValue ? DateTime.UtcNow : null,
            TrabajoSolicitado = model.TrabajoSolicitado.Trim(),
            DiagnosticoInicial = NormalizeOptional(model.DiagnosticoInicial),
            Observaciones = NormalizeOptional(model.Observaciones)
        };

        _context.OrdenesTrabajo.Add(order);

        if (order.IdIncidenciaMantenimiento.HasValue)
        {
            var incident = await _context.IncidenciasMantenimiento.FirstOrDefaultAsync(
                i => i.IdIncidenciaMantenimiento == order.IdIncidenciaMantenimiento.Value,
                cancellationToken);

            if (incident != null)
            {
                incident.Estado = order.IdTecnicoUsuario.HasValue ? "EN_REPARACION" : "EN_DIAGNOSTICO";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Orden {order.NumeroOrden} creada correctamente." });
    }

    [HttpPut("{id:int}/asignar")]
    public async Task<IActionResult> Assign(int id, [FromBody] WorkOrderAssignmentDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.JefeMantenimiento))
        {
            return Forbid();
        }

        var order = await _context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.IdOrdenTrabajo == id, cancellationToken);
        if (order == null)
        {
            return NotFound(new { message = "Orden de trabajo no encontrada." });
        }

        var technician = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == model.IdTecnicoUsuario && u.Activo, cancellationToken);

        if (technician == null || technician.Rol.Nombre != Roles.TecnicoMantenimiento)
        {
            return BadRequest(new { message = "El tecnico seleccionado no es valido." });
        }

        order.IdTecnicoUsuario = model.IdTecnicoUsuario;
        order.FechaProgramada = model.FechaProgramada.ToUniversalTime();
        order.FechaAsignacion = DateTime.UtcNow;
        order.Estado = "ASIGNADA";
        order.Observaciones = MergeNotes(order.Observaciones, model.Observaciones);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Tecnico asignado correctamente." });
    }

    [HttpPost("{id:int}/ejecutar")]
    public async Task<IActionResult> Execute(int id, [FromBody] MaintenanceExecutionDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.TecnicoMantenimiento, Roles.Administrador))
        {
            return Forbid();
        }

        var order = await _context.OrdenesTrabajo
            .Include(o => o.Vehiculo)
            .Include(o => o.IncidenciaMantenimiento)
            .Include(o => o.PlanMantenimientoPreventivo)
            .Include(o => o.MantenimientosEjecutados)
            .FirstOrDefaultAsync(o => o.IdOrdenTrabajo == id, cancellationToken);

        if (order == null)
        {
            return NotFound(new { message = "Orden de trabajo no encontrada." });
        }

        var currentUserId = GetCurrentUserId();
        if (User.IsInRole(Roles.TecnicoMantenimiento) && order.IdTecnicoUsuario.HasValue && order.IdTecnicoUsuario != currentUserId)
        {
            return Forbid();
        }

        if (order.Estado == "FINALIZADA")
        {
            return BadRequest(new { message = "La orden ya fue finalizada." });
        }

        if (order.MantenimientosEjecutados.Any())
        {
            return BadRequest(new { message = "La orden ya registra una ejecucion tecnica." });
        }

        var maintenance = new MantenimientoEjecutado
        {
            IdOrdenTrabajo = order.IdOrdenTrabajo,
            IdTecnicoUsuario = currentUserId,
            FechaInicio = model.FechaInicio.ToUniversalTime(),
            FechaFin = model.FechaFin?.ToUniversalTime() ?? DateTime.UtcNow,
            TipoMantenimiento = model.TipoMantenimiento.Trim().ToUpperInvariant(),
            Diagnostico = model.Diagnostico.Trim(),
            AccionesRealizadas = model.AccionesRealizadas.Trim(),
            Recomendaciones = NormalizeOptional(model.Recomendaciones),
            EstadoResultado = model.EstadoResultado.Trim().ToUpperInvariant(),
            NuevoEstadoOperativoVehiculo = model.NuevoEstadoOperativoVehiculo.Trim().ToUpperInvariant()
        };

        _context.MantenimientosEjecutados.Add(maintenance);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var part in model.Repuestos.Where(p => !string.IsNullOrWhiteSpace(p.CodigoRepuesto) && !string.IsNullOrWhiteSpace(p.NombreRepuesto)))
        {
            _context.RepuestosUtilizados.Add(new RepuestoUtilizado
            {
                IdMantenimientoEjecutado = maintenance.IdMantenimientoEjecutado,
                CodigoRepuesto = part.CodigoRepuesto.Trim().ToUpperInvariant(),
                NombreRepuesto = part.NombreRepuesto.Trim(),
                Cantidad = part.Cantidad,
                CostoUnitario = part.CostoUnitario,
                Observaciones = NormalizeOptional(part.Observaciones)
            });
        }

        order.IdTecnicoUsuario ??= currentUserId;
        order.FechaInicio = maintenance.FechaInicio;
        order.FechaFin = maintenance.FechaFin;
        order.Estado = "FINALIZADA";
        order.Vehiculo.EstadoOperativo = maintenance.NuevoEstadoOperativoVehiculo;
        order.Vehiculo.FechaUltimoMantenimiento = maintenance.FechaFin;
        order.Vehiculo.ObservacionesMantenimiento = $"Ultimo mantenimiento {order.TipoOrden.ToLowerInvariant()} finalizado con orden {order.NumeroOrden}.";

        if (order.IncidenciaMantenimiento != null)
        {
            order.IncidenciaMantenimiento.Estado = "CERRADA";
            order.IncidenciaMantenimiento.FechaCierre = maintenance.FechaFin;
        }

        if (order.PlanMantenimientoPreventivo != null)
        {
            order.PlanMantenimientoPreventivo.Estado = "EJECUTADO";
            order.PlanMantenimientoPreventivo.UltimaEjecucion = maintenance.FechaFin;
            order.PlanMantenimientoPreventivo.ProximaFechaProgramada = maintenance.FechaFin!.Value.AddDays(order.PlanMantenimientoPreventivo.FrecuenciaDias);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Mantenimiento registrado correctamente." });
    }

    private static WorkOrderDto MapToDto(OrdenTrabajo order)
    {
        var latestMaintenance = order.MantenimientosEjecutados
            .OrderByDescending(m => m.FechaFin ?? m.FechaInicio)
            .FirstOrDefault();

        return new WorkOrderDto
        {
            IdOrdenTrabajo = order.IdOrdenTrabajo,
            IdVehiculo = order.IdVehiculo,
            IdGeneradoPorUsuario = order.IdGeneradoPorUsuario,
            IdTecnicoUsuario = order.IdTecnicoUsuario,
            IdPlanMantenimientoPreventivo = order.IdPlanMantenimientoPreventivo,
            IdIncidenciaMantenimiento = order.IdIncidenciaMantenimiento,
            NumeroOrden = order.NumeroOrden,
            TipoOrden = order.TipoOrden,
            Prioridad = order.Prioridad,
            Estado = order.Estado,
            FechaGeneracion = order.FechaGeneracion,
            FechaProgramada = order.FechaProgramada,
            FechaAsignacion = order.FechaAsignacion,
            FechaInicio = order.FechaInicio,
            FechaFin = order.FechaFin,
            TrabajoSolicitado = order.TrabajoSolicitado,
            DiagnosticoInicial = latestMaintenance?.Diagnostico ?? order.DiagnosticoInicial,
            Observaciones = order.Observaciones,
            NombreVehiculo = order.Vehiculo.CodigoInterno + " - " + order.Vehiculo.Placa,
            NombreGeneradoPor = (order.GeneradoPorUsuario.Nombres + " " + order.GeneradoPorUsuario.Apellidos).Trim(),
            NombreTecnico = order.TecnicoUsuario == null ? null : (order.TecnicoUsuario.Nombres + " " + order.TecnicoUsuario.Apellidos).Trim(),
            TituloIncidencia = order.IncidenciaMantenimiento?.Titulo,
            ActividadesPlan = order.PlanMantenimientoPreventivo?.Actividades,
            Repuestos = latestMaintenance?.RepuestosUtilizados
                .Select(p => new SparePartDto
                {
                    CodigoRepuesto = p.CodigoRepuesto,
                    NombreRepuesto = p.NombreRepuesto,
                    Cantidad = p.Cantidad,
                    CostoUnitario = p.CostoUnitario,
                    Observaciones = p.Observaciones
                })
                .ToList() ?? []
        };
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

    private static string? MergeNotes(string? existing, string? addition)
    {
        var trimmed = NormalizeOptional(addition);
        if (trimmed == null)
        {
            return existing;
        }

        if (string.IsNullOrWhiteSpace(existing))
        {
            return trimmed;
        }

        return existing.Trim() + " | " + trimmed;
    }
}
