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
public class IncidenciasController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public IncidenciasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var query = _context.IncidenciasMantenimiento
            .Include(i => i.Vehiculo)
            .Include(i => i.ReportadoPorUsuario)
            .AsQueryable();

        if (User.IsInRole(Roles.Conductor))
        {
            var userId = GetCurrentUserId();
            query = query.Where(i => i.IdReportadoPorUsuario == userId);
        }

        var incidents = await query
            .OrderByDescending(i => i.FechaReporte)
            .Select(i => new IncidentDto
            {
                IdIncidenciaMantenimiento = i.IdIncidenciaMantenimiento,
                IdVehiculo = i.IdVehiculo,
                IdReportadoPorUsuario = i.IdReportadoPorUsuario,
                IdInspeccionDiaria = i.IdInspeccionDiaria,
                FechaReporte = i.FechaReporte,
                TipoIncidencia = i.TipoIncidencia,
                Severidad = i.Severidad,
                Titulo = i.Titulo,
                Descripcion = i.Descripcion,
                Estado = i.Estado,
                RequiereParada = i.RequiereParada,
                UbicacionReferencia = i.UbicacionReferencia,
                FechaCierre = i.FechaCierre,
                NombreVehiculo = i.Vehiculo.CodigoInterno + " - " + i.Vehiculo.Placa,
                NombreReportadoPor = (i.ReportadoPorUsuario.Nombres + " " + i.ReportadoPorUsuario.Apellidos).Trim()
            })
            .ToListAsync(cancellationToken);

        return Ok(incidents);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IncidentDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Conductor))
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

        var incident = new IncidenciaMantenimiento
        {
            IdVehiculo = model.IdVehiculo,
            IdReportadoPorUsuario = GetCurrentUserId(),
            IdInspeccionDiaria = model.IdInspeccionDiaria,
            FechaReporte = model.FechaReporte == default ? DateTime.UtcNow : model.FechaReporte.ToUniversalTime(),
            TipoIncidencia = model.TipoIncidencia.Trim().ToUpperInvariant(),
            Severidad = model.Severidad.Trim().ToUpperInvariant(),
            Titulo = model.Titulo.Trim(),
            Descripcion = model.Descripcion.Trim(),
            Estado = "REPORTADA",
            RequiereParada = model.RequiereParada,
            UbicacionReferencia = NormalizeOptional(model.UbicacionReferencia)
        };

        if (incident.RequiereParada || incident.Severidad == "CRITICA")
        {
            vehicle.EstadoOperativo = "FUERA_DE_SERVICIO";
        }
        else if (vehicle.EstadoOperativo == "OPERATIVO")
        {
            vehicle.EstadoOperativo = "EN_MANTENIMIENTO";
        }

        vehicle.ObservacionesMantenimiento = $"Incidencia abierta: {incident.Titulo}";

        _context.IncidenciasMantenimiento.Add(incident);
        await _context.SaveChangesAsync(cancellationToken);

        model.IdIncidenciaMantenimiento = incident.IdIncidenciaMantenimiento;
        model.Estado = incident.Estado;
        model.IdReportadoPorUsuario = incident.IdReportadoPorUsuario;
        model.NombreVehiculo = vehicle.CodigoInterno + " - " + vehicle.Placa;
        return CreatedAtAction(nameof(GetAll), new { id = incident.IdIncidenciaMantenimiento }, model);
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] IncidentDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.JefeMantenimiento, Roles.TecnicoMantenimiento, Roles.Administrador))
        {
            return Forbid();
        }

        var incident = await _context.IncidenciasMantenimiento
            .Include(i => i.Vehiculo)
            .FirstOrDefaultAsync(i => i.IdIncidenciaMantenimiento == id, cancellationToken);

        if (incident == null)
        {
            return NotFound(new { message = "Incidencia no encontrada." });
        }

        incident.Estado = model.Estado.Trim().ToUpperInvariant();
        if (incident.Estado is "RESUELTA" or "CERRADA")
        {
            incident.FechaCierre = DateTime.UtcNow;
        }

        if (incident.Estado == "CERRADA" && incident.Vehiculo.EstadoOperativo == "EN_MANTENIMIENTO")
        {
            incident.Vehiculo.EstadoOperativo = "OPERATIVO";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Estado de incidencia actualizado correctamente." });
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
