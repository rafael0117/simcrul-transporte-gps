using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsuariosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Usuarios
                .Include(u => u.Rol)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(u => u.Activo);
            }

            var users = await query
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync(cancellationToken);

            return Ok(users.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar usuarios.", details = ex.Message });
        }
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .Select(r => new RoleOptionDto
                {
                    IdRol = r.IdRol,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion
                })
                .ToListAsync(cancellationToken);

            return Ok(roles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener roles.", details = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Trim().Length < 6)
            {
                return BadRequest(new { message = "La contrasena inicial es obligatoria y debe tener al menos 6 caracteres." });
            }

            var normalizedUsername = model.Username.Trim();
            var normalizedEmail = model.Email.Trim();

            if (await _context.Usuarios.AnyAsync(u => u.Username == normalizedUsername, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe un usuario con ese nombre de usuario." });
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe un usuario con ese correo." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == model.IdRol && r.Activo, cancellationToken);
            if (role == null)
            {
                return BadRequest(new { message = "El rol seleccionado no existe o esta inactivo." });
            }

            var user = new Usuario
            {
                Username = normalizedUsername,
                Email = normalizedEmail,
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Telefono = NormalizeOptional(model.Telefono),
                PasswordHash = ComputeHash(model.Password.Trim()),
                IdRol = model.IdRol,
                Activo = model.Activo,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var created = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstAsync(u => u.IdUsuario == user.IdUsuario, cancellationToken);

            return CreatedAtAction(nameof(GetAll), new { id = created.IdUsuario }, MapToDto(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear usuario.", details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserManagementDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != model.IdUsuario)
            {
                return BadRequest(new { message = "El identificador del usuario no coincide." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == id, cancellationToken);

            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            var normalizedUsername = model.Username.Trim();
            var normalizedEmail = model.Email.Trim();

            if (await _context.Usuarios.AnyAsync(u => u.IdUsuario != id && u.Username == normalizedUsername, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe otro usuario con ese nombre de usuario." });
            }

            if (await _context.Usuarios.AnyAsync(u => u.IdUsuario != id && u.Email == normalizedEmail, cancellationToken))
            {
                return BadRequest(new { message = "Ya existe otro usuario con ese correo." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == model.IdRol && r.Activo, cancellationToken);
            if (role == null)
            {
                return BadRequest(new { message = "El rol seleccionado no existe o esta inactivo." });
            }

            user.Username = normalizedUsername;
            user.Email = normalizedEmail;
            user.Nombres = model.Nombres.Trim();
            user.Apellidos = model.Apellidos.Trim();
            user.Telefono = NormalizeOptional(model.Telefono);
            user.IdRol = model.IdRol;
            user.Activo = model.Activo;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (model.Password.Trim().Length < 6)
                {
                    return BadRequest(new { message = "La nueva contrasena debe tener al menos 6 caracteres." });
                }

                user.PasswordHash = ComputeHash(model.Password.Trim());
            }

            await _context.SaveChangesAsync(cancellationToken);

            user.Rol = role;
            return Ok(MapToDto(user));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar usuario.", details = ex.Message });
        }
    }

    [HttpPost("ensure-core-roles")]
    public async Task<IActionResult> EnsureCoreRoles(CancellationToken cancellationToken = default)
    {
        try
        {
            var requiredRoles = new[]
            {
                Roles.Administrador,
                Roles.Supervisor,
                Roles.Operador,
                Roles.Conductor,
                Roles.Pasajero
            };

            foreach (var roleName in requiredRoles)
            {
                if (await _context.Roles.AnyAsync(r => r.Nombre == roleName, cancellationToken))
                {
                    continue;
                }

                _context.Roles.Add(new Rol
                {
                    Nombre = roleName,
                    Descripcion = $"Rol de {roleName} para control de accesos",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Roles base verificados correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "No se pudieron asegurar los roles base.", details = ex.Message });
        }
    }

    private static UserManagementDto MapToDto(Usuario user)
    {
        return new UserManagementDto
        {
            IdUsuario = user.IdUsuario,
            Username = user.Username,
            Email = user.Email,
            Nombres = user.Nombres,
            Apellidos = user.Apellidos,
            Telefono = user.Telefono,
            IdRol = user.IdRol,
            Rol = user.Rol.Nombre,
            Activo = user.Activo,
            FechaCreacion = user.FechaCreacion
        };
    }

    private static string ComputeHash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
