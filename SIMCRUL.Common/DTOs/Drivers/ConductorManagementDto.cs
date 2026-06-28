namespace SIMCRUL.Common.DTOs.Drivers;

public class ConductorManagementDto
{
    public int IdConductor { get; set; }
    public int IdEmpresa { get; set; }
    public int? IdUsuario { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string NumeroLicencia { get; set; } = string.Empty;
    public string CategoriaLicencia { get; set; } = string.Empty;
    public DateTime FechaVencimientoLicencia { get; set; }
    public string? Telefono { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaRegistro { get; set; }
    public string? UsernameUsuario { get; set; }
    public string? NombreUsuario { get; set; }
}
