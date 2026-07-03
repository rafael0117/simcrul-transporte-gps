using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Vehicles;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiculosController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public VehiculosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Vehiculos.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(v => v.Estado);
        }

        var vehicles = await query
            .OrderBy(v => v.CodigoInterno)
            .ToListAsync(cancellationToken);

        return Ok(vehicles.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
        if (vehicle == null)
        {
            return NotFound(new { message = "Vehiculo no encontrado." });
        }

        return Ok(MapToDto(vehicle));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleManagementDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var normalizedPlate = model.Placa.Trim().ToUpperInvariant();
        var normalizedCode = model.CodigoInterno.Trim().ToUpperInvariant();

        if (await _context.Vehiculos.AnyAsync(v => v.Placa == normalizedPlate, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe un vehiculo con esa placa." });
        }

        if (await _context.Vehiculos.AnyAsync(v => v.CodigoInterno == normalizedCode, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe un vehiculo con ese codigo interno." });
        }

        var company = await _context.EmpresasTransporte.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync(cancellationToken);
        if (company == null)
        {
            return BadRequest(new { message = "No existe una empresa activa para asociar el vehiculo." });
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
            KilometrajeActual = model.KilometrajeActual,
            EstadoOperativo = model.EstadoOperativo.Trim().ToUpperInvariant(),
            FechaUltimaInspeccion = model.FechaUltimaInspeccion,
            FechaUltimoMantenimiento = model.FechaUltimoMantenimiento,
            ObservacionesMantenimiento = NormalizeOptional(model.ObservacionesMantenimiento),
            Estado = model.Estado,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Vehiculos.Add(vehicle);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = vehicle.IdVehiculo }, MapToDto(vehicle));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleManagementDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        if (id != model.IdVehiculo)
        {
            return BadRequest(new { message = "El identificador del vehiculo no coincide." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
        if (vehicle == null)
        {
            return NotFound(new { message = "Vehiculo no encontrado." });
        }

        var normalizedPlate = model.Placa.Trim().ToUpperInvariant();
        var normalizedCode = model.CodigoInterno.Trim().ToUpperInvariant();

        if (await _context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.Placa == normalizedPlate, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe otro vehiculo con esa placa." });
        }

        if (await _context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.CodigoInterno == normalizedCode, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe otro vehiculo con ese codigo interno." });
        }

        vehicle.Placa = normalizedPlate;
        vehicle.CodigoInterno = normalizedCode;
        vehicle.TipoVehiculo = model.TipoVehiculo.Trim().ToUpperInvariant();
        vehicle.Marca = NormalizeOptional(model.Marca);
        vehicle.Modelo = NormalizeOptional(model.Modelo);
        vehicle.Anio = model.Anio;
        vehicle.CapacidadPasajeros = model.CapacidadPasajeros;
        vehicle.VelocidadMaximaKmh = model.VelocidadMaximaKmh;
        vehicle.KilometrajeActual = model.KilometrajeActual;
        vehicle.EstadoOperativo = model.EstadoOperativo.Trim().ToUpperInvariant();
        vehicle.FechaUltimaInspeccion = model.FechaUltimaInspeccion;
        vehicle.FechaUltimoMantenimiento = model.FechaUltimoMantenimiento;
        vehicle.ObservacionesMantenimiento = NormalizeOptional(model.ObservacionesMantenimiento);
        vehicle.Estado = model.Estado;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(vehicle));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
        if (vehicle == null)
        {
            return NotFound(new { message = "Vehiculo no encontrado." });
        }

        vehicle.Estado = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Vehiculo desactivado correctamente." });
    }

    private static VehicleManagementDto MapToDto(Vehiculo vehicle)
    {
        return new VehicleManagementDto
        {
            IdVehiculo = vehicle.IdVehiculo,
            Placa = vehicle.Placa,
            CodigoInterno = vehicle.CodigoInterno,
            TipoVehiculo = vehicle.TipoVehiculo,
            Marca = vehicle.Marca,
            Modelo = vehicle.Modelo,
            Anio = vehicle.Anio,
            CapacidadPasajeros = vehicle.CapacidadPasajeros,
            VelocidadMaximaKmh = vehicle.VelocidadMaximaKmh,
            KilometrajeActual = vehicle.KilometrajeActual,
            EstadoOperativo = vehicle.EstadoOperativo,
            FechaUltimaInspeccion = vehicle.FechaUltimaInspeccion,
            FechaUltimoMantenimiento = vehicle.FechaUltimoMantenimiento,
            ObservacionesMantenimiento = vehicle.ObservacionesMantenimiento,
            Estado = vehicle.Estado
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
