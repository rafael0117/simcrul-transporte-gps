using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class DispositivoGps
{
    public int IdDispositivo { get; set; }
    public int? IdVehiculo { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string NumeroSerie { get; set; } = string.Empty;
    public string? Proveedor { get; set; }
    public DateTime FechaInstalacion { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime? UltimaSincronizacion { get; set; }

    public virtual Vehiculo? Vehiculo { get; set; }
}
