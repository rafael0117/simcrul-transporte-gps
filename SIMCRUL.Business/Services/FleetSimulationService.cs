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
    private const string NormalEvent = "NORMAL";
    private const string SpeedEvent = "VELOCIDAD";
    private const string RouteDeviationEvent = "DESVIO";

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
                RouteSpeedLimitKmh = (double)assignment.Ruta.VelocidadMaximaKmh,
                CurrentPointIndex = startIndex,
                TicksUntilEvent = Random.Shared.Next(5, 11),
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
                var snapshot = BuildTelemetrySnapshot(vehicle);
                payloads.Add(new TelemetryDto
                {
                    Imei = vehicle.Imei,
                    Latitud = snapshot.Latitude,
                    Longitud = snapshot.Longitude,
                    VelocidadKmh = snapshot.SpeedKmh,
                    RumboGrados = Random.Shared.Next(0, 359),
                    PrecisionMetros = 4,
                    EventoForzado = snapshot.EventCode
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

    private static TelemetrySnapshot BuildTelemetrySnapshot(SimulatedVehicleState vehicle)
    {
        var currentPoint = vehicle.Points[vehicle.CurrentPointIndex];
        var nextPoint = vehicle.Points[(vehicle.CurrentPointIndex + 1) % vehicle.Points.Count];

        UpdateEventState(vehicle);

        var latitude = currentPoint.Latitude;
        var longitude = currentPoint.Longitude;
        var speedKmh = Random.Shared.Next(38, 56);
        var eventCode = NormalEvent;

        if (vehicle.ActiveEventCode == SpeedEvent)
        {
            var minimumSpeed = (int)Math.Ceiling(vehicle.RouteSpeedLimitKmh + 8);
            speedKmh = Math.Max(minimumSpeed, Random.Shared.Next(65, 92));
            eventCode = SpeedEvent;
        }
        else if (vehicle.ActiveEventCode == RouteDeviationEvent)
        {
            var deviatedPoint = OffsetFromRoute(
                currentPoint,
                nextPoint,
                vehicle.RouteDeviationOffsetMeters,
                vehicle.RouteDeviationSide);

            latitude = deviatedPoint.Latitude;
            longitude = deviatedPoint.Longitude;
            speedKmh = Math.Max(22, Random.Shared.Next(28, 45));
            eventCode = RouteDeviationEvent;
        }

        return new TelemetrySnapshot(latitude, longitude, speedKmh, eventCode);
    }

    private static void UpdateEventState(SimulatedVehicleState vehicle)
    {
        if (vehicle.ActiveEventTicksRemaining > 0)
        {
            vehicle.ActiveEventTicksRemaining--;
            if (vehicle.ActiveEventTicksRemaining == 0)
            {
                vehicle.ActiveEventCode = NormalEvent;
                vehicle.TicksUntilEvent = Random.Shared.Next(6, 12);
            }

            return;
        }

        if (vehicle.TicksUntilEvent > 0)
        {
            vehicle.TicksUntilEvent--;
            return;
        }

        var nextEventCode = Random.Shared.NextDouble() < 0.5 ? SpeedEvent : RouteDeviationEvent;
        vehicle.ActiveEventCode = nextEventCode;
        vehicle.ActiveEventTicksRemaining = nextEventCode == SpeedEvent
            ? Random.Shared.Next(2, 5)
            : Random.Shared.Next(3, 6);
        vehicle.TicksUntilEvent = Random.Shared.Next(8, 14);
        vehicle.RouteDeviationOffsetMeters = Random.Shared.Next(180, 320);
        vehicle.RouteDeviationSide = Random.Shared.Next(0, 2) == 0 ? -1 : 1;

        vehicle.ActiveEventTicksRemaining--;
        if (vehicle.ActiveEventTicksRemaining == 0)
        {
            vehicle.ActiveEventCode = NormalEvent;
        }
    }

    private static SimulationCoordinate OffsetFromRoute(
        SimulationCoordinate currentPoint,
        SimulationCoordinate nextPoint,
        double offsetMeters,
        int direction)
    {
        var deltaLongitude = nextPoint.Longitude - currentPoint.Longitude;
        var deltaLatitude = nextPoint.Latitude - currentPoint.Latitude;
        var vectorLength = Math.Sqrt((deltaLongitude * deltaLongitude) + (deltaLatitude * deltaLatitude));

        if (vectorLength < 0.000001d)
        {
            vectorLength = 1d;
            deltaLatitude = 1d;
            deltaLongitude = 0d;
        }

        var perpendicularLongitude = (-deltaLatitude / vectorLength) * direction;
        var perpendicularLatitude = (deltaLongitude / vectorLength) * direction;

        var metersPerDegreeLatitude = 111_320d;
        var metersPerDegreeLongitude = Math.Max(
            1d,
            111_320d * Math.Cos(currentPoint.Latitude * Math.PI / 180d));

        return new SimulationCoordinate(
            currentPoint.Latitude + ((perpendicularLatitude * offsetMeters) / metersPerDegreeLatitude),
            currentPoint.Longitude + ((perpendicularLongitude * offsetMeters) / metersPerDegreeLongitude));
    }

    private sealed class SimulatedVehicleState
    {
        public string Imei { get; set; } = string.Empty;
        public int RouteId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public double RouteSpeedLimitKmh { get; set; }
        public int CurrentPointIndex { get; set; }
        public int TicksUntilEvent { get; set; }
        public int ActiveEventTicksRemaining { get; set; }
        public string ActiveEventCode { get; set; } = NormalEvent;
        public double RouteDeviationOffsetMeters { get; set; }
        public int RouteDeviationSide { get; set; } = 1;
        public List<SimulationCoordinate> Points { get; set; } = new();
    }

    private sealed record TelemetrySnapshot(double Latitude, double Longitude, double SpeedKmh, string EventCode);
    private sealed record SimulationCoordinate(double Latitude, double Longitude);
}
