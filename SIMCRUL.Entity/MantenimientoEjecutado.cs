namespace SIMCRUL.Entity;

public class MantenimientoEjecutado
{
    public int IdMantenimientoEjecutado { get; set; }
    public int IdOrdenTrabajo { get; set; }
    public int IdTecnicoUsuario { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public string TipoMantenimiento { get; set; } = "CORRECTIVO";
    public string Diagnostico { get; set; } = string.Empty;
    public string AccionesRealizadas { get; set; } = string.Empty;
    public string? Recomendaciones { get; set; }
    public string EstadoResultado { get; set; } = "COMPLETADO";
    public string NuevoEstadoOperativoVehiculo { get; set; } = "OPERATIVO";

    public virtual OrdenTrabajo OrdenTrabajo { get; set; } = null!;
    public virtual Usuario TecnicoUsuario { get; set; } = null!;
    public virtual ICollection<RepuestoUtilizado> RepuestosUtilizados { get; set; } = new List<RepuestoUtilizado>();
}
