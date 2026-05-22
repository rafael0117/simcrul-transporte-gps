using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.DTOs.Dashboard;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;
using System.Security.Claims;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var activeTrips = await _context.Viajes
                .Include(v => v.AsignacionOperacion)
                .Where(v => v.Estado == "EN_PROGRESO")
                .ToListAsync(cancellationToken);

            var alertsTodayCount = await _context.Alertas
                .CountAsync(a => a.FechaAlerta >= today && a.FechaAlerta < tomorrow, cancellationToken);

            double avgSpeed = 0.0;
            if (activeTrips.Count > 0)
            {
                var tripIds = activeTrips.Select(v => (long?)v.IdViaje).ToList();
                avgSpeed = await _context.GpsLecturas
                    .Where(g => tripIds.Contains(g.IdViaje))
                    .AverageAsync(g => (double?)g.VelocidadKmh, cancellationToken) ?? 0.0;
            }

            var routesMonitored = activeTrips.Select(v => v.AsignacionOperacion.IdRuta).Distinct().Count();
            
            var avgScore = activeTrips.Count > 0 
                ? activeTrips.Average(v => v.ScoreConduccion) 
                : 100.0;

            var activeDrivers = activeTrips.Select(v => v.AsignacionOperacion.IdConductor).Distinct().Count();

            var kpis = new DashboardKpisDto
            {
                VehiculosActivos = activeTrips.Count,
                AlertasHoy = alertsTodayCount,
                VelocidadPromedioKmh = Math.Round(avgSpeed, 1),
                RutasMonitoreadas = routesMonitored,
                ScorePromedioConductores = Math.Round(avgScore, 1),
                ConductoresConectados = activeDrivers
            };

            return Ok(kpis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener KPIs.", details = ex.Message });
        }
    }

    [HttpGet("active-vehicles")]
    public async Task<IActionResult> GetActiveVehicles(CancellationToken cancellationToken)
    {
        try
        {
            var vehicles = await _context.VwUltimaUbicacionVehiculos.ToListAsync(cancellationToken);
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener vehículos activos.", details = ex.Message });
        }
    }

    [HttpGet("pending-alerts")]
    public async Task<IActionResult> GetPendingAlerts(CancellationToken cancellationToken)
    {
        try
        {
            var alerts = await _context.VwAlertasPendientes
                .Where(a => a.Estado == "PENDIENTE")
                .ToListAsync(cancellationToken);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener alertas pendientes.", details = ex.Message });
        }
    }

    [HttpPost("attend-alert/{id}")]
    public async Task<IActionResult> AttendAlert(long id, [FromBody] string observations, CancellationToken cancellationToken)
    {
        try
        {
            var alert = await _context.Alertas.FirstOrDefaultAsync(a => a.IdAlerta == id, cancellationToken);
            if (alert == null)
            {
                return NotFound(new { message = "No se encontró la alerta especificada." });
            }

            // Find current user from claims or default
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

            alert.Estado = "ATENDIDA";
            alert.AtendidoPor = userId ?? 1; // Default to Admin
            alert.FechaAtencion = DateTime.UtcNow;
            alert.ObservacionesAtencion = observations;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Alerta atendida con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al atender la alerta.", details = ex.Message });
        }
    }

    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes(CancellationToken cancellationToken)
    {
        try
        {
            var routes = await _context.Rutas
                .Include(r => r.RutaParaderos.OrderBy(rp => rp.Orden))
                    .ThenInclude(rp => rp.Paradero)
                .Include(r => r.RutaPuntosControl.OrderBy(rpc => rpc.Orden))
                .Where(r => r.Activa)
                .ToListAsync(cancellationToken);
            return Ok(routes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener las rutas.", details = ex.Message });
        }
    }

    [HttpGet("route/{id}/details")]
    public async Task<IActionResult> GetRouteDetails(int id, CancellationToken cancellationToken)
    {
        try
        {
            var route = await _context.Rutas
                .Include(r => r.RutaParaderos.OrderBy(rp => rp.Orden))
                    .ThenInclude(rp => rp.Paradero)
                .Include(r => r.RutaPuntosControl.OrderBy(rpc => rpc.Orden))
                .FirstOrDefaultAsync(r => r.IdRuta == id && r.Activa, cancellationToken);

            if (route == null) return NotFound(new { message = "Ruta no encontrada." });
            return Ok(route);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener detalles de la ruta.", details = ex.Message });
        }
    }
}
