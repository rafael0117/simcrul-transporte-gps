using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class Conductor
{
    public int IdConductor { get; set; }
    public int IdEmpresa { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string NumeroLicencia { get; set; } = string.Empty;
    public string CategoriaLicencia { get; set; } = string.Empty;
    public DateTime FechaVencimientoLicencia { get; set; }
    public string? Telefono { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual EmpresaTransporte? EmpresaTransporte { get; set; }

    [JsonIgnore]
    public virtual ICollection<AsignacionOperacion> Asignaciones { get; set; } = new List<AsignacionOperacion>();
}
