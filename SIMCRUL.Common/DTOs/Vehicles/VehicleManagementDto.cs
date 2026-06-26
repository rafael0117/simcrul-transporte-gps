using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Vehicles;

public class VehicleManagementDto
{
    public int IdVehiculo { get; set; }

    [Required(ErrorMessage = "La placa es obligatoria.")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código interno es obligatorio.")]
    public string CodigoInterno { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de vehículo es obligatorio.")]
    public string TipoVehiculo { get; set; } = string.Empty;

    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Anio { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor que cero.")]
    public int CapacidadPasajeros { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "La velocidad máxima debe ser mayor que cero.")]
    public decimal VelocidadMaximaKmh { get; set; }

    public bool Estado { get; set; } = true;

    public int? IdDispositivoGps { get; set; }
    public string? Imei { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Proveedor { get; set; }
    public bool GpsActivo { get; set; }

    public int? IdAsignacionActiva { get; set; }
    public int? IdRuta { get; set; }
    public string? CodigoRuta { get; set; }
    public string? NombreRuta { get; set; }
    public int? IdConductor { get; set; }
    public string? NombreConductor { get; set; }
    public string? EstadoAsignacion { get; set; }
}
