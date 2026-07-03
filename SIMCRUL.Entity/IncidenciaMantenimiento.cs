namespace SIMCRUL.Entity;

public class IncidenciaMantenimiento
{
    public int IdIncidenciaMantenimiento { get; set; }
    public int IdVehiculo { get; set; }
    public int IdReportadoPorUsuario { get; set; }
    public int? IdInspeccionDiaria { get; set; }
    public DateTime FechaReporte { get; set; } = DateTime.UtcNow;
    public string TipoIncidencia { get; set; } = "MECANICA";
    public string Severidad { get; set; } = "MEDIA";
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = "REPORTADA";
    public bool RequiereParada { get; set; }
    public string? UbicacionReferencia { get; set; }
    public DateTime? FechaCierre { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual Usuario ReportadoPorUsuario { get; set; } = null!;
    public virtual InspeccionDiaria? InspeccionDiaria { get; set; }
    public virtual ICollection<OrdenTrabajo> OrdenesTrabajo { get; set; } = new List<OrdenTrabajo>();
}
