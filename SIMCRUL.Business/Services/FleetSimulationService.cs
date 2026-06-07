using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.DTOs.Simulation;
using SIMCRUL.Common.DTOs.Telemetry;
using SIMCRUL.Data.Context;

namespace SIMCRUL.Business.Services;

public class FleetSimulationService : IFleetSimulationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FleetSimulationService> _logger;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly object _stateLock = new();

    private CancellationTokenSource? _simulationCts;
    private Task? _simulationTask;
    private List<SimulatedVehicleState> _vehicles = new();
    private DateTime? _lastTelemetryAtUtc;
    private int _intervalSeconds = 3;

    public FleetSimulationService(IServiceScopeFactory scopeFactory, ILogger<FleetSimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task<SimulationStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            return Task.FromResult(BuildStatusUnsafe());
        }
    }

    public async Task<SimulationStatusDto> StartAsync(int intervalSeconds = 3, CancellationToken cancellationToken = default)
    {
        intervalSeconds = Math.Clamp(intervalSeconds, 1, 10);

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (_simulationTask is { IsCompleted: false })
            {
                lock (_stateLock)
                {
                    return BuildStatusUnsafe();
                }
            }

            var vehicles = await LoadFleetAsync(cancellationToken);
            if (vehicles.Count == 0)
            {
                throw new InvalidOperationException("No hay vehiculos asignados con GPS activo para iniciar la simulacion.");
            }

            lock (_stateLock)
            {
                _vehicles = vehicles;
                _intervalSeconds = intervalSeconds;
                _lastTelemetryAtUtc = null;
            }

            _simulationCts = new CancellationTokenSource();
            _simulationTask = RunSimulationLoopAsync(_simulationCts.Token);

            lock (_stateLock)
            {
                return BuildStatusUnsafe();
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<SimulationStatusDto> StopAsync(CancellationToken cancellationToken = default)
    {
        Task? simulationTaskToAwait = null;

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (_simulationCts != null)
            {
                _simulationCts.Cancel();
                simulationTaskToAwait = _simulationTask;
            }

            _simulationCts = null;
            _simulationTask = null;

            lock (_stateLock)
            {
                _vehicles = new List<SimulatedVehicleState>();
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }

        if (simulationTaskToAwait != null)
        {
            try
            {
                await simulationTaskToAwait;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the simulation.
            }
        }

        lock (_stateLock)
        {
            return BuildStatusUnsafe();
        }
    }

    private async Task<List<SimulatedVehicleState>> LoadFleetAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var assignments = await dbContext.AsignacionesOperacion
            .AsNoTracking()
            .Include(assignment => assignment.Ruta)
            .Include(assignment => assignment.Vehiculo)
            .Include(assignment => assignment.Conductor)
            .Where(assignment => assignment.Estado == "ACTIVA" &&
                                 assignment.Ruta.Activa &&
                                 assignment.Vehiculo.Estado &&
                                 assignment.Conductor.Estado)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return new List<SimulatedVehicleState>();
        }

        var routeIds = assignments.Select(assignment => assignment.IdRuta).Distinct().ToList();
        var vehicleIds = assignments.Select(assignment => assignment.IdVehiculo).Distinct().ToList();

        var routePointsByRoute = await dbContext.RutaPuntosControl
            .AsNoTracking()
            .Where(point => routeIds.Contains(point.IdRuta))
            .OrderBy(point => point.IdRuta)
            .ThenBy(point => point.Orden)
            .ToListAsync(cancellationToken);

        var devicesByVehicle = await dbContext.DispositivosGps
            .AsNoTracking()
            .Where(device => device.IdVehiculo.HasValue &&
                             vehicleIds.Contains(device.IdVehiculo.Value) &&
                             device.Estado)
            .GroupBy(device => device.IdVehiculo!.Value)
            .ToDictionaryAsync(group => group.Key, group => group.First(), cancellationToken);

        var vehicles = new List<SimulatedVehicleState>();
        var groupedPoints = routePointsByRoute
            .GroupBy(point => point.IdRuta)
            .ToDictionary(group => group.Key, group => group.ToList());

        var routeOffsets = new Dictionary<int, int>();

        foreach (var assignment in assignments)
        {
            if (!devicesByVehicle.TryGetValue(assignment.IdVehiculo, out var device))
            {
                continue;
            }

            if (!groupedPoints.TryGetValue(assignment.IdRuta, out var controlPoints) || controlPoints.Count < 2)
            {
                continue;
            }

            routeOffsets.TryGetValue(assignment.IdRuta, out var routeVehicleIndex);
            routeOffsets[assignment.IdRuta] = routeVehicleIndex + 1;

            var startIndex = (routeVehicleIndex * Math.Max(2, controlPoints.Count / 3)) % controlPoints.Count;

            vehicles.Add(new SimulatedVehicleState
            {
                Imei = device.Imei,
                RouteId = assignment.IdRuta,
                RouteCode = assignment.Ruta.CodigoRuta,
                RouteName = assignment.Ruta.NombreRuta,
                VehicleId = assignment.IdVehiculo,
                VehiclePlate = assignment.Vehiculo.Placa,
                CurrentPointIndex = startIndex,
                Points = controlPoints.Select(point => new SimulationCoordinate(
                    (double)point.Latitud,
                    (double)point.Longitud)).ToList()
            });
        }

        return vehicles;
    }

    private async Task RunSimulationLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));

        try
        {
            await SendBatchAsync(cancellationToken);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await SendBatchAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Simulacion GPS detenida.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "La simulacion GPS encontro un error no controlado.");
        }
    }

    private async Task SendBatchAsync(CancellationToken cancellationToken)
    {
        List<TelemetryDto> payloads;

        lock (_stateLock)
        {
            payloads = new List<TelemetryDto>(_vehicles.Count);

            foreach (var vehicle in _vehicles)
            {
                var point = vehicle.Points[vehicle.CurrentPointIndex];
                payloads.Add(new TelemetryDto
                {
                    Imei = vehicle.Imei,
                    Latitud = point.Latitude,
                    Longitud = point.Longitude,
                    VelocidadKmh = Random.Shared.Next(38, 56),
                    RumboGrados = Random.Shared.Next(0, 359),
                    PrecisionMetros = 4,
                    EventoForzado = "NORMAL"
                });

                vehicle.CurrentPointIndex = (vehicle.CurrentPointIndex + 1) % vehicle.Points.Count;
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var gpsProcessingService = scope.ServiceProvider.GetRequiredService<IGpsProcessingService>();

        foreach (var payload in payloads)
        {
            try
            {
                await gpsProcessingService.ProcessTelemetryAsync(payload, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "No se pudo procesar telemetria simulada para el IMEI {Imei}.", payload.Imei);
            }
        }

        lock (_stateLock)
        {
            _lastTelemetryAtUtc = DateTime.UtcNow;
        }
    }

    private SimulationStatusDto BuildStatusUnsafe()
    {
        var routeSummaries = _vehicles
            .GroupBy(vehicle => new { vehicle.RouteId, vehicle.RouteCode, vehicle.RouteName })
            .Select(group => new RouteSimulationStatusDto
            {
                IdRuta = group.Key.RouteId,
                CodigoRuta = group.Key.RouteCode,
                NombreRuta = group.Key.RouteName,
                VehiculosSimulados = group.Count()
            })
            .OrderBy(route => route.CodigoRuta)
            .ToList();

        return new SimulationStatusDto
        {
            IsRunning = _simulationTask is { IsCompleted: false },
            IntervalSeconds = _intervalSeconds,
            VehiculosSimulados = _vehicles.Count,
            RutasSimuladas = routeSummaries.Count,
            LastTelemetryAtUtc = _lastTelemetryAtUtc,
            Routes = routeSummaries
        };
    }

    private sealed class SimulatedVehicleState
    {
        public string Imei { get; set; } = string.Empty;
        public int RouteId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public int CurrentPointIndex { get; set; }
        public List<SimulationCoordinate> Points { get; set; } = new();
    }

    private sealed record SimulationCoordinate(double Latitude, double Longitude);
}
