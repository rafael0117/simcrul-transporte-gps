namespace SIMCRUL.Entity;

public class RutaParadero
{
    public int IdRutaParadero { get; set; }
    public int IdRuta { get; set; }
    public int IdParadero { get; set; }
    public int Orden { get; set; }
    public int TiempoEstimadoDesdeInicioMin { get; set; }
    public bool Activo { get; set; } = true;

    public virtual Ruta Ruta { get; set; } = null!;
    public virtual Paradero Paradero { get; set; } = null!;
}
