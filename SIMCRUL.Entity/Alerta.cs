namespace SIMCRUL.Entity;

public class Alerta
{
    public long IdAlerta { get; set; }
    public int IdTipoAlerta { get; set; }
    public long? IdViaje { get; set; }
    public int IdVehiculo { get; set; }
    public int? IdConductor { get; set; }
    public long? IdLecturaGps { get; set; }
    public DateTime FechaAlerta { get; set; } = DateTime.UtcNow;
    public string Descripcion { get; set; } = string.Empty;
    public decimal? ValorDetectado { get; set; }
    public decimal? ValorPermitido { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, ATENDIDA, DESCARTADA
    public int? AtendidoPor { get; set; }
    public DateTime? FechaAtencion { get; set; }
    public string? ObservacionesAtencion { get; set; }

    public virtual TipoAlerta TipoAlerta { get; set; } = null!;
    public virtual Viaje? Viaje { get; set; }
    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual Conductor? Conductor { get; set; }
    public virtual GpsLectura? GpsLectura { get; set; }
    public virtual Usuario? AtendidoPorUsuario { get; set; }
}
