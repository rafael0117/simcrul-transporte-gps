using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class Viaje
{
    public long IdViaje { get; set; }
    public int IdAsignacion { get; set; }
    public DateTime FechaInicioReal { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFinReal { get; set; }
    public string Estado { get; set; } = "EN_PROGRESO"; // EN_PROGRESO, FINALIZADO, ABORTADO
    public decimal? LatitudInicio { get; set; }
    public decimal? LongitudInicio { get; set; }
    public decimal? LatitudFin { get; set; }
    public decimal? LongitudFin { get; set; }
    public string? Observaciones { get; set; }
    public int ScoreConduccion { get; set; } = 100; // Driver score starting at 100

    public virtual AsignacionOperacion AsignacionOperacion { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<GpsLectura> GpsLecturas { get; set; } = new List<GpsLectura>();

    [JsonIgnore]
    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}
