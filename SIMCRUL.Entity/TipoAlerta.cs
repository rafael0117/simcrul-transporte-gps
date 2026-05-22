using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class TipoAlerta
{
    public int IdTipoAlerta { get; set; }
    public string Codigo { get; set; } = string.Empty; // VELOCIDAD, DESVIO_RUTA, etc.
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int NivelSeveridad { get; set; } // 1 to 5
    public bool Activa { get; set; } = true;

    [JsonIgnore]
    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}
