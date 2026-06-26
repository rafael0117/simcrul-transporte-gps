namespace SIMCRUL.Common.DTOs.Passenger;

public class PassengerRequestDto
{
    public int IdSolicitudPasajero { get; set; }
    public int IdUsuario { get; set; }
    public int? IdRuta { get; set; }
    public string? CodigoRuta { get; set; }
    public string? NombreRuta { get; set; }
    public string TipoSolicitud { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string? EmailContacto { get; set; }
    public string? TelefonoContacto { get; set; }
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}
