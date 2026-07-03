namespace SIMCRUL.Entity;

public class PlanMantenimientoPreventivo
{
    public int IdPlanMantenimientoPreventivo { get; set; }
    public int IdVehiculo { get; set; }
    public int IdCreadoPorUsuario { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime ProximaFechaProgramada { get; set; }
    public int FrecuenciaDias { get; set; }
    public decimal? FrecuenciaKilometros { get; set; }
    public string Actividades { get; set; } = string.Empty;
    public string Estado { get; set; } = "PROGRAMADO";
    public string Prioridad { get; set; } = "MEDIA";
    public string? Observaciones { get; set; }
    public DateTime? UltimaEjecucion { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual Usuario CreadoPorUsuario { get; set; } = null!;
    public virtual ICollection<OrdenTrabajo> OrdenesTrabajo { get; set; } = new List<OrdenTrabajo>();
}
