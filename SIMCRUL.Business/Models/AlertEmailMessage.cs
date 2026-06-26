namespace SIMCRUL.Business.Models;

public class AlertEmailMessage
{
    public IReadOnlyCollection<string> Recipients { get; set; } = Array.Empty<string>();
    public string AlertType { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime AlertDateUtc { get; set; }
}
