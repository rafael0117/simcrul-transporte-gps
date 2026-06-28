using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El correo o nombre de usuario es obligatorio.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;

    public string? RecaptchaToken { get; set; }
}
