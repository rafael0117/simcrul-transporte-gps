using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RutasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RutasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Rutas.AsQueryable();
            if (!includeDeleted)
            {
                query = query.Where(r => r.Activa);
            }
            var list = await query.ToListAsync(cancellationToken);
            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar rutas.", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var ruta = await _context.Rutas
                .Include(r => r.RutaParaderos)
                .Include(r => r.RutaPuntosControl)
                .FirstOrDefaultAsync(r => r.IdRuta == id, cancellationToken);
            if (ruta == null) return NotFound(new { message = "Ruta no encontrada." });
            return Ok(ruta);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener ruta.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Ruta model, CancellationToken cancellationToken = default)
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
            model.Activa = true;

            _context.Rutas.Add(model);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = model.IdRuta }, model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear ruta.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Ruta model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != model.IdRuta) return BadRequest(new { message = "ID de ruta no coincide." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Rutas.FirstOrDefaultAsync(r => r.IdRuta == id, cancellationToken);
            if (existing == null) return NotFound(new { message = "Ruta no encontrada." });

            existing.CodigoRuta = model.CodigoRuta;
            existing.NombreRuta = model.NombreRuta;
            existing.Origen = model.Origen;
            existing.Destino = model.Destino;
            existing.DistanciaKm = model.DistanciaKm;
            existing.TiempoEstimadoMin = model.TiempoEstimadoMin;
            existing.VelocidadMaximaKmh = model.VelocidadMaximaKmh;
            existing.Activa = model.Activa;

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(existing);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar ruta.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var ruta = await _context.Rutas.FirstOrDefaultAsync(r => r.IdRuta == id, cancellationToken);
            if (ruta == null) return NotFound(new { message = "Ruta no encontrada." });

            // Soft Delete
            ruta.Activa = false;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Ruta eliminada (desactivada) con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar ruta.", details = ex.Message });
        }
    }
}
