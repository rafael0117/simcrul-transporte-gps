using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class EmpresaTransporte
{
    public int IdEmpresa { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual ICollection<Conductor> Conductores { get; set; } = new List<Conductor>();

    [JsonIgnore]
    public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();

    [JsonIgnore]
    public virtual ICollection<Ruta> Rutas { get; set; } = new List<Ruta>();
}
