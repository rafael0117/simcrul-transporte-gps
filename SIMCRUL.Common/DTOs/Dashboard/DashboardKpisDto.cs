namespace SIMCRUL.Common.DTOs.Dashboard;

public class DashboardKpisDto
{
    public int VehiculosActivos { get; set; }
    public int AlertasHoy { get; set; }
    public double VelocidadPromedioKmh { get; set; }
    public int RutasMonitoreadas { get; set; }
    public double ScorePromedioConductores { get; set; }
    public int ConductoresConectados { get; set; }
}
