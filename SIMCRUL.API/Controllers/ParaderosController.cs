using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.DTOs.Paraderos;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParaderosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ParaderosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Paraderos
                .Include(p => p.RutaParaderos)
                    .ThenInclude(rp => rp.Ruta)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(p => p.Activo);
            }

            var list = await query
                .OrderBy(p => p.Nombre)
                .ToListAsync(cancellationToken);

            return Ok(list.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar paraderos.", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var paradero = await _context.Paraderos
                .Include(p => p.RutaParaderos)
                    .ThenInclude(rp => rp.Ruta)
                .FirstOrDefaultAsync(p => p.IdParadero == id, cancellationToken);

            if (paradero == null)
            {
                return NotFound(new { message = "Paradero no encontrado." });
            }

            return Ok(MapToDto(paradero));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener paradero.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ParaderoManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var paradero = new Paradero
            {
                Nombre = model.Nombre.Trim(),
                DireccionReferencia = NormalizeOptional(model.DireccionReferencia),
                Distrito = model.Distrito.Trim(),
                Latitud = model.Latitud,
                Longitud = model.Longitud,
                Activo = true
            };

            _context.Paraderos.Add(paradero);
            await _context.SaveChangesAsync(cancellationToken);

            await SyncRouteAssignmentAsync(paradero.IdParadero, model, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var created = await _context.Paraderos
                .Include(p => p.RutaParaderos)
                    .ThenInclude(rp => rp.Ruta)
                .FirstAsync(p => p.IdParadero == paradero.IdParadero, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.IdParadero }, MapToDto(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear paradero.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ParaderoManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != model.IdParadero) return BadRequest(new { message = "ID de paradero no coincide." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Paraderos
                .Include(p => p.RutaParaderos)
                    .ThenInclude(rp => rp.Ruta)
                .FirstOrDefaultAsync(p => p.IdParadero == id, cancellationToken);

            if (existing == null)
            {
                return NotFound(new { message = "Paradero no encontrado." });
            }

            existing.Nombre = model.Nombre.Trim();
            existing.DireccionReferencia = NormalizeOptional(model.DireccionReferencia);
            existing.Distrito = model.Distrito.Trim();
            existing.Latitud = model.Latitud;
            existing.Longitud = model.Longitud;
            existing.Activo = model.Activo;

            await SyncRouteAssignmentAsync(existing.IdParadero, model, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(MapToDto(existing));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar paradero.", details = ex.Message });
        }
    }

    private async Task SyncRouteAssignmentAsync(int paraderoId, ParaderoManagementDto model, CancellationToken cancellationToken)
    {
        var assignments = await _context.RutaParaderos
            .Where(rp => rp.IdParadero == paraderoId)
            .ToListAsync(cancellationToken);

        if (!model.IdRuta.HasValue)
        {
            foreach (var assignment in assignments.Where(rp => rp.Activo))
            {
                assignment.Activo = false;
            }

            return;
        }

        var route = await _context.Rutas.FirstOrDefaultAsync(r => r.IdRuta == model.IdRuta.Value, cancellationToken);
        if (route == null)
        {
            throw new InvalidOperationException("La ruta seleccionada no existe.");
        }

        foreach (var assignment in assignments.Where(rp => rp.IdRuta != model.IdRuta.Value && rp.Activo))
        {
            assignment.Activo = false;
        }

        var currentAssignment = assignments.FirstOrDefault(rp => rp.IdRuta == model.IdRuta.Value);
        if (currentAssignment == null)
        {
            currentAssignment = new RutaParadero
            {
                IdRuta = model.IdRuta.Value,
                IdParadero = paraderoId
            };
            _context.RutaParaderos.Add(currentAssignment);
        }

        currentAssignment.Orden = model.Orden ?? 1;
        currentAssignment.TiempoEstimadoDesdeInicioMin = model.TiempoEstimadoDesdeInicioMin ?? 0;
        currentAssignment.Activo = true;
    }

    private static ParaderoManagementDto MapToDto(Paradero paradero)
    {
        var assignment = paradero.RutaParaderos
            .Where(rp => rp.Activo)
            .OrderBy(rp => rp.Orden)
            .FirstOrDefault();

        return new ParaderoManagementDto
        {
            IdParadero = paradero.IdParadero,
            Nombre = paradero.Nombre,
            DireccionReferencia = paradero.DireccionReferencia,
            Distrito = paradero.Distrito,
            Latitud = paradero.Latitud,
            Longitud = paradero.Longitud,
            Activo = paradero.Activo,
            IdRuta = assignment?.IdRuta,
            CodigoRuta = assignment?.Ruta?.CodigoRuta,
            NombreRuta = assignment?.Ruta?.NombreRuta,
            RutaActiva = assignment?.Ruta?.Activa,
            Orden = assignment?.Orden,
            TiempoEstimadoDesdeInicioMin = assignment?.TiempoEstimadoDesdeInicioMin
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
