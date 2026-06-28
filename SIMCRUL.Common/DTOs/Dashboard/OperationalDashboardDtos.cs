namespace SIMCRUL.Common.DTOs.Dashboard;

public class OperationalDashboardSummaryDto
{
    public decimal DistanciaRecorridaKm { get; set; }
    public int TiempoOperativoMin { get; set; }
    public int ExcesosVelocidad { get; set; }
    public int DesviosRuta { get; set; }
    public int ReclamosRecibidos { get; set; }
    public string TopEmpresa { get; set; } = "Sin datos";
    public string TopVehiculo { get; set; } = "Sin datos";
    public string TopRuta { get; set; } = "Sin datos";
    public decimal TopDistanciaKm { get; set; }
    public List<OperationalChartPointDto> ExcesosPorDia { get; set; } = new();
    public List<OperationalChartPointDto> UnidadesPorHora { get; set; } = new();
}

public class OperationalChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class OperationalDetailDto
{
    public DateTime Fecha { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string CodigoVehiculo { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public decimal DistanciaKm { get; set; }
    public int TiempoOperativoMin { get; set; }
    public int Eventos { get; set; }
    public decimal? ValorMaximo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class RouteOptionDto
{
    public int IdRuta { get; set; }
    public string CodigoRuta { get; set; } = string.Empty;
    public string NombreRuta { get; set; } = string.Empty;
}

public class OperationalAlertNotificationDto
{
    public long IdAlerta { get; set; }
    public string TipoCodigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Severidad { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string CodigoVehiculo { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Conductor { get; set; } = string.Empty;
    public DateTime FechaAlerta { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal? ValorDetectado { get; set; }
    public decimal? ValorPermitido { get; set; }
    public string Estado { get; set; } = string.Empty;
}
