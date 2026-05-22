namespace SIMCRUL.Entity;

public class VwAlertaPendiente
{
    public long IdAlerta { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public string Vehiculo { get; set; } = string.Empty;
    public string Conductor { get; set; } = string.Empty;
    public DateTime FechaAlerta { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string Estado { get; set; } = string.Empty;
}
