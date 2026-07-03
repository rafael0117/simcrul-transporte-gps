using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Data.Context;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/maintenance/dashboard")]
[Authorize]
public class MaintenanceDashboardController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public MaintenanceDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var dto = new MaintenanceDashboardDto
        {
            TotalVehiculos = await _context.Vehiculos.CountAsync(v => v.Estado, cancellationToken),
            VehiculosOperativos = await _context.Vehiculos.CountAsync(v => v.Estado && v.EstadoOperativo == "OPERATIVO", cancellationToken),
            VehiculosEnMantenimiento = await _context.Vehiculos.CountAsync(v => v.Estado && v.EstadoOperativo == "EN_MANTENIMIENTO", cancellationToken),
            VehiculosFueraDeServicio = await _context.Vehiculos.CountAsync(v => v.Estado && v.EstadoOperativo == "FUERA_DE_SERVICIO", cancellationToken),
            InspeccionesHoy = await _context.InspeccionesDiarias.CountAsync(i => i.FechaInspeccion >= today, cancellationToken),
            IncidenciasAbiertas = await _context.IncidenciasMantenimiento.CountAsync(i => i.Estado != "CERRADA", cancellationToken),
            OrdenesPendientes = await _context.OrdenesTrabajo.CountAsync(o => o.Estado != "FINALIZADA", cancellationToken),
            OrdenesFinalizadasMes = await _context.OrdenesTrabajo.CountAsync(o => o.Estado == "FINALIZADA" && o.FechaFin >= monthStart, cancellationToken)
        };

        dto.IncidenciasPorTipo = await _context.IncidenciasMantenimiento
            .GroupBy(i => i.TipoIncidencia)
            .Select(g => new ChartDataPointDto
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        dto.OrdenesPorEstado = await _context.OrdenesTrabajo
            .GroupBy(o => o.Estado)
            .Select(g => new ChartDataPointDto
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        dto.HistorialMensual = await _context.OrdenesTrabajo
            .Where(o => o.FechaGeneracion >= today.AddMonths(-5))
            .GroupBy(o => new { o.FechaGeneracion.Year, o.FechaGeneracion.Month })
            .Select(g => new ChartDataPointDto
            {
                Label = g.Key.Year + "-" + g.Key.Month.ToString("00"),
                Value = g.Count()
            })
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);

        dto.ProximosPreventivos = await _context.PlanesMantenimientoPreventivo
            .Include(p => p.Vehiculo)
            .Where(p => p.Estado == "PROGRAMADO")
            .OrderBy(p => p.ProximaFechaProgramada)
            .Take(5)
            .Select(p => new UpcomingPlanDto
            {
                IdPlanMantenimientoPreventivo = p.IdPlanMantenimientoPreventivo,
                Vehiculo = p.Vehiculo.CodigoInterno + " - " + p.Vehiculo.Placa,
                ProximaFechaProgramada = p.ProximaFechaProgramada,
                Prioridad = p.Prioridad,
                Actividades = p.Actividades
            })
            .ToListAsync(cancellationToken);

        return Ok(dto);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador, Roles.JefeMantenimiento))
        {
            return Forbid();
        }

        var inspections = await _context.InspeccionesDiarias
            .Include(i => i.Vehiculo)
            .Include(i => i.ConductorUsuario)
            .OrderByDescending(i => i.FechaInspeccion)
            .Take(20)
            .Select(i => new MaintenanceHistoryItemDto
            {
                TipoRegistro = "INSPECCION",
                CodigoReferencia = "INS-" + i.IdInspeccionDiaria,
                Vehiculo = i.Vehiculo.CodigoInterno + " - " + i.Vehiculo.Placa,
                Fecha = i.FechaInspeccion,
                Estado = i.Resultado,
                Responsable = (i.ConductorUsuario.Nombres + " " + i.ConductorUsuario.Apellidos).Trim(),
                Resumen = i.Observaciones ?? "Inspeccion diaria registrada."
            })
            .ToListAsync(cancellationToken);

        var orders = await _context.OrdenesTrabajo
            .Include(o => o.Vehiculo)
            .Include(o => o.TecnicoUsuario)
            .OrderByDescending(o => o.FechaProgramada)
            .Take(20)
            .Select(o => new MaintenanceHistoryItemDto
            {
                TipoRegistro = "ORDEN",
                CodigoReferencia = o.NumeroOrden,
                Vehiculo = o.Vehiculo.CodigoInterno + " - " + o.Vehiculo.Placa,
                Fecha = o.FechaFin ?? o.FechaProgramada,
                Estado = o.Estado,
                Responsable = o.TecnicoUsuario == null ? "Sin tecnico" : (o.TecnicoUsuario.Nombres + " " + o.TecnicoUsuario.Apellidos).Trim(),
                Resumen = o.TrabajoSolicitado
            })
            .ToListAsync(cancellationToken);

        var incidents = await _context.IncidenciasMantenimiento
            .Include(i => i.Vehiculo)
            .Include(i => i.ReportadoPorUsuario)
            .OrderByDescending(i => i.FechaReporte)
            .Take(20)
            .Select(i => new MaintenanceHistoryItemDto
            {
                TipoRegistro = "INCIDENCIA",
                CodigoReferencia = "INC-" + i.IdIncidenciaMantenimiento,
                Vehiculo = i.Vehiculo.CodigoInterno + " - " + i.Vehiculo.Placa,
                Fecha = i.FechaReporte,
                Estado = i.Estado,
                Responsable = (i.ReportadoPorUsuario.Nombres + " " + i.ReportadoPorUsuario.Apellidos).Trim(),
                Resumen = i.Titulo
            })
            .ToListAsync(cancellationToken);

        var history = inspections
            .Concat(orders)
            .Concat(incidents)
            .OrderByDescending(h => h.Fecha)
            .Take(50)
            .ToList();

        return Ok(history);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador, Roles.JefeMantenimiento))
        {
            return Forbid();
        }

        var from = fechaDesde?.Date ?? DateTime.UtcNow.Date.AddMonths(-1);
        var to = (fechaHasta?.Date ?? DateTime.UtcNow.Date).AddDays(1);

        var orders = await _context.OrdenesTrabajo
            .Include(o => o.Vehiculo)
            .Include(o => o.TecnicoUsuario)
            .Where(o => o.FechaGeneracion >= from && o.FechaGeneracion < to)
            .OrderByDescending(o => o.FechaGeneracion)
            .ToListAsync(cancellationToken);

        var incidents = await _context.IncidenciasMantenimiento
            .Include(i => i.Vehiculo)
            .Include(i => i.ReportadoPorUsuario)
            .Where(i => i.FechaReporte >= from && i.FechaReporte < to)
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Tipo,Referencia,Vehiculo,Fecha,Estado,Responsable,Detalle");

        foreach (var order in orders)
        {
            builder.AppendLine(ToCsvRow(
                "ORDEN",
                order.NumeroOrden,
                order.Vehiculo.CodigoInterno + " - " + order.Vehiculo.Placa,
                order.FechaGeneracion.ToString("yyyy-MM-dd HH:mm"),
                order.Estado,
                order.TecnicoUsuario == null ? "Sin tecnico" : (order.TecnicoUsuario.Nombres + " " + order.TecnicoUsuario.Apellidos).Trim(),
                order.TrabajoSolicitado));
        }

        foreach (var incident in incidents)
        {
            builder.AppendLine(ToCsvRow(
                "INCIDENCIA",
                "INC-" + incident.IdIncidenciaMantenimiento,
                incident.Vehiculo.CodigoInterno + " - " + incident.Vehiculo.Placa,
                incident.FechaReporte.ToString("yyyy-MM-dd HH:mm"),
                incident.Estado,
                (incident.ReportadoPorUsuario.Nombres + " " + incident.ReportadoPorUsuario.Apellidos).Trim(),
                incident.Titulo));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return File(bytes, "text/csv", $"reporte-mantenimiento-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static string ToCsvRow(params string[] values)
    {
        return string.Join(",", values.Select(EscapeCsv));
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n'))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }
}
