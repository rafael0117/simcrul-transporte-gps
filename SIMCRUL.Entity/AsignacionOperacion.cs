using System.Text.Json.Serialization;

namespace SIMCRUL.Entity;

public class AsignacionOperacion
{
    public int IdAsignacion { get; set; }
    public int IdRuta { get; set; }
    public int IdVehiculo { get; set; }
    public int IdConductor { get; set; }
    public DateTime FechaInicioProgramada { get; set; }
    public DateTime FechaFinProgramada { get; set; }
    public string Turno { get; set; } = string.Empty;
    public string Estado { get; set; } = "PROGRAMADA"; // PROGRAMADA, ACTIVA, COMPLETADA, CANCELADA
    public string? Observaciones { get; set; }

    public virtual Ruta Ruta { get; set; } = null!;
    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual Conductor Conductor { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
}
