namespace SIMCRUL.Entity;

public class Auditoria
{
    public long IdAuditoria { get; set; }
    public string Usuario { get; set; } = "SYSTEM";
    public string Accion { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public string TablaAfectada { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? DatosAnteriores { get; set; } // JSON
    public string? DatosNuevos { get; set; } // JSON
}
