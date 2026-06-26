using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class Paradero
{
    public int IdParadero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? DireccionReferencia { get; set; }
    public string Distrito { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public bool Activo { get; set; } = true;

    [JsonIgnore]
    public virtual ICollection<RutaParadero> RutaParaderos { get; set; } = new List<RutaParadero>();
}
