using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class Vehiculo
{
    public int IdVehiculo { get; set; }
    public int IdEmpresa { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string CodigoInterno { get; set; } = string.Empty;
    public string TipoVehiculo { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Anio { get; set; }
    public int CapacidadPasajeros { get; set; }
    public decimal VelocidadMaximaKmh { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual EmpresaTransporte? EmpresaTransporte { get; set; }

    [JsonIgnore]
    public virtual ICollection<DispositivoGps> DispositivosGps { get; set; } = new List<DispositivoGps>();

    [JsonIgnore]
    public virtual ICollection<AsignacionOperacion> Asignaciones { get; set; } = new List<AsignacionOperacion>();
}
