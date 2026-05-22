namespace SIMCRUL.Entity;

public class RutaPuntoControl
{
    public int IdPuntoControl { get; set; }
    public int IdRuta { get; set; }
    public int Orden { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public int RadioToleranciaMetros { get; set; }
    public bool EsParadero { get; set; }
    public string? Descripcion { get; set; }

    public virtual Ruta Ruta { get; set; } = null!;
}
