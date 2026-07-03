using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Maintenance;
using SIMCRUL.Data.Context;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/maintenance/catalog")]
[Authorize]
public class MaintenanceCatalogController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public MaintenanceCatalogController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles(CancellationToken cancellationToken = default)
    {
        var vehicles = await _context.Vehiculos
            .Where(v => v.Estado)
            .OrderBy(v => v.CodigoInterno)
            .Select(v => new VehicleOptionDto
            {
                IdVehiculo = v.IdVehiculo,
                Etiqueta = v.CodigoInterno + " - " + v.Placa + " (" + v.EstadoOperativo + ")",
                EstadoOperativo = v.EstadoOperativo
            })
            .ToListAsync(cancellationToken);

        return Ok(vehicles);
    }

    [HttpGet("technicians")]
    public async Task<IActionResult> GetTechnicians(CancellationToken cancellationToken = default)
    {
        var technicians = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo && u.Rol.Nombre == Roles.TecnicoMantenimiento)
            .OrderBy(u => u.Nombres)
            .ThenBy(u => u.Apellidos)
            .Select(u => new UserOptionDto
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim(),
                Rol = u.Rol.Nombre
            })
            .ToListAsync(cancellationToken);

        return Ok(technicians);
    }

    [HttpGet("conductors")]
    public async Task<IActionResult> GetConductors(CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador, Roles.JefeMantenimiento))
        {
            return Forbid();
        }

        var users = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo && u.Rol.Nombre == Roles.Conductor)
            .OrderBy(u => u.Nombres)
            .ThenBy(u => u.Apellidos)
            .Select(u => new UserOptionDto
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim(),
                Rol = u.Rol.Nombre
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}
