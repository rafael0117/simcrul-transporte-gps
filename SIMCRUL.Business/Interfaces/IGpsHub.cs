using SIMCRUL.Entity;

namespace SIMCRUL.Business.Interfaces;

public interface IGpsHub
{
    Task SendTelemetryUpdate(object telemetry);
    Task SendAlertNotification(object alert);
}
