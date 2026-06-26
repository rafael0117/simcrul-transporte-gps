using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Passenger;

public class PassengerRequestCreateDto
{
    [Required(ErrorMessage = "Debe seleccionar un tipo de solicitud.")]
    public string TipoSolicitud { get; set; } = "RECLAMO";

    public int? IdRuta { get; set; }

    [Required(ErrorMessage = "El asunto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El asunto no debe superar 150 caracteres.")]
    public string Asunto { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripcion es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripcion no debe superar 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido.")]
    public string? EmailContacto { get; set; }

    [StringLength(30, ErrorMessage = "El telefono no debe superar 30 caracteres.")]
    public string? TelefonoContacto { get; set; }
}
