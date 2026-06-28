using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Common.DTOs.Shared;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRecaptchaService _recaptchaService;

    public AuthController(IAuthService authService, IRecaptchaService recaptchaService)
    {
        _authService = authService;
        _recaptchaService = recaptchaService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var requesterIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var captchaValid = await _recaptchaService.ValidateAsync(request.RecaptchaToken, requesterIp, "login", cancellationToken);
            if (!captchaValid)
            {
                return BadRequest(new { message = "No se pudo validar el captcha. Intente nuevamente." });
            }

            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor.", details = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var requesterIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.RequestPasswordResetAsync(request, requesterIp, cancellationToken);
            return Ok(new MessageResponseDto
            {
                Message = "Si el correo existe en el sistema, recibira un enlace para restablecer su contrasena."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "No se pudo procesar la recuperacion de contrasena.", details = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _authService.ResetPasswordAsync(request, cancellationToken);
            return Ok(new MessageResponseDto
            {
                Message = "La contrasena fue actualizada correctamente."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "No se pudo restablecer la contrasena.", details = ex.Message });
        }
    }
}
