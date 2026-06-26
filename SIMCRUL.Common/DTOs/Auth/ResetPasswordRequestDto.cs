using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "El token es obligatorio.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contrasena es obligatoria.")]
    [MinLength(6, ErrorMessage = "La nueva contrasena debe tener al menos 6 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contrasena.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contrasenas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
