using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Auth;

public class UserManagementDto
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, ErrorMessage = "El usuario no debe exceder los 50 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo es invalido.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "La contrasena no debe exceder los 100 caracteres.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(100, ErrorMessage = "Los nombres no deben exceder 100 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(100, ErrorMessage = "Los apellidos no deben exceder 100 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El telefono no debe exceder 30 caracteres.")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un rol.")]
    public int IdRol { get; set; }

    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}
