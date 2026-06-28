using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.DTOs.Dashboard;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;
using System.Data;
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

    [HttpGet("operational-summary")]
    public async Task<IActionResult> GetOperationalSummary(DateTime? dateFrom, DateTime? dateTo, int? routeId, CancellationToken cancellationToken)
    {
        try
        {
            var from = (dateFrom ?? DateTime.Today.AddDays(-7)).Date;
            var to = (dateTo ?? DateTime.Today).Date;
            if (from > to)
            {
                return BadRequest(new { message = "La fecha desde no puede ser mayor a la fecha hasta." });
            }

            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SP_DASHBOARD_OPERATIVO_RESUMEN";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@FechaDesde", SqlDbType.Date) { Value = from });
            command.Parameters.Add(new SqlParameter("@FechaHasta", SqlDbType.Date) { Value = to });
            command.Parameters.Add(new SqlParameter("@IdRuta", SqlDbType.Int) { Value = routeId.HasValue ? routeId.Value : DBNull.Value });

            var summary = new OperationalDashboardSummaryDto();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                summary.DistanciaRecorridaKm = reader.GetDecimal(reader.GetOrdinal("DistanciaRecorridaKm"));
                summary.TiempoOperativoMin = reader.GetInt32(reader.GetOrdinal("TiempoOperativoMin"));
                summary.ExcesosVelocidad = reader.GetInt32(reader.GetOrdinal("ExcesosVelocidad"));
                summary.DesviosRuta = reader.GetInt32(reader.GetOrdinal("DesviosRuta"));
                summary.ReclamosRecibidos = reader.GetInt32(reader.GetOrdinal("ReclamosRecibidos"));
                summary.TopEmpresa = reader.GetString(reader.GetOrdinal("TopEmpresa"));
                summary.TopVehiculo = reader.GetString(reader.GetOrdinal("TopVehiculo"));
                summary.TopRuta = reader.GetString(reader.GetOrdinal("TopRuta"));
                summary.TopDistanciaKm = reader.GetDecimal(reader.GetOrdinal("TopDistanciaKm"));
            }

            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    summary.ExcesosPorDia.Add(new OperationalChartPointDto
                    {
                        Label = reader.GetString(reader.GetOrdinal("Label")),
                        Value = reader.GetDecimal(reader.GetOrdinal("Value"))
                    });
                }
            }

            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    summary.UnidadesPorHora.Add(new OperationalChartPointDto
                    {
                        Label = reader.GetString(reader.GetOrdinal("Label")),
                        Value = reader.GetDecimal(reader.GetOrdinal("Value"))
                    });
                }
            }

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener estadisticas operativas.", details = ex.Message });
        }
    }

    [HttpGet("operational-detail/{type}")]
    public async Task<IActionResult> GetOperationalDetail(string type, DateTime? dateFrom, DateTime? dateTo, int? routeId, CancellationToken cancellationToken)
    {
        var allowedTypes = new[] { "distancia", "tiempo", "velocidad", "desvio" };
        if (!allowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Tipo de detalle no soportado." });
        }

        try
        {
            var from = (dateFrom ?? DateTime.Today.AddDays(-7)).Date;
            var to = (dateTo ?? DateTime.Today).Date;

            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SP_DASHBOARD_OPERATIVO_DETALLE";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.NVarChar, 30) { Value = type.ToLowerInvariant() });
            command.Parameters.Add(new SqlParameter("@FechaDesde", SqlDbType.Date) { Value = from });
            command.Parameters.Add(new SqlParameter("@FechaHasta", SqlDbType.Date) { Value = to });
            command.Parameters.Add(new SqlParameter("@IdRuta", SqlDbType.Int) { Value = routeId.HasValue ? routeId.Value : DBNull.Value });

            var rows = new List<OperationalDetailDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new OperationalDetailDto
                {
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    Empresa = reader.GetString(reader.GetOrdinal("Empresa")),
                    Placa = reader.GetString(reader.GetOrdinal("Placa")),
                    CodigoVehiculo = reader.GetString(reader.GetOrdinal("CodigoVehiculo")),
                    Ruta = reader.GetString(reader.GetOrdinal("Ruta")),
                    DistanciaKm = reader.GetDecimal(reader.GetOrdinal("DistanciaKm")),
                    TiempoOperativoMin = reader.GetInt32(reader.GetOrdinal("TiempoOperativoMin")),
                    Eventos = reader.GetInt32(reader.GetOrdinal("Eventos")),
                    ValorMaximo = reader.IsDBNull(reader.GetOrdinal("ValorMaximo")) ? null : reader.GetDecimal(reader.GetOrdinal("ValorMaximo")),
                    Descripcion = reader.GetString(reader.GetOrdinal("Descripcion")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado"))
                });
            }

            return Ok(rows);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener detalle operativo.", details = ex.Message });
        }
    }

    [HttpGet("route-options")]
    public async Task<IActionResult> GetRouteOptions(CancellationToken cancellationToken)
    {
        var routes = await _context.Rutas
            .Where(r => r.Activa)
            .OrderBy(r => r.CodigoRuta)
            .Select(r => new RouteOptionDto
            {
                IdRuta = r.IdRuta,
                CodigoRuta = r.CodigoRuta,
                NombreRuta = r.NombreRuta
            })
            .ToListAsync(cancellationToken);

        return Ok(routes);
    }

    [HttpGet("operational-alerts")]
    public async Task<IActionResult> GetOperationalAlerts(DateTime? dateFrom, DateTime? dateTo, int? routeId, CancellationToken cancellationToken)
    {
        try
        {
            var from = (dateFrom ?? DateTime.Today.AddDays(-7)).Date;
            var toExclusive = (dateTo ?? DateTime.Today).Date.AddDays(1);

            var alerts = await _context.Alertas
                .Include(a => a.TipoAlerta)
                .Include(a => a.Vehiculo)
                .Include(a => a.Conductor)
                .Include(a => a.Viaje)
                    .ThenInclude(v => v!.AsignacionOperacion)
                        .ThenInclude(ao => ao.Ruta)
                .Where(a => a.FechaAlerta >= from && a.FechaAlerta < toExclusive)
                .Where(a => routeId == null || (a.Viaje != null && a.Viaje.AsignacionOperacion.IdRuta == routeId.Value))
                .OrderByDescending(a => a.FechaAlerta)
                .Take(30)
                .Select(a => new OperationalAlertNotificationDto
                {
                    IdAlerta = a.IdAlerta,
                    TipoCodigo = a.TipoAlerta.Codigo,
                    Tipo = a.TipoAlerta.Nombre,
                    Severidad = a.TipoAlerta.NivelSeveridad,
                    Placa = a.Vehiculo.Placa,
                    CodigoVehiculo = a.Vehiculo.CodigoInterno,
                    Ruta = a.Viaje == null
                        ? "Sin ruta"
                        : a.Viaje.AsignacionOperacion.Ruta.CodigoRuta + " - " + a.Viaje.AsignacionOperacion.Ruta.NombreRuta,
                    Conductor = a.Conductor == null ? "Sin conductor" : a.Conductor.Nombres + " " + a.Conductor.Apellidos,
                    FechaAlerta = a.FechaAlerta,
                    Descripcion = a.Descripcion,
                    ValorDetectado = a.ValorDetectado,
                    ValorPermitido = a.ValorPermitido,
                    Estado = a.Estado
                })
                .ToListAsync(cancellationToken);

            return Ok(alerts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener alertas operativas.", details = ex.Message });
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
