using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Telemetry;

public class TelemetryDto
{
    [Required(ErrorMessage = "El IMEI del dispositivo es requerido.")]
    public string Imei { get; set; } = string.Empty;

    [Required]
    public double Latitud { get; set; }

    [Required]
    public double Longitud { get; set; }

    [Required]
    public double VelocidadKmh { get; set; }

    public double? RumboGrados { get; set; }
    
    public double? PrecisionMetros { get; set; }

    public string? EventoForzado { get; set; } // "NORMAL", "VELOCIDAD", "DESVIO", "PANICO"
}
