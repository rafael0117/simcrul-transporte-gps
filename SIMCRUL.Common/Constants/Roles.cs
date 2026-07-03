namespace SIMCRUL.Common.Constants;

public static class Roles
{
    public const string Administrador = "Administrador de Flota";
    public const string JefeMantenimiento = "Jefe de Mantenimiento";
    public const string TecnicoMantenimiento = "Tecnico de Mantenimiento";
    public const string Conductor = "Conductor";

    public static readonly string[] RolesBackoffice =
    [
        Administrador,
        JefeMantenimiento,
        TecnicoMantenimiento
    ];

    public static readonly string[] RolesGestion =
    [
        Administrador,
        JefeMantenimiento
    ];
}
