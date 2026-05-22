using SIMCRUL.Common.DTOs.Telemetry;

namespace SIMCRUL.Business.Interfaces;

public interface IGpsProcessingService
{
    Task ProcessTelemetryAsync(TelemetryDto telemetryDto, CancellationToken cancellationToken = default);
}
