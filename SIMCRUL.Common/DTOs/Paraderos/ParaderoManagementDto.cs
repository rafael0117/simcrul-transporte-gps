using System.ComponentModel.DataAnnotations;

namespace SIMCRUL.Common.DTOs.Paraderos;

public class ParaderoManagementDto
{
    public int IdParadero { get; set; }

    [Required(ErrorMessage = "El nombre del paradero es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    public string? DireccionReferencia { get; set; }

    [Required(ErrorMessage = "El distrito es obligatorio.")]
    public string Distrito { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "La latitud no es valida.")]
    public decimal Latitud { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitud no es valida.")]
    public decimal Longitud { get; set; }

    public bool Activo { get; set; } = true;

    public int? IdRuta { get; set; }
    public string? CodigoRuta { get; set; }
    public string? NombreRuta { get; set; }
    public bool? RutaActiva { get; set; }
    public int? Orden { get; set; }
    public int? TiempoEstimadoDesdeInicioMin { get; set; }
}
