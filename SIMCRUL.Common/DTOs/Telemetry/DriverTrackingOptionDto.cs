namespace SIMCRUL.Common.DTOs.Telemetry;

public class DriverTrackingOptionDto
{
    public int IdAsignacion { get; set; }
    public int IdVehiculo { get; set; }
    public int IdConductor { get; set; }
    public int IdRuta { get; set; }
    public string EstadoAsignacion { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string CodigoVehiculo { get; set; } = string.Empty;
    public string Conductor { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Imei { get; set; } = string.Empty;
    public decimal VelocidadMaximaKmh { get; set; }
}
