namespace SIMCRUL.Entity;

public class FavoritoPasajero
{
    public int IdFavorito { get; set; }
    public int IdPasajero { get; set; }
    public int IdRuta { get; set; }
    public int? IdParaderoOrigen { get; set; }
    public int? IdParaderoDestino { get; set; }
    public string Alias { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public virtual Pasajero Pasajero { get; set; } = null!;
    public virtual Ruta Ruta { get; set; } = null!;
    public virtual Paradero? ParaderoOrigen { get; set; }
    public virtual Paradero? ParaderoDestino { get; set; }
}
