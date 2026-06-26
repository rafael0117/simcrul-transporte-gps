using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.DTOs.Vehicles;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiculosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VehiculosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Vehiculos
                .Include(v => v.DispositivosGps)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Ruta)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Conductor)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(v => v.Estado);
            }

            var list = await query
                .OrderBy(v => v.CodigoInterno)
                .ToListAsync(cancellationToken);

            return Ok(list.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar vehiculos.", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.DispositivosGps)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Ruta)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Conductor)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);

            if (vehiculo == null)
            {
                return NotFound(new { message = "Vehiculo no encontrado." });
            }

            return Ok(MapToDto(vehiculo));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener vehiculo.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var normalizedPlate = model.Placa.Trim().ToUpperInvariant();
            var normalizedCode = model.CodigoInterno.Trim().ToUpperInvariant();
            var normalizedImei = NormalizeOptional(model.Imei);

            if (await _context.Vehiculos.AnyAsync(v => v.Placa == normalizedPlate, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe un vehiculo con esa placa." });
            }

            if (await _context.Vehiculos.AnyAsync(v => v.CodigoInterno == normalizedCode, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe un vehiculo con ese codigo interno." });
            }

            if (!string.IsNullOrWhiteSpace(normalizedImei) &&
                await _context.DispositivosGps.AnyAsync(d => d.Imei == normalizedImei, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe un dispositivo GPS con ese IMEI." });
            }

            var company = await _context.EmpresasTransporte.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                return BadRequest(new { message = "No existe una empresa de transporte registrada para asociar el vehiculo." });
            }

            var vehicle = new Vehiculo
            {
                IdEmpresa = company.IdEmpresa,
                Placa = normalizedPlate,
                CodigoInterno = normalizedCode,
                TipoVehiculo = model.TipoVehiculo.Trim().ToUpperInvariant(),
                Marca = NormalizeOptional(model.Marca),
                Modelo = NormalizeOptional(model.Modelo),
                Anio = model.Anio,
                CapacidadPasajeros = model.CapacidadPasajeros,
                VelocidadMaximaKmh = model.VelocidadMaximaKmh,
                Estado = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Vehiculos.Add(vehicle);
            await _context.SaveChangesAsync(cancellationToken);

            await SyncGpsDeviceAsync(vehicle.IdVehiculo, model, null, cancellationToken);
            await SyncRouteAssignmentAsync(vehicle.IdVehiculo, model, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var createdVehicle = await LoadVehicleGraphAsync(vehicle.IdVehiculo, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = createdVehicle.IdVehiculo }, MapToDto(createdVehicle));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear vehiculo.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != model.IdVehiculo)
            {
                return BadRequest(new { message = "ID de vehiculo no coincide." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _context.Vehiculos
                .Include(v => v.DispositivosGps)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Ruta)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Conductor)
                .FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);

            if (existing == null)
            {
                return NotFound(new { message = "Vehiculo no encontrado." });
            }

            var normalizedPlate = model.Placa.Trim().ToUpperInvariant();
            var normalizedCode = model.CodigoInterno.Trim().ToUpperInvariant();
            var normalizedImei = NormalizeOptional(model.Imei);

            if (await _context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.Placa == normalizedPlate, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe otro vehiculo con esa placa." });
            }

            if (await _context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.CodigoInterno == normalizedCode, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe otro vehiculo con ese codigo interno." });
            }

            if (!string.IsNullOrWhiteSpace(normalizedImei) &&
                await _context.DispositivosGps.AnyAsync(d => d.IdVehiculo != id && d.Imei == normalizedImei, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe otro dispositivo GPS con ese IMEI." });
            }

            existing.Placa = normalizedPlate;
            existing.CodigoInterno = normalizedCode;
            existing.TipoVehiculo = model.TipoVehiculo.Trim().ToUpperInvariant();
            existing.Marca = NormalizeOptional(model.Marca);
            existing.Modelo = NormalizeOptional(model.Modelo);
            existing.Anio = model.Anio;
            existing.CapacidadPasajeros = model.CapacidadPasajeros;
            existing.VelocidadMaximaKmh = model.VelocidadMaximaKmh;
            existing.Estado = model.Estado;

            var device = existing.DispositivosGps
                .OrderByDescending(d => d.Estado)
                .ThenByDescending(d => d.IdDispositivo)
                .FirstOrDefault();

            await SyncGpsDeviceAsync(existing.IdVehiculo, model, device, cancellationToken);
            await SyncRouteAssignmentAsync(existing.IdVehiculo, model, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var updatedVehicle = await LoadVehicleGraphAsync(existing.IdVehiculo, cancellationToken);
            return Ok(MapToDto(updatedVehicle));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar vehiculo.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
            if (vehiculo == null)
            {
                return NotFound(new { message = "Vehiculo no encontrado." });
            }

            vehiculo.Estado = false;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Vehiculo desactivado con exito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al desactivar vehiculo.", details = ex.Message });
        }
    }

    private async Task<Vehiculo> LoadVehicleGraphAsync(int vehicleId, CancellationToken cancellationToken)
    {
        return await _context.Vehiculos
            .Include(v => v.DispositivosGps)
            .Include(v => v.Asignaciones)
                .ThenInclude(a => a.Ruta)
            .Include(v => v.Asignaciones)
                .ThenInclude(a => a.Conductor)
            .FirstAsync(v => v.IdVehiculo == vehicleId, cancellationToken);
    }

    private static VehicleManagementDto MapToDto(Vehiculo vehiculo)
    {
        var device = vehiculo.DispositivosGps
            .OrderByDescending(d => d.Estado)
            .ThenByDescending(d => d.IdDispositivo)
            .FirstOrDefault();

        var activeAssignment = vehiculo.Asignaciones
            .Where(a => a.Estado == "ACTIVA" || a.Estado == "PROGRAMADA")
            .OrderByDescending(a => a.Estado == "ACTIVA")
            .ThenByDescending(a => a.FechaInicioProgramada)
            .FirstOrDefault();

        return new VehicleManagementDto
        {
            IdVehiculo = vehiculo.IdVehiculo,
            Placa = vehiculo.Placa,
            CodigoInterno = vehiculo.CodigoInterno,
            TipoVehiculo = vehiculo.TipoVehiculo,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            CapacidadPasajeros = vehiculo.CapacidadPasajeros,
            VelocidadMaximaKmh = vehiculo.VelocidadMaximaKmh,
            Estado = vehiculo.Estado,
            IdDispositivoGps = device?.IdDispositivo,
            Imei = device?.Imei,
            NumeroSerie = device?.NumeroSerie,
            Proveedor = device?.Proveedor,
            GpsActivo = device?.Estado ?? false,
            IdAsignacionActiva = activeAssignment?.IdAsignacion,
            IdRuta = activeAssignment?.IdRuta,
            CodigoRuta = activeAssignment?.Ruta?.CodigoRuta,
            NombreRuta = activeAssignment?.Ruta?.NombreRuta,
            IdConductor = activeAssignment?.IdConductor,
            NombreConductor = activeAssignment?.Conductor == null
                ? null
                : $"{activeAssignment.Conductor.Nombres} {activeAssignment.Conductor.Apellidos}".Trim(),
            EstadoAsignacion = activeAssignment?.Estado
        };
    }

    private async Task SyncRouteAssignmentAsync(int vehicleId, VehicleManagementDto model, CancellationToken cancellationToken)
    {
        var currentAssignments = await _context.AsignacionesOperacion
            .Include(a => a.Viajes)
            .Where(a => a.IdVehiculo == vehicleId && (a.Estado == "ACTIVA" || a.Estado == "PROGRAMADA"))
            .OrderByDescending(a => a.FechaInicioProgramada)
            .ToListAsync(cancellationToken);

        if (!model.IdRuta.HasValue)
        {
            foreach (var assignment in currentAssignments)
            {
                CancelAssignment(assignment);
            }

            return;
        }

        if (!model.IdConductor.HasValue)
        {
            throw new InvalidOperationException("Debe seleccionar un conductor para asignar la ruta al vehiculo.");
        }

        var routeExists = await _context.Rutas
            .AnyAsync(r => r.IdRuta == model.IdRuta.Value && r.Activa, cancellationToken);

        if (!routeExists)
        {
            throw new InvalidOperationException("La ruta seleccionada no existe o esta inactiva.");
        }

        var driverExists = await _context.Conductores
            .AnyAsync(c => c.IdConductor == model.IdConductor.Value && c.Estado, cancellationToken);

        if (!driverExists)
        {
            throw new InvalidOperationException("El conductor seleccionado no existe o esta inactivo.");
        }

        var driverBusyInAnotherVehicle = await _context.AsignacionesOperacion
            .AnyAsync(a =>
                a.IdConductor == model.IdConductor.Value &&
                a.IdVehiculo != vehicleId &&
                (a.Estado == "ACTIVA" || a.Estado == "PROGRAMADA"),
                cancellationToken);

        if (driverBusyInAnotherVehicle)
        {
            throw new InvalidOperationException("El conductor seleccionado ya tiene otra asignacion activa o programada.");
        }

        var matchingAssignment = currentAssignments.FirstOrDefault(a =>
            a.IdRuta == model.IdRuta.Value &&
            a.IdConductor == model.IdConductor.Value);

        foreach (var assignment in currentAssignments.Where(a => matchingAssignment == null || a.IdAsignacion != matchingAssignment.IdAsignacion))
        {
            CancelAssignment(assignment);
        }

        if (matchingAssignment != null)
        {
            matchingAssignment.Estado = "PROGRAMADA";
            matchingAssignment.FechaInicioProgramada = DateTime.UtcNow;
            matchingAssignment.FechaFinProgramada = DateTime.UtcNow.AddHours(12);
            matchingAssignment.Turno = ResolveTurn(DateTime.UtcNow);
            matchingAssignment.Observaciones = "Asignacion actualizada desde mantenimiento de vehiculos.";
            return;
        }

        _context.AsignacionesOperacion.Add(new AsignacionOperacion
        {
            IdRuta = model.IdRuta.Value,
            IdVehiculo = vehicleId,
            IdConductor = model.IdConductor.Value,
            FechaInicioProgramada = DateTime.UtcNow,
            FechaFinProgramada = DateTime.UtcNow.AddHours(12),
            Turno = ResolveTurn(DateTime.UtcNow),
            Estado = "PROGRAMADA",
            Observaciones = "Asignacion creada desde mantenimiento de vehiculos."
        });
    }

    private static void CancelAssignment(AsignacionOperacion assignment)
    {
        assignment.Estado = "CANCELADA";
        assignment.FechaFinProgramada = DateTime.UtcNow;
        assignment.Observaciones = "Asignacion retirada desde mantenimiento de vehiculos.";

        foreach (var trip in assignment.Viajes.Where(v => v.Estado == "EN_PROGRESO"))
        {
            trip.Estado = "ABORTADO";
            trip.FechaFinReal = DateTime.UtcNow;
        }
    }

    private static string ResolveTurn(DateTime currentTimeUtc)
    {
        var hour = currentTimeUtc.Hour;
        if (hour < 12)
        {
            return "MANANA";
        }

        if (hour < 18)
        {
            return "TARDE";
        }

        return "NOCHE";
    }

    private async Task SyncGpsDeviceAsync(
        int vehicleId,
        VehicleManagementDto model,
        DispositivoGps? existingDevice,
        CancellationToken cancellationToken)
    {
        var normalizedImei = NormalizeOptional(model.Imei);
        if (string.IsNullOrWhiteSpace(normalizedImei))
        {
            return;
        }

        var numeroSerie = NormalizeOptional(model.NumeroSerie) ?? normalizedImei;
        var proveedor = NormalizeOptional(model.Proveedor) ?? "TELEFONO_MOVIL";

        var gpsDevice = existingDevice;
        if (gpsDevice == null)
        {
            gpsDevice = await _context.DispositivosGps
                .FirstOrDefaultAsync(d => d.IdVehiculo == vehicleId, cancellationToken);
        }

        if (gpsDevice == null)
        {
            _context.DispositivosGps.Add(new DispositivoGps
            {
                IdVehiculo = vehicleId,
                Imei = normalizedImei,
                NumeroSerie = numeroSerie,
                Proveedor = proveedor,
                FechaInstalacion = DateTime.UtcNow,
                Estado = true
            });

            return;
        }

        gpsDevice.IdVehiculo = vehicleId;
        gpsDevice.Imei = normalizedImei;
        gpsDevice.NumeroSerie = numeroSerie;
        gpsDevice.Proveedor = proveedor;
        gpsDevice.Estado = true;
        if (gpsDevice.FechaInstalacion == default)
        {
            gpsDevice.FechaInstalacion = DateTime.UtcNow;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
