using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConductoresController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ConductoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Conductores.AsQueryable();
            if (!includeDeleted)
            {
                query = query.Where(c => c.Estado);
            }
            var list = await query.ToListAsync(cancellationToken);
            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar conductores.", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var conductor = await _context.Conductores.FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);
            if (conductor == null) return NotFound(new { message = "Conductor no encontrado." });
            return Ok(conductor);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener conductor.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Conductor model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.IdEmpresa <= 0)
            {
                var company = await _context.EmpresasTransporte.FirstOrDefaultAsync(cancellationToken);
                model.IdEmpresa = company?.IdEmpresa ?? 1;
            }

            model.FechaRegistro = DateTime.UtcNow;
            model.Estado = true;

            _context.Conductores.Add(model);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = model.IdConductor }, model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear conductor.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Conductor model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != model.IdConductor) return BadRequest(new { message = "ID de conductor no coincide." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Conductores.FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);
            if (existing == null) return NotFound(new { message = "Conductor no encontrado." });

            existing.Nombres = model.Nombres;
            existing.Apellidos = model.Apellidos;
            existing.Dni = model.Dni;
            existing.NumeroLicencia = model.NumeroLicencia;
            existing.CategoriaLicencia = model.CategoriaLicencia;
            existing.FechaVencimientoLicencia = model.FechaVencimientoLicencia;
            existing.Telefono = model.Telefono;
            existing.Estado = model.Estado;

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(existing);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar conductor.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var conductor = await _context.Conductores.FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);
            if (conductor == null) return NotFound(new { message = "Conductor no encontrado." });

            // Soft Delete
            conductor.Estado = false;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Conductor eliminado (desactivado) con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar conductor.", details = ex.Message });
        }
    }
}
