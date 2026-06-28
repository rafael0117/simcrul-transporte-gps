using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIMCRUL.Business.Interfaces;

namespace SIMCRUL.API.Services;

public sealed class SimulationStartupService : IHostedService
{
    private readonly IFleetSimulationService _fleetSimulationService;
    private readonly ILogger<SimulationStartupService> _logger;

    public SimulationStartupService(
        IFleetSimulationService fleetSimulationService,
        ILogger<SimulationStartupService> logger)
    {
        _fleetSimulationService = fleetSimulationService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _fleetSimulationService.StartAsync(cancellationToken: cancellationToken);
            _logger.LogInformation(
                "Simulacion GPS iniciada automaticamente con {VehiculosSimulados} vehiculos en {RutasSimuladas} rutas.",
                status.VehiculosSimulados,
                status.RutasSimuladas);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "La simulacion GPS no pudo iniciarse automaticamente al arrancar la API.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error inesperado al iniciar automaticamente la simulacion GPS.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fleetSimulationService.StopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ignored during graceful shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error al detener la simulacion GPS durante el apagado.");
        }
    }
}
