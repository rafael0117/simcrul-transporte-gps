using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, ErrorMessage = "El usuario no debe exceder los 50 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo es inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nombres son obligatorios.")]
    [StringLength(100, ErrorMessage = "Nombres no deben exceder 100 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Apellidos son obligatorios.")]
    [StringLength(100, ErrorMessage = "Apellidos no deben exceder 100 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Rol { get; set; } = "Pasajero"; // Default to passenger
}
