using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.Constants;
using SIMCRUL.Data.Context;
using SIMCRUL.Common.DTOs.Telemetry;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GpsController : ControllerBase
{
    private readonly IGpsProcessingService _gpsProcessingService;
    private readonly ApplicationDbContext _context;

    public GpsController(IGpsProcessingService gpsProcessingService, ApplicationDbContext context)
    {
        _gpsProcessingService = gpsProcessingService;
        _context = context;
    }

    [HttpPost("telemetry")]
    public async Task<IActionResult> PostTelemetry([FromBody] TelemetryDto telemetryDto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _gpsProcessingService.ProcessTelemetryAsync(telemetryDto, cancellationToken);
            return Ok(new { status = "success", message = "Telemetría procesada y transmitida correctamente." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { status = "error", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = "Error interno al procesar telemetría.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("driver-options")]
    public async Task<IActionResult> GetDriverOptions(CancellationToken cancellationToken)
    {
        try
        {
            var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var currentUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isDriverSession = string.Equals(currentRole, Roles.Conductor, StringComparison.OrdinalIgnoreCase);

            IQueryable<Entity.AsignacionOperacion> assignmentsQuery = _context.AsignacionesOperacion
                .Include(a => a.Ruta)
                .Include(a => a.Conductor)
                    .ThenInclude(c => c.Usuario)
                .Include(a => a.Vehiculo)
                    .ThenInclude(v => v.DispositivosGps)
                .Where(a => (a.Estado == "ACTIVA" || a.Estado == "PROGRAMADA") &&
                            a.Conductor.Estado &&
                            a.Vehiculo.Estado &&
                            a.Ruta.Activa);

            if (isDriverSession)
            {
                if (!int.TryParse(currentUserIdValue, out var currentUserId))
                {
                    return Unauthorized(new { message = "No se pudo identificar el usuario conductor autenticado." });
                }

                var conductor = await _context.Conductores
                    .Where(c => c.Estado && c.IdUsuario == currentUserId)
                    .Select(c => new { c.IdConductor, Nombre = (c.Nombres + " " + c.Apellidos).Trim() })
                    .FirstOrDefaultAsync(cancellationToken);

                if (conductor == null)
                {
                    return StatusCode(403, new
                    {
                        message = "Tu usuario conductor no esta vinculado a un registro de conductor activo. Solicita al administrador que lo asigne en Mantenimiento de Conductores."
                    });
                }

                assignmentsQuery = assignmentsQuery.Where(a => a.IdConductor == conductor.IdConductor);
            }

            var assignments = await assignmentsQuery
                .OrderByDescending(a => a.Estado == "ACTIVA")
                .ThenBy(a => a.Vehiculo.CodigoInterno)
                .ToListAsync(cancellationToken);

            var options = assignments
                .Select(a =>
                {
                    var device = a.Vehiculo.DispositivosGps
                        .Where(d => d.Estado && !string.IsNullOrWhiteSpace(d.Imei))
                        .OrderByDescending(d => d.IdDispositivo)
                        .FirstOrDefault();

                    if (device == null)
                    {
                        return null;
                    }

                    return new DriverTrackingOptionDto
                    {
                        IdAsignacion = a.IdAsignacion,
                        IdVehiculo = a.IdVehiculo,
                        IdConductor = a.IdConductor,
                        IdRuta = a.IdRuta,
                        EstadoAsignacion = a.Estado,
                        Placa = a.Vehiculo.Placa,
                        CodigoVehiculo = a.Vehiculo.CodigoInterno,
                        Conductor = $"{a.Conductor.Nombres} {a.Conductor.Apellidos}".Trim(),
                        Ruta = $"{a.Ruta.CodigoRuta} - {a.Ruta.NombreRuta}",
                        Imei = device.Imei,
                        VelocidadMaximaKmh = a.Ruta.VelocidadMaximaKmh
                    };
                })
                .Where(option => option != null)
                .Cast<DriverTrackingOptionDto>()
                .ToList();

            return Ok(options);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "No se pudieron cargar las unidades activas para telemetria web.", details = ex.Message });
        }
    }
}
