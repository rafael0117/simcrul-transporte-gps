using SIMCRUL.Common.DTOs.Simulation;

namespace SIMCRUL.Business.Interfaces;

public interface IFleetSimulationService
{
    Task<SimulationStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<SimulationStatusDto> StartAsync(int intervalSeconds = 3, CancellationToken cancellationToken = default);
    Task<SimulationStatusDto> StopAsync(CancellationToken cancellationToken = default);
}
