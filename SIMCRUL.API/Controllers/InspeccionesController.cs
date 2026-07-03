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
public class InspeccionesController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public InspeccionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var query = _context.InspeccionesDiarias
            .Include(i => i.Vehiculo)
            .Include(i => i.ConductorUsuario)
            .AsQueryable();

        if (User.IsInRole(Roles.Conductor))
        {
            var userId = GetCurrentUserId();
            query = query.Where(i => i.IdConductorUsuario == userId);
        }

        var inspections = await query
            .OrderByDescending(i => i.FechaInspeccion)
            .Select(i => new InspectionDto
            {
                IdInspeccionDiaria = i.IdInspeccionDiaria,
                IdVehiculo = i.IdVehiculo,
                IdConductorUsuario = i.IdConductorUsuario,
                NombreVehiculo = i.Vehiculo.CodigoInterno + " - " + i.Vehiculo.Placa,
                NombreConductor = (i.ConductorUsuario.Nombres + " " + i.ConductorUsuario.Apellidos).Trim(),
                FechaInspeccion = i.FechaInspeccion,
                Kilometraje = i.Kilometraje,
                NivelCombustible = i.NivelCombustible,
                LimpiezaCabina = i.LimpiezaCabina,
                LimpiezaExterior = i.LimpiezaExterior,
                LucesOperativas = i.LucesOperativas,
                FrenosOperativos = i.FrenosOperativos,
                NeumaticosOperativos = i.NeumaticosOperativos,
                BocinaOperativa = i.BocinaOperativa,
                EspejosOperativos = i.EspejosOperativos,
                BotiquinDisponible = i.BotiquinDisponible,
                ExtintorDisponible = i.ExtintorDisponible,
                DocumentosCompletos = i.DocumentosCompletos,
                Resultado = i.Resultado,
                Observaciones = i.Observaciones
            })
            .ToListAsync(cancellationToken);

        return Ok(inspections);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InspectionDto model, CancellationToken cancellationToken = default)
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

        model.Resultado = ResolveInspectionResult(model);

        var inspection = new InspeccionDiaria
        {
            IdVehiculo = model.IdVehiculo,
            IdConductorUsuario = GetCurrentUserId(),
            FechaInspeccion = model.FechaInspeccion == default ? DateTime.UtcNow : model.FechaInspeccion.ToUniversalTime(),
            Kilometraje = model.Kilometraje,
            NivelCombustible = model.NivelCombustible.Trim().ToUpperInvariant(),
            LimpiezaCabina = model.LimpiezaCabina,
            LimpiezaExterior = model.LimpiezaExterior,
            LucesOperativas = model.LucesOperativas,
            FrenosOperativos = model.FrenosOperativos,
            NeumaticosOperativos = model.NeumaticosOperativos,
            BocinaOperativa = model.BocinaOperativa,
            EspejosOperativos = model.EspejosOperativos,
            BotiquinDisponible = model.BotiquinDisponible,
            ExtintorDisponible = model.ExtintorDisponible,
            DocumentosCompletos = model.DocumentosCompletos,
            Resultado = model.Resultado,
            Observaciones = NormalizeOptional(model.Observaciones)
        };

        vehicle.KilometrajeActual = model.Kilometraje;
        vehicle.FechaUltimaInspeccion = inspection.FechaInspeccion;
        if (inspection.Resultado == "RECHAZADO")
        {
            vehicle.EstadoOperativo = "FUERA_DE_SERVICIO";
            vehicle.ObservacionesMantenimiento = "Unidad marcada fuera de servicio por inspeccion diaria rechazada.";
        }

        _context.InspeccionesDiarias.Add(inspection);
        await _context.SaveChangesAsync(cancellationToken);

        model.IdInspeccionDiaria = inspection.IdInspeccionDiaria;
        model.IdConductorUsuario = inspection.IdConductorUsuario;
        model.NombreVehiculo = vehicle.CodigoInterno + " - " + vehicle.Placa;
        return CreatedAtAction(nameof(GetAll), new { id = inspection.IdInspeccionDiaria }, model);
    }

    private static string ResolveInspectionResult(InspectionDto model)
    {
        var failures = new[]
        {
            model.LucesOperativas,
            model.FrenosOperativos,
            model.NeumaticosOperativos,
            model.BocinaOperativa,
            model.EspejosOperativos,
            model.BotiquinDisponible,
            model.ExtintorDisponible,
            model.DocumentosCompletos
        }.Count(value => !value);

        if (failures >= 2 || !model.FrenosOperativos || !model.NeumaticosOperativos)
        {
            return "RECHAZADO";
        }

        if (failures == 1 || !model.LimpiezaCabina || !model.LimpiezaExterior)
        {
            return "OBSERVADO";
        }

        return "APROBADO";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
