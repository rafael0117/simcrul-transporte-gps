namespace SIMCRUL.Entity;

public class VwUltimaUbicacionVehiculo
{
    public int IdVehiculo { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string CodigoInterno { get; set; } = string.Empty;
    public long? IdViaje { get; set; }
    public int? IdRuta { get; set; }
    public string? CodigoRuta { get; set; }
    public string? Conductor { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public decimal? VelocidadKmh { get; set; }
    public DateTime? FechaGps { get; set; }
    public int? ScoreConduccion { get; set; }
}
