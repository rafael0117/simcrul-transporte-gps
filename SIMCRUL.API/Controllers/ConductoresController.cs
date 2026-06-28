using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Drivers;
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
            var query = _context.Conductores
                .Include(c => c.Usuario)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(c => c.Estado);
            }

            var list = await query
                .OrderBy(c => c.Apellidos)
                .ThenBy(c => c.Nombres)
                .Select(c => new ConductorManagementDto
                {
                    IdConductor = c.IdConductor,
                    IdEmpresa = c.IdEmpresa,
                    IdUsuario = c.IdUsuario,
                    Nombres = c.Nombres,
                    Apellidos = c.Apellidos,
                    Dni = c.Dni,
                    NumeroLicencia = c.NumeroLicencia,
                    CategoriaLicencia = c.CategoriaLicencia,
                    FechaVencimientoLicencia = c.FechaVencimientoLicencia,
                    Telefono = c.Telefono,
                    Estado = c.Estado,
                    FechaRegistro = c.FechaRegistro,
                    UsernameUsuario = c.Usuario != null ? c.Usuario.Username : null,
                    NombreUsuario = c.Usuario != null ? (c.Usuario.Nombres + " " + c.Usuario.Apellidos).Trim() : null
                })
                .ToListAsync(cancellationToken);

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
            var conductor = await _context.Conductores
                .Include(c => c.Usuario)
                .Where(c => c.IdConductor == id)
                .Select(c => new ConductorManagementDto
                {
                    IdConductor = c.IdConductor,
                    IdEmpresa = c.IdEmpresa,
                    IdUsuario = c.IdUsuario,
                    Nombres = c.Nombres,
                    Apellidos = c.Apellidos,
                    Dni = c.Dni,
                    NumeroLicencia = c.NumeroLicencia,
                    CategoriaLicencia = c.CategoriaLicencia,
                    FechaVencimientoLicencia = c.FechaVencimientoLicencia,
                    Telefono = c.Telefono,
                    Estado = c.Estado,
                    FechaRegistro = c.FechaRegistro,
                    UsernameUsuario = c.Usuario != null ? c.Usuario.Username : null,
                    NombreUsuario = c.Usuario != null ? (c.Usuario.Nombres + " " + c.Usuario.Apellidos).Trim() : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conductor == null) return NotFound(new { message = "Conductor no encontrado." });
            return Ok(conductor);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener conductor.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("driver-users")]
    public async Task<IActionResult> GetDriverUsers(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Activo && u.Rol.Nombre == Roles.Conductor)
                .OrderBy(u => u.Username)
                .Select(u => new ConductorUserOptionDto
                {
                    IdUsuario = u.IdUsuario,
                    Username = u.Username,
                    NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim(),
                    Asignado = _context.Conductores.Any(c => c.IdUsuario == u.IdUsuario && c.Estado),
                    IdConductorAsignado = _context.Conductores
                        .Where(c => c.IdUsuario == u.IdUsuario && c.Estado)
                        .Select(c => (int?)c.IdConductor)
                        .FirstOrDefault(),
                    ConductorAsignado = _context.Conductores
                        .Where(c => c.IdUsuario == u.IdUsuario && c.Estado)
                        .Select(c => (c.Nombres + " " + c.Apellidos).Trim())
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "No se pudieron cargar los usuarios conductores.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ConductorManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userValidationError = await ValidateAssignedUserAsync(model.IdUsuario, null, cancellationToken);
            if (userValidationError != null)
            {
                return BadRequest(new { message = userValidationError });
            }

            if (model.IdEmpresa <= 0)
            {
                var company = await _context.EmpresasTransporte.FirstOrDefaultAsync(cancellationToken);
                model.IdEmpresa = company?.IdEmpresa ?? 1;
            }

            var entity = new Conductor
            {
                IdEmpresa = model.IdEmpresa,
                IdUsuario = model.IdUsuario,
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                Dni = model.Dni,
                NumeroLicencia = model.NumeroLicencia,
                CategoriaLicencia = model.CategoriaLicencia,
                FechaVencimientoLicencia = model.FechaVencimientoLicencia,
                Telefono = model.Telefono,
                FechaRegistro = DateTime.UtcNow,
                Estado = true
            };

            _context.Conductores.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = entity.IdConductor }, await BuildDtoAsync(entity.IdConductor, cancellationToken));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear conductor.", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ConductorManagementDto model, CancellationToken cancellationToken = default)
    {
        try
            {
            if (id != model.IdConductor) return BadRequest(new { message = "ID de conductor no coincide." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Conductores.FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);
            if (existing == null) return NotFound(new { message = "Conductor no encontrado." });

            var userValidationError = await ValidateAssignedUserAsync(model.IdUsuario, id, cancellationToken);
            if (userValidationError != null)
            {
                return BadRequest(new { message = userValidationError });
            }

            existing.IdUsuario = model.IdUsuario;
            existing.Nombres = model.Nombres;
            existing.Apellidos = model.Apellidos;
            existing.Dni = model.Dni;
            existing.NumeroLicencia = model.NumeroLicencia;
            existing.CategoriaLicencia = model.CategoriaLicencia;
            existing.FechaVencimientoLicencia = model.FechaVencimientoLicencia;
            existing.Telefono = model.Telefono;
            existing.Estado = model.Estado;

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(await BuildDtoAsync(id, cancellationToken));
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

    private async Task<string?> ValidateAssignedUserAsync(int? userId, int? currentDriverId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == userId.Value, cancellationToken);

        if (user == null || !user.Activo)
        {
            return "El usuario seleccionado no existe o esta inactivo.";
        }

        if (!string.Equals(user.Rol.Nombre, Roles.Conductor, StringComparison.OrdinalIgnoreCase))
        {
            return "Solo se pueden vincular usuarios con rol Conductor.";
        }

        var assignedDriver = await _context.Conductores
            .Where(c => c.IdUsuario == userId.Value && (!currentDriverId.HasValue || c.IdConductor != currentDriverId.Value))
            .Select(c => new { c.IdConductor, c.Nombres, c.Apellidos })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignedDriver != null)
        {
            return $"El usuario ya esta vinculado al conductor #{assignedDriver.IdConductor} {assignedDriver.Nombres} {assignedDriver.Apellidos}.";
        }

        return null;
    }

    private async Task<ConductorManagementDto?> BuildDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Conductores
            .Include(c => c.Usuario)
            .Where(c => c.IdConductor == id)
            .Select(c => new ConductorManagementDto
            {
                IdConductor = c.IdConductor,
                IdEmpresa = c.IdEmpresa,
                IdUsuario = c.IdUsuario,
                Nombres = c.Nombres,
                Apellidos = c.Apellidos,
                Dni = c.Dni,
                NumeroLicencia = c.NumeroLicencia,
                CategoriaLicencia = c.CategoriaLicencia,
                FechaVencimientoLicencia = c.FechaVencimientoLicencia,
                Telefono = c.Telefono,
                Estado = c.Estado,
                FechaRegistro = c.FechaRegistro,
                UsernameUsuario = c.Usuario != null ? c.Usuario.Username : null,
                NombreUsuario = c.Usuario != null ? (c.Usuario.Nombres + " " + c.Usuario.Apellidos).Trim() : null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
