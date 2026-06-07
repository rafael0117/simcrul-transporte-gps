namespace SIMCRUL.Common.DTOs.Simulation;

public class SimulationStatusDto
{
    public bool IsRunning { get; set; }
    public int IntervalSeconds { get; set; }
    public int VehiculosSimulados { get; set; }
    public int RutasSimuladas { get; set; }
    public DateTime? LastTelemetryAtUtc { get; set; }
    public List<RouteSimulationStatusDto> Routes { get; set; } = new();
}
