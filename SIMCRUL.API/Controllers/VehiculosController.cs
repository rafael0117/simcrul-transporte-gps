using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var query = _context.Vehiculos.AsQueryable();
            if (!includeDeleted)
            {
                query = query.Where(v => v.Estado);
            }
            var list = await query.ToListAsync(cancellationToken);
            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar vehículos.", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
            if (vehiculo == null) return NotFound(new { message = "Vehículo no encontrado." });
            return Ok(vehiculo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener vehículo.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Vehiculo model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Assign default company if not specified
            if (model.IdEmpresa <= 0)
            {
                var company = await _context.EmpresasTransporte.FirstOrDefaultAsync(cancellationToken);
                model.IdEmpresa = company?.IdEmpresa ?? 1;
            }

            model.FechaRegistro = DateTime.UtcNow;
            model.Estado = true;

            _context.Vehiculos.Add(model);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = model.IdVehiculo }, model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear vehículo.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Vehiculo model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != model.IdVehiculo) return BadRequest(new { message = "ID de vehículo no coincide." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
            if (existing == null) return NotFound(new { message = "Vehículo no encontrado." });

            existing.Placa = model.Placa;
            existing.CodigoInterno = model.CodigoInterno;
            existing.TipoVehiculo = model.TipoVehiculo;
            existing.Marca = model.Marca;
            existing.Modelo = model.Modelo;
            existing.Anio = model.Anio;
            existing.CapacidadPasajeros = model.CapacidadPasajeros;
            existing.VelocidadMaximaKmh = model.VelocidadMaximaKmh;
            existing.Estado = model.Estado;

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(existing);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar vehículo.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
            if (vehiculo == null) return NotFound(new { message = "Vehículo no encontrado." });

            // Soft Delete
            vehiculo.Estado = false;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Vehículo eliminado (desactivado) con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar vehículo.", details = ex.Message });
        }
    }
}
