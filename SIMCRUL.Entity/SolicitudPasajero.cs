namespace SIMCRUL.Entity;

public class SolicitudPasajero
{
    public int IdSolicitudPasajero { get; set; }
    public int IdUsuario { get; set; }
    public int? IdRuta { get; set; }
    public string TipoSolicitud { get; set; } = "RECLAMO";
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public string? EmailContacto { get; set; }
    public string? TelefonoContacto { get; set; }
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Ruta? Ruta { get; set; }
}
