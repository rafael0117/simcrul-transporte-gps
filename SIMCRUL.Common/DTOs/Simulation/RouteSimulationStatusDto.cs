namespace SIMCRUL.Common.DTOs.Simulation;

public class RouteSimulationStatusDto
{
    public int IdRuta { get; set; }
    public string CodigoRuta { get; set; } = string.Empty;
    public string NombreRuta { get; set; } = string.Empty;
    public int VehiculosSimulados { get; set; }
}
