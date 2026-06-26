using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.DTOs.Passenger;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PassengerRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PassengerRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "No se pudo identificar al pasajero autenticado." });
        }

        var requests = await _context.SolicitudesPasajero
            .AsNoTracking()
            .Include(sp => sp.Ruta)
            .Where(sp => sp.IdUsuario == userId.Value)
            .OrderByDescending(sp => sp.FechaRegistro)
            .Select(sp => new PassengerRequestDto
            {
                IdSolicitudPasajero = sp.IdSolicitudPasajero,
                IdUsuario = sp.IdUsuario,
                IdRuta = sp.IdRuta,
                CodigoRuta = sp.Ruta != null ? sp.Ruta.CodigoRuta : null,
                NombreRuta = sp.Ruta != null ? sp.Ruta.NombreRuta : null,
                TipoSolicitud = sp.TipoSolicitud,
                Asunto = sp.Asunto,
                Descripcion = sp.Descripcion,
                Estado = sp.Estado,
                FechaRegistro = sp.FechaRegistro,
                EmailContacto = sp.EmailContacto,
                TelefonoContacto = sp.TelefonoContacto,
                Respuesta = sp.Respuesta,
                FechaRespuesta = sp.FechaRespuesta
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PassengerRequestCreateDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "No se pudo identificar al pasajero autenticado." });
        }

        if (request.IdRuta.HasValue)
        {
            var routeExists = await _context.Rutas.AnyAsync(r => r.IdRuta == request.IdRuta.Value && r.Activa, cancellationToken);
            if (!routeExists)
            {
                return BadRequest(new { message = "La ruta seleccionada no existe o no esta activa." });
            }
        }

        var entity = new SolicitudPasajero
        {
            IdUsuario = userId.Value,
            IdRuta = request.IdRuta,
            TipoSolicitud = request.TipoSolicitud.Trim().ToUpperInvariant(),
            Asunto = request.Asunto.Trim(),
            Descripcion = request.Descripcion.Trim(),
            Estado = "PENDIENTE",
            FechaRegistro = DateTime.UtcNow,
            EmailContacto = NormalizeOptional(request.EmailContacto),
            TelefonoContacto = NormalizeOptional(request.TelefonoContacto)
        };

        _context.SolicitudesPasajero.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new PassengerRequestDto
        {
            IdSolicitudPasajero = entity.IdSolicitudPasajero,
            IdUsuario = entity.IdUsuario,
            IdRuta = entity.IdRuta,
            TipoSolicitud = entity.TipoSolicitud,
            Asunto = entity.Asunto,
            Descripcion = entity.Descripcion,
            Estado = entity.Estado,
            FechaRegistro = entity.FechaRegistro,
            EmailContacto = entity.EmailContacto,
            TelefonoContacto = entity.TelefonoContacto
        });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
