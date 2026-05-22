using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class Pasajero
{
    public int IdPasajero { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual ICollection<FavoritoPasajero> Favoritos { get; set; } = new List<FavoritoPasajero>();

    [JsonIgnore]
    public virtual ICollection<ConsultaRuta> Consultas { get; set; } = new List<ConsultaRuta>();
}
