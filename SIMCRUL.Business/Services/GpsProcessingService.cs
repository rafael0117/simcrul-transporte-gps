using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Business.Hubs;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Telemetry;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.Business.Services;

public class GpsProcessingService : IGpsProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GpsHub> _hubContext;

    public GpsProcessingService(ApplicationDbContext context, IHubContext<GpsHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task ProcessTelemetryAsync(TelemetryDto telemetryDto, CancellationToken cancellationToken = default)
    {
        // 1. Fetch GPS Device
        var device = await _context.DispositivosGps
            .FirstOrDefaultAsync(d => d.Imei == telemetryDto.Imei && d.Estado, cancellationToken);

        if (device == null)
        {
            throw new KeyNotFoundException($"No se encontró un dispositivo GPS activo con el IMEI: {telemetryDto.Imei}");
        }

        if (device.IdVehiculo == null)
        {
            throw new InvalidOperationException($"El dispositivo GPS con IMEI {telemetryDto.Imei} no está asignado a ningún vehículo.");
        }

        // 2. Fetch Vehicle
        var vehicle = await _context.Vehiculos
            .FirstOrDefaultAsync(v => v.IdVehiculo == device.IdVehiculo.Value && v.Estado, cancellationToken);

        if (vehicle == null)
        {
            throw new KeyNotFoundException($"No se encontró el vehículo activo asignado al dispositivo GPS.");
        }

        // Update last sync on GPS Device
        device.UltimaSincronizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Find active assignment
        var assignment = await _context.AsignacionesOperacion
            .Include(ao => ao.Ruta)
            .Include(ao => ao.Conductor)
            .FirstOrDefaultAsync(ao => ao.IdVehiculo == vehicle.IdVehiculo && ao.Estado == "ACTIVA", cancellationToken);

        // If no active, search for a scheduled one for today and activate it
        if (assignment == null)
        {
            assignment = await _context.AsignacionesOperacion
                .Include(ao => ao.Ruta)
                .Include(ao => ao.Conductor)
                .FirstOrDefaultAsync(ao => ao.IdVehiculo == vehicle.IdVehiculo && ao.Estado == "PROGRAMADA", cancellationToken);

            if (assignment != null)
            {
                assignment.Estado = "ACTIVA";
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        if (assignment == null)
        {
            // No assignment found, we cannot track route. We can log telemetry but won't check route deviation.
            // Let's create a temporary dummy assignment or throw
            throw new InvalidOperationException($"El vehículo con placa {vehicle.Placa} no tiene una asignación de ruta activa o programada.");
        }

        // 4. Find active Trip (Viaje)
        var trip = await _context.Viajes
            .FirstOrDefaultAsync(v => v.IdAsignacion == assignment.IdAsignacion && v.Estado == "EN_PROGRESO", cancellationToken);

        if (trip == null)
        {
            trip = new Viaje
            {
                IdAsignacion = assignment.IdAsignacion,
                FechaInicioReal = DateTime.UtcNow,
                Estado = "EN_PROGRESO",
                LatitudInicio = (decimal)telemetryDto.Latitud,
                LongitudInicio = (decimal)telemetryDto.Longitud,
                ScoreConduccion = 100
            };
            _context.Viajes.Add(trip);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 5. Save GPS reading
        var reading = new GpsLectura
        {
            IdViaje = trip.IdViaje,
            IdVehiculo = vehicle.IdVehiculo,
            IdDispositivo = device.IdDispositivo,
            FechaGps = DateTime.UtcNow,
            Latitud = (decimal)telemetryDto.Latitud,
            Longitud = (decimal)telemetryDto.Longitud,
            VelocidadKmh = (decimal)telemetryDto.VelocidadKmh,
            RumboGrados = telemetryDto.RumboGrados.HasValue ? (decimal)telemetryDto.RumboGrados.Value : null,
            PrecisionMetros = telemetryDto.PrecisionMetros.HasValue ? (decimal)telemetryDto.PrecisionMetros.Value : null,
            OrigenDato = "SIMULADOR",
            Procesado = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.GpsLecturas.Add(reading);
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Risk Engine and Rule Evaluation
        var route = assignment.Ruta;
        bool hasAlert = false;
        Alerta? createdAlert = null;

        // A. Speed check
        bool isSpeeding = reading.VelocidadKmh > route.VelocidadMaximaKmh || telemetryDto.EventoForzado == "VELOCIDAD";
        if (isSpeeding)
        {
            var tipoVelocidad = await GetOrCreateAlertKindAsync(AlertTypes.Velocidad, "Exceso de Velocidad", "El vehículo superó la velocidad máxima permitida para la ruta.", 3, cancellationToken);
            
            createdAlert = new Alerta
            {
                IdTipoAlerta = tipoVelocidad.IdTipoAlerta,
                IdViaje = trip.IdViaje,
                IdVehiculo = vehicle.IdVehiculo,
                IdConductor = assignment.IdConductor,
                IdLecturaGps = reading.IdLectura,
                FechaAlerta = DateTime.UtcNow,
                Descripcion = $"Exceso de velocidad: {reading.VelocidadKmh} Km/h (Límite: {route.VelocidadMaximaKmh} Km/h)",
                ValorDetectado = reading.VelocidadKmh,
                ValorPermitido = route.VelocidadMaximaKmh,
                Latitud = reading.Latitud,
                Longitud = reading.Longitud,
                Estado = "PENDIENTE"
            };
            
            _context.Alertas.Add(createdAlert);
            trip.ScoreConduccion = Math.Max(0, trip.ScoreConduccion - 5);
            hasAlert = true;
        }

        // B. Deviation check
        bool isDeviated = false;
        if (telemetryDto.EventoForzado == "DESVIO")
        {
            isDeviated = true;
        }
        else
        {
            // Retrieve route control points
            var controlPoints = await _context.RutaPuntosControl
                .Where(rpc => rpc.IdRuta == route.IdRuta)
                .OrderBy(rpc => rpc.Orden)
                .ToListAsync(cancellationToken);

            if (controlPoints.Count > 0)
            {
                // Calculate distance from current coordinate to closest control point
                double minDistance = double.MaxValue;
                int closestTolerance = 150; // default 150m

                foreach (var cp in controlPoints)
                {
                    double distance = CalculateDistanceMeters(
                        (double)reading.Latitud, 
                        (double)reading.Longitud, 
                        (double)cp.Latitud, 
                        (double)cp.Longitud
                    );

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestTolerance = (int)cp.RadioToleranciaMetros;
                    }
                }

                // If nearest point is further than the tolerance radius, it's out of route
                if (minDistance > closestTolerance)
                {
                    isDeviated = true;
                }
            }
        }

        if (isDeviated && !isSpeeding) // Don't stack speed + deviation alerts in one payload unless separate
        {
            var tipoDesvio = await GetOrCreateAlertKindAsync(AlertTypes.DesvioRuta, "Desvío de Ruta", "El vehículo se salió del radio de tolerancia de la ruta autorizada.", 4, cancellationToken);
            
            createdAlert = new Alerta
            {
                IdTipoAlerta = tipoDesvio.IdTipoAlerta,
                IdViaje = trip.IdViaje,
                IdVehiculo = vehicle.IdVehiculo,
                IdConductor = assignment.IdConductor,
                IdLecturaGps = reading.IdLectura,
                FechaAlerta = DateTime.UtcNow,
                Descripcion = $"Vehículo fuera de la ruta permitida.",
                ValorDetectado = null,
                ValorPermitido = null,
                Latitud = reading.Latitud,
                Longitud = reading.Longitud,
                Estado = "PENDIENTE"
            };
            
            _context.Alertas.Add(createdAlert);
            trip.ScoreConduccion = Math.Max(0, trip.ScoreConduccion - 10);
            hasAlert = true;
        }

        // C. Panic button event
        if (telemetryDto.EventoForzado == "PANICO")
        {
            var tipoPanico = await GetOrCreateAlertKindAsync(AlertTypes.Panico, "Botón de Pánico", "El conductor activó el botón de auxilio de emergencia.", 5, cancellationToken);
            
            createdAlert = new Alerta
            {
                IdTipoAlerta = tipoPanico.IdTipoAlerta,
                IdViaje = trip.IdViaje,
                IdVehiculo = vehicle.IdVehiculo,
                IdConductor = assignment.IdConductor,
                IdLecturaGps = reading.IdLectura,
                FechaAlerta = DateTime.UtcNow,
                Descripcion = $"ALERTA DE EMERGENCIA: ¡BOTÓN DE PÁNICO ACTIVADO!",
                ValorDetectado = null,
                ValorPermitido = null,
                Latitud = reading.Latitud,
                Longitud = reading.Longitud,
                Estado = "PENDIENTE"
            };
            
            _context.Alertas.Add(createdAlert);
            trip.ScoreConduccion = Math.Max(0, trip.ScoreConduccion - 20);
            hasAlert = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 7. SignalR Real-Time Broadcasting
        var telemetryUpdate = new
        {
            idVehiculo = vehicle.IdVehiculo,
            placa = vehicle.Placa,
            codigoInterno = vehicle.CodigoInterno,
            idViaje = trip.IdViaje,
            idRuta = route.IdRuta,
            codigoRuta = route.CodigoRuta,
            nombreRuta = route.NombreRuta,
            conductor = $"{assignment.Conductor.Nombres} {assignment.Conductor.Apellidos}",
            latitud = reading.Latitud,
            longitud = reading.Longitud,
            velocidadKmh = reading.VelocidadKmh,
            fechaGps = reading.FechaGps.ToString("yyyy-MM-dd HH:mm:ss"),
            scoreConduccion = trip.ScoreConduccion,
            alertaActiva = hasAlert,
            tipoAlerta = createdAlert?.Descripcion
        };

        // Broadcast current location to all connected dashboard users
        await _hubContext.Clients.All.SendAsync("SendTelemetryUpdate", telemetryUpdate, cancellationToken);

        if (hasAlert && createdAlert != null)
        {
            var alertNotify = new
            {
                idAlerta = createdAlert.IdAlerta,
                tipo = createdAlert.TipoAlerta.Nombre,
                severidad = createdAlert.TipoAlerta.NivelSeveridad,
                placa = vehicle.Placa,
                conductor = $"{assignment.Conductor.Nombres} {assignment.Conductor.Apellidos}",
                fecha = createdAlert.FechaAlerta.ToString("yyyy-MM-dd HH:mm:ss"),
                descripcion = createdAlert.Descripcion,
                latitud = createdAlert.Latitud,
                longitud = createdAlert.Longitud
            };

            // Broadcast alert detail to all dashboard users
            await _hubContext.Clients.All.SendAsync("SendAlertNotification", alertNotify, cancellationToken);
        }
    }

    private async Task<TipoAlerta> GetOrCreateAlertKindAsync(string codigo, string nombre, string descripcion, int severidad, CancellationToken cancellationToken)
    {
        var tipo = await _context.TiposAlerta.FirstOrDefaultAsync(ta => ta.Codigo == codigo, cancellationToken);
        if (tipo == null)
        {
            tipo = new TipoAlerta
            {
                Codigo = codigo,
                Nombre = nombre,
                Descripcion = descripcion,
                NivelSeveridad = severidad,
                Activa = true
            };
            _context.TiposAlerta.Add(tipo);
            await _context.SaveChangesAsync(cancellationToken);
        }
        return tipo;
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle)
    {
        return (Math.PI / 180) * angle;
    }
}
