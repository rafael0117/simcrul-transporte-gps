namespace SIMCRUL.Entity;

public class RepuestoUtilizado
{
    public int IdRepuestoUtilizado { get; set; }
    public int IdMantenimientoEjecutado { get; set; }
    public string CodigoRepuesto { get; set; } = string.Empty;
    public string NombreRepuesto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Observaciones { get; set; }

    public virtual MantenimientoEjecutado MantenimientoEjecutado { get; set; } = null!;
}
