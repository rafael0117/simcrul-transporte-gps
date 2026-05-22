using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class Ruta
{
    public int IdRuta { get; set; }
    public int IdEmpresa { get; set; }
    public string CodigoRuta { get; set; } = string.Empty;
    public string NombreRuta { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public decimal DistanciaKm { get; set; }
    public int TiempoEstimadoMin { get; set; }
    public decimal VelocidadMaximaKmh { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual EmpresaTransporte? EmpresaTransporte { get; set; }

    public virtual ICollection<RutaParadero> RutaParaderos { get; set; } = new List<RutaParadero>();

    public virtual ICollection<RutaPuntoControl> RutaPuntosControl { get; set; } = new List<RutaPuntoControl>();

    [JsonIgnore]
    public virtual ICollection<AsignacionOperacion> Asignaciones { get; set; } = new List<AsignacionOperacion>();

    [JsonIgnore]
    public virtual ICollection<FavoritoPasajero> Favoritos { get; set; } = new List<FavoritoPasajero>();

    [JsonIgnore]
    public virtual ICollection<ConsultaRuta> Consultas { get; set; } = new List<ConsultaRuta>();
}
