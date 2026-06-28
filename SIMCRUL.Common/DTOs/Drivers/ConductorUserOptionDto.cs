namespace SIMCRUL.Common.DTOs.Drivers;

public class ConductorUserOptionDto
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Asignado { get; set; }
    public int? IdConductorAsignado { get; set; }
    public string? ConductorAsignado { get; set; }
}
