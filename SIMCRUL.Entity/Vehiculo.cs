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
    public decimal KilometrajeActual { get; set; }
    public string EstadoOperativo { get; set; } = "OPERATIVO";
    public DateTime? FechaUltimaInspeccion { get; set; }
    public DateTime? FechaUltimoMantenimiento { get; set; }
    public string? ObservacionesMantenimiento { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual EmpresaTransporte? EmpresaTransporte { get; set; }

    [JsonIgnore]
    public virtual ICollection<DispositivoGps> DispositivosGps { get; set; } = new List<DispositivoGps>();

    [JsonIgnore]
    public virtual ICollection<AsignacionOperacion> Asignaciones { get; set; } = new List<AsignacionOperacion>();

    [JsonIgnore]
    public virtual ICollection<InspeccionDiaria> InspeccionesDiarias { get; set; } = new List<InspeccionDiaria>();

    [JsonIgnore]
    public virtual ICollection<IncidenciaMantenimiento> IncidenciasMantenimiento { get; set; } = new List<IncidenciaMantenimiento>();

    [JsonIgnore]
    public virtual ICollection<PlanMantenimientoPreventivo> PlanesMantenimientoPreventivo { get; set; } = new List<PlanMantenimientoPreventivo>();

    [JsonIgnore]
    public virtual ICollection<OrdenTrabajo> OrdenesTrabajo { get; set; } = new List<OrdenTrabajo>();
}
