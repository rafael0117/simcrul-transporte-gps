namespace SIMCRUL.Entity;

public class InspeccionDiaria
{
    public int IdInspeccionDiaria { get; set; }
    public int IdVehiculo { get; set; }
    public int IdConductorUsuario { get; set; }
    public DateTime FechaInspeccion { get; set; } = DateTime.UtcNow;
    public decimal Kilometraje { get; set; }
    public string NivelCombustible { get; set; } = "MEDIO";
    public bool LimpiezaCabina { get; set; }
    public bool LimpiezaExterior { get; set; }
    public bool LucesOperativas { get; set; }
    public bool FrenosOperativos { get; set; }
    public bool NeumaticosOperativos { get; set; }
    public bool BocinaOperativa { get; set; }
    public bool EspejosOperativos { get; set; }
    public bool BotiquinDisponible { get; set; }
    public bool ExtintorDisponible { get; set; }
    public bool DocumentosCompletos { get; set; }
    public string Resultado { get; set; } = "APROBADO";
    public string? Observaciones { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
    public virtual Usuario ConductorUsuario { get; set; } = null!;
}
