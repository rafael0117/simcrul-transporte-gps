namespace SIMCRUL.Entity;

public class ConsultaRuta
{
    public long IdConsulta { get; set; }
    public int? IdPasajero { get; set; }
    public int IdRuta { get; set; }
    public DateTime FechaConsulta { get; set; } = DateTime.UtcNow;
    public decimal? LatitudOrigenConsulta { get; set; }
    public decimal? LongitudOrigenConsulta { get; set; }
    public string Canal { get; set; } = "WEB"; // WEB, MOVIL

    public virtual Pasajero? Pasajero { get; set; }
    public virtual Ruta Ruta { get; set; } = null!;
}
