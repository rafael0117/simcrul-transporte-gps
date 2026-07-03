using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Business.Models;
using SIMCRUL.Business.Security;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.Business.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordRecoveryOptions _passwordRecoveryOptions;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IOptions<JwtOptions> jwtOptions,
        IOptions<PasswordRecoveryOptions> passwordRecoveryOptions,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _passwordRecoveryOptions = passwordRecoveryOptions.Value;
        _emailService = emailService;
        _logger = logger;
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

        await TrySendWelcomeEmailAsync(usuario, cancellationToken);

        return GenerateAuthResponse(usuario);
    }

    private async Task TrySendWelcomeEmailAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendWelcomeEmailAsync(
                new WelcomeEmailMessage
                {
                    RecipientEmail = usuario.Email,
                    RecipientName = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                    Username = usuario.Username
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de bienvenida para el usuario {Username}.", usuario.Username);
        }
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequestDto request, string? requesterIp, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo, cancellationToken);

        if (usuario == null)
        {
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var activeTokens = await _context.PasswordResetTokens
            .Where(t => t.IdUsuario == usuario.IdUsuario && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.UsedAtUtc = DateTime.UtcNow;
        }

        var rawToken = GenerateResetToken();
        var expirationUtc = DateTime.UtcNow.AddMinutes(_passwordRecoveryOptions.TokenExpiryMinutes);
        usuario.PasswordHash = ComputeHash(GenerateTemporaryPassword());

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            IdUsuario = usuario.IdUsuario,
            TokenHash = ComputeHash(rawToken),
            ExpirationUtc = expirationUtc,
            RequestedByIp = requesterIp,
            EmailSentTo = usuario.Email
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordRecoveryEmailAsync(
            new PasswordRecoveryEmailMessage
            {
                RecipientEmail = usuario.Email,
                RecipientName = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                ResetUrl = BuildResetUrl(rawToken),
                ExpirationUtc = expirationUtc
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeHash(request.Token);
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.UsedAtUtc == null &&
                t.ExpirationUtc > DateTime.UtcNow,
                cancellationToken);

        if (resetToken == null)
        {
            throw new InvalidOperationException("El enlace de recuperacion no es valido o ya expiro.");
        }

        resetToken.Usuario.PasswordHash = ComputeHash(request.NewPassword);
        resetToken.UsedAtUtc = DateTime.UtcNow;

        var siblingTokens = await _context.PasswordResetTokens
            .Where(t => t.IdUsuario == resetToken.IdUsuario && t.IdPasswordResetToken != resetToken.IdPasswordResetToken && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var siblingToken in siblingTokens)
        {
            siblingToken.UsedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
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

    private static string GenerateResetToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
    }

    private static string GenerateTemporaryPassword()
    {
        return $"SIMCRUL-{WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(18))}";
    }

    private string BuildResetUrl(string rawToken)
    {
        var separator = _passwordRecoveryOptions.FrontendResetUrl.Contains('?') ? "&" : "?";
        return $"{_passwordRecoveryOptions.FrontendResetUrl}{separator}token={Uri.EscapeDataString(rawToken)}";
    }
}
