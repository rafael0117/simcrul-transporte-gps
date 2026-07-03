namespace SIMCRUL.Entity;

public class OrdenTrabajo
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
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public DateTime FechaProgramada { get; set; } = DateTime.UtcNow;
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string TrabajoSolicitado { get; set; } = string.Empty;
    public string? DiagnosticoInicial { get; set; }
    public string? Observaciones { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual Usuario GeneradoPorUsuario { get; set; } = null!;
    public virtual Usuario? TecnicoUsuario { get; set; }
    public virtual PlanMantenimientoPreventivo? PlanMantenimientoPreventivo { get; set; }
    public virtual IncidenciaMantenimiento? IncidenciaMantenimiento { get; set; }
    public virtual ICollection<MantenimientoEjecutado> MantenimientosEjecutados { get; set; } = new List<MantenimientoEjecutado>();
}
