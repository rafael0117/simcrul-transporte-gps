using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class GpsLectura
{
    public long IdLectura { get; set; }
    public long? IdViaje { get; set; }
    public int IdVehiculo { get; set; }
    public int? IdDispositivo { get; set; }
    public DateTime FechaGps { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public decimal VelocidadKmh { get; set; }
    public decimal? RumboGrados { get; set; }
    public decimal? PrecisionMetros { get; set; }
    public string OrigenDato { get; set; } = "SIMULADOR"; // SIMULADOR, DISPOSITIVO
    public bool Procesado { get; set; } = false;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public virtual Viaje? Viaje { get; set; }
    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual DispositivoGps? DispositivoGps { get; set; }

    [JsonIgnore]
    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}
