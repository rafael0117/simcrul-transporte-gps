using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditoriaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tabla,
        [FromQuery] string? accion,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Auditorias.AsQueryable();

            if (!string.IsNullOrEmpty(tabla))
            {
                query = query.Where(a => a.TablaAfectada.Contains(tabla));
            }

            if (!string.IsNullOrEmpty(accion))
            {
                query = query.Where(a => a.Accion == accion);
            }

            if (desde.HasValue)
            {
                query = query.Where(a => a.Fecha >= desde.Value);
            }

            if (hasta.HasValue)
            {
                // Set to end of day if it has date but not time
                var limitDate = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Fecha <= limitDate);
            }

            var logs = await query
                .OrderByDescending(a => a.Fecha)
                .Take(250) // Cap results for performance
                .ToListAsync(cancellationToken);

            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar bitácora de auditoría.", details = ex.Message });
        }
    }
}
