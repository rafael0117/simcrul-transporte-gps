using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Maintenance;

public class VehicleOptionDto
{
    public int IdVehiculo { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public string EstadoOperativo { get; set; } = string.Empty;
}

public class UserOptionDto
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public class InspectionDto
{
    public int IdInspeccionDiaria { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un vehiculo.")]
    public int IdVehiculo { get; set; }

    public int IdConductorUsuario { get; set; }
    public string? NombreVehiculo { get; set; }
    public string? NombreConductor { get; set; }
    public DateTime FechaInspeccion { get; set; } = DateTime.Now;

    [Range(0, double.MaxValue, ErrorMessage = "El kilometraje no puede ser negativo.")]
    public decimal Kilometraje { get; set; }

    [Required(ErrorMessage = "Debe indicar el nivel de combustible.")]
    public string NivelCombustible { get; set; } = "MEDIO";

    public bool LimpiezaCabina { get; set; } = true;
    public bool LimpiezaExterior { get; set; } = true;
    public bool LucesOperativas { get; set; } = true;
    public bool FrenosOperativos { get; set; } = true;
    public bool NeumaticosOperativos { get; set; } = true;
    public bool BocinaOperativa { get; set; } = true;
    public bool EspejosOperativos { get; set; } = true;
    public bool BotiquinDisponible { get; set; } = true;
    public bool ExtintorDisponible { get; set; } = true;
    public bool DocumentosCompletos { get; set; } = true;
    public string Resultado { get; set; } = "APROBADO";
    public string? Observaciones { get; set; }
}

public class IncidentDto
{
    public int IdIncidenciaMantenimiento { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un vehiculo.")]
    public int IdVehiculo { get; set; }

    public int IdReportadoPorUsuario { get; set; }
    public int? IdInspeccionDiaria { get; set; }
    public DateTime FechaReporte { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Debe indicar el tipo de incidencia.")]
    public string TipoIncidencia { get; set; } = "MECANICA";

    [Required(ErrorMessage = "Debe indicar la severidad.")]
    public string Severidad { get; set; } = "MEDIA";

    [Required(ErrorMessage = "El titulo es obligatorio.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripcion es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    public string Estado { get; set; } = "REPORTADA";
    public bool RequiereParada { get; set; }
    public string? UbicacionReferencia { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string? NombreVehiculo { get; set; }
    public string? NombreReportadoPor { get; set; }
}

public class PreventivePlanDto
{
    public int IdPlanMantenimientoPreventivo { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un vehiculo.")]
    public int IdVehiculo { get; set; }

    public int IdCreadoPorUsuario { get; set; }
    public string? NombreVehiculo { get; set; }
    public string? NombreCreadoPor { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "La fecha programada es obligatoria.")]
    public DateTime ProximaFechaProgramada { get; set; } = DateTime.Today.AddDays(1);

    [Range(1, 3650, ErrorMessage = "La frecuencia en dias debe ser mayor a cero.")]
    public int FrecuenciaDias { get; set; } = 30;

    [Range(0, double.MaxValue, ErrorMessage = "La frecuencia en kilometros no puede ser negativa.")]
    public decimal? FrecuenciaKilometros { get; set; }

    [Required(ErrorMessage = "Debe detallar las actividades.")]
    public string Actividades { get; set; } = string.Empty;

    public string Estado { get; set; } = "PROGRAMADO";
    public string Prioridad { get; set; } = "MEDIA";
    public string? Observaciones { get; set; }
    public DateTime? UltimaEjecucion { get; set; }
}

public class SparePartDto
{
    [Required(ErrorMessage = "El codigo del repuesto es obligatorio.")]
    public string CodigoRepuesto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del repuesto es obligatorio.")]
    public string NombreRepuesto { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public decimal Cantidad { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "El costo no puede ser negativo.")]
    public decimal CostoUnitario { get; set; }

    public string? Observaciones { get; set; }
}

public class WorkOrderDto
{
    public int IdOrdenTrabajo { get; set; }
    public int IdVehiculo { get; set; }
    public int IdGeneradoPorUsuario { get; set; }
    public int? IdTecnicoUsuario { get; set; }
    public int? IdPlanMantenimientoPreventivo { get; set; }
    public int? IdIncidenciaMantenimiento { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public string TipoOrden { get; set; } = "CORRECTIVO";
    public string Prioridad { get; set; } = "MEDIA";
    public string Estado { get; set; } = "GENERADA";
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public DateTime FechaProgramada { get; set; } = DateTime.Today;
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    [Required(ErrorMessage = "Debe describir el trabajo solicitado.")]
    public string TrabajoSolicitado { get; set; } = string.Empty;

    public string? DiagnosticoInicial { get; set; }
    public string? Observaciones { get; set; }
    public string? NombreVehiculo { get; set; }
    public string? NombreGeneradoPor { get; set; }
    public string? NombreTecnico { get; set; }
    public string? TituloIncidencia { get; set; }
    public string? ActividadesPlan { get; set; }
    public List<SparePartDto> Repuestos { get; set; } = [];
}

public class WorkOrderAssignmentDto
{
    [Required(ErrorMessage = "Debe indicar el tecnico responsable.")]
    public int IdTecnicoUsuario { get; set; }

    [Required(ErrorMessage = "Debe indicar la fecha programada.")]
    public DateTime FechaProgramada { get; set; } = DateTime.Today;

    public string? Observaciones { get; set; }
}

public class MaintenanceExecutionDto
{
    public int IdOrdenTrabajo { get; set; }

    [Required(ErrorMessage = "Debe registrar el inicio del mantenimiento.")]
    public DateTime FechaInicio { get; set; } = DateTime.Now;

    public DateTime? FechaFin { get; set; } = DateTime.Now.AddHours(1);

    [Required(ErrorMessage = "El diagnostico es obligatorio.")]
    public string Diagnostico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe detallar las acciones realizadas.")]
    public string AccionesRealizadas { get; set; } = string.Empty;

    public string? Recomendaciones { get; set; }
    public string TipoMantenimiento { get; set; } = "CORRECTIVO";
    public string EstadoResultado { get; set; } = "COMPLETADO";
    public string NuevoEstadoOperativoVehiculo { get; set; } = "OPERATIVO";
    public List<SparePartDto> Repuestos { get; set; } = [];
}

public class MaintenanceHistoryItemDto
{
    public string TipoRegistro { get; set; } = string.Empty;
    public string CodigoReferencia { get; set; } = string.Empty;
    public string Vehiculo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string Resumen { get; set; } = string.Empty;
}

public class ChartDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class UpcomingPlanDto
{
    public int IdPlanMantenimientoPreventivo { get; set; }
    public string Vehiculo { get; set; } = string.Empty;
    public DateTime ProximaFechaProgramada { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public string Actividades { get; set; } = string.Empty;
}

public class MaintenanceDashboardDto
{
    public int TotalVehiculos { get; set; }
    public int VehiculosOperativos { get; set; }
    public int VehiculosEnMantenimiento { get; set; }
    public int VehiculosFueraDeServicio { get; set; }
    public int InspeccionesHoy { get; set; }
    public int IncidenciasAbiertas { get; set; }
    public int OrdenesPendientes { get; set; }
    public int OrdenesFinalizadasMes { get; set; }
    public List<ChartDataPointDto> IncidenciasPorTipo { get; set; } = [];
    public List<ChartDataPointDto> OrdenesPorEstado { get; set; } = [];
    public List<ChartDataPointDto> HistorialMensual { get; set; } = [];
    public List<UpcomingPlanDto> ProximosPreventivos { get; set; } = [];
}

public class ExportFilterDto
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}
