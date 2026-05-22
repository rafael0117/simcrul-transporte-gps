using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Business.Security;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.Business.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _jwtOptions;

    public AuthService(ApplicationDbContext context, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var passwordHash = ComputeHash(request.Password);

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => 
                (u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail) 
                && u.PasswordHash == passwordHash 
                && u.Activo, 
                cancellationToken);

        if (usuario == null)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas o usuario inactivo.");
        }

        return GenerateAuthResponse(usuario);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existeUser = await _context.Usuarios.AnyAsync(u => u.Username == request.Username || u.Email == request.Email, cancellationToken);
        if (existeUser)
        {
            throw new InvalidOperationException("El nombre de usuario o correo ya está registrado.");
        }

        // Get or create Role
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == request.Rol, cancellationToken);
        if (rol == null)
        {
            rol = new Rol { Nombre = request.Rol, Activo = true, FechaCreacion = DateTime.UtcNow };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var usuario = new Usuario
        {
            Username = request.Username,
            Email = request.Email,
            Nombres = request.Nombres,
            Apellidos = request.Apellidos,
            Telefono = request.Telefono,
            PasswordHash = ComputeHash(request.Password),
            IdRol = rol.IdRol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        // If registering as Passenger, let's also create the passenger record
        if (request.Rol == Common.Constants.Roles.Pasajero)
        {
            var pasajero = new Pasajero
            {
                Nombres = request.Nombres,
                Apellidos = request.Apellidos,
                Email = request.Email,
                Telefono = request.Telefono,
                DocumentoIdentidad = "00000000",
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            };
            _context.Pasajeros.Add(pasajero);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return GenerateAuthResponse(usuario);
    }

    private AuthResponseDto GenerateAuthResponse(Usuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.Nombre),
            new("Nombres", usuario.Nombres),
            new("Apellidos", usuario.Apellidos)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new AuthResponseDto
        {
            Token = tokenString,
            Expiration = tokenDescriptor.Expires.Value,
            Rol = usuario.Rol.Nombre,
            Username = usuario.Username,
            Nombres = usuario.Nombres,
            Apellidos = usuario.Apellidos
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
}
