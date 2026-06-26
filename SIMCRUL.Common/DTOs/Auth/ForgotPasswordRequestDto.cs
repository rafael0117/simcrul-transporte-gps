using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    public string Email { get; set; } = string.Empty;
}
