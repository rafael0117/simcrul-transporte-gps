using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Vehicles;

public class VehicleManagementDto
{
    public int IdVehiculo { get; set; }

    [Required(ErrorMessage = "La placa es obligatoria.")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "El codigo interno es obligatorio.")]
    public string CodigoInterno { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de vehiculo es obligatorio.")]
    public string TipoVehiculo { get; set; } = string.Empty;

    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Anio { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor que cero.")]
    public int CapacidadPasajeros { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "La velocidad maxima debe ser mayor que cero.")]
    public decimal VelocidadMaximaKmh { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El kilometraje no puede ser negativo.")]
    public decimal KilometrajeActual { get; set; }

    [Required(ErrorMessage = "El estado operativo es obligatorio.")]
    public string EstadoOperativo { get; set; } = "OPERATIVO";

    public DateTime? FechaUltimaInspeccion { get; set; }
    public DateTime? FechaUltimoMantenimiento { get; set; }
    public string? ObservacionesMantenimiento { get; set; }
    public bool Estado { get; set; } = true;
}
