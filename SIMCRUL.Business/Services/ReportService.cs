using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Data.Context;

namespace SIMCRUL.Business.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
        // Initialize QuestPDF Community license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateAlertsPdfReportAsync(DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default)
    {
        var alerts = await _context.Alertas
            .Include(a => a.TipoAlerta)
            .Include(a => a.Vehiculo)
            .Include(a => a.Conductor)
            .Where(a => a.FechaAlerta >= dateFrom && a.FechaAlerta <= dateTo)
            .OrderByDescending(a => a.FechaAlerta)
            .ToListAsync(cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                // Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("SISTEMA INTELIGENTE DE MONITOREO Y CONTROL DE RUTAS")
                            .Bold().FontSize(14).FontColor("#1A365D");
                        col.Item().Text($"REPORTE DE VIOLACIONES Y ALERTAS DEL TRÁNSITO")
                            .Bold().FontSize(12).FontColor("#4A5568");
                        col.Item().Text($"Rango: {dateFrom:dd/MM/yyyy HH:mm} al {dateTo:dd/MM/yyyy HH:mm}")
                            .FontSize(9).Italic().FontColor("#718096");
                    });

                    row.ConstantItem(80).AlignRight().AlignMiddle().Column(col =>
                    {
                        col.Item().Text("SIMCRUL").Bold().FontSize(20).FontColor("#1A365D");
                        col.Item().Text("GPS Tracker").FontSize(8).FontColor("#718096");
                    });
                });

                // Content
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        // 8 Columns: Fecha, Alerta, Severidad, Vehículo, Conductor, Descripción, Lat/Lng, Estado
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(110); // Fecha
                            columns.ConstantColumn(90);  // Tipo
                            columns.ConstantColumn(60);  // Severidad
                            columns.ConstantColumn(70);  // Vehículo
                            columns.ConstantColumn(120); // Conductor
                            columns.RelativeColumn();    // Descripción
                            columns.ConstantColumn(60);  // Estado
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background("#1A365D").Padding(5).Text("Fecha/Hora").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1A365D").Padding(5).Text("Tipo Alerta").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1A365D").Padding(5).Text("Severidad").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1A365D").Padding(5).Text("Placa/Veh").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1A365D").Padding(5).Text("Conductor").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1A365D").Padding(5).Text("Detalle / Infracción").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1A365D").Padding(5).Text("Estado").Bold().FontColor(Colors.White);
                        });

                        // Data rows
                        foreach (var a in alerts)
                        {
                            var severityColor = a.TipoAlerta.NivelSeveridad switch
                            {
                                >= 4 => "#C53030", // Red
                                3 => "#DD6B20",    // Orange
                                _ => "#3182CE"     // Blue
                            };

                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text($"{a.FechaAlerta:dd/MM/yyyy HH:mm:ss}");
                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(a.TipoAlerta.Nombre).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text($"Nivel {a.TipoAlerta.NivelSeveridad}").Bold().FontColor(severityColor);
                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(a.Vehiculo.Placa);
                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(a.Conductor != null ? $"{a.Conductor.Nombres} {a.Conductor.Apellidos}" : "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(a.Descripcion);
                            table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(a.Estado).Bold().FontColor(a.Estado == "PENDIENTE" ? "#DD6B20" : "#38A169");
                        }
                    });
                });

                // Footer
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Reporte generado por SIMCRUL - Confidencial del Operador").FontSize(8).FontColor("#A0AEC0");
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Página ").FontSize(8).FontColor("#A0AEC0");
                        x.CurrentPageNumber().FontSize(8).FontColor("#A0AEC0");
                        x.Span(" de ").FontSize(8).FontColor("#A0AEC0");
                        x.TotalPages().FontSize(8).FontColor("#A0AEC0");
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateTripsExcelReportAsync(DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default)
    {
        var trips = await _context.Viajes
            .Include(v => v.AsignacionOperacion)
                .ThenInclude(ao => ao.Ruta)
            .Include(v => v.AsignacionOperacion)
                .ThenInclude(ao => ao.Vehiculo)
            .Include(v => v.AsignacionOperacion)
                .ThenInclude(ao => ao.Conductor)
            .Where(v => v.FechaInicioReal >= dateFrom && v.FechaInicioReal <= dateTo)
            .OrderByDescending(v => v.FechaInicioReal)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Resumen de Viajes");

        // Styling Title
        ws.Cell("A1").Value = "SISTEMA DE MONITOREO GPS - REPORTE DE DESEMPEÑO DE OPERACIONES";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1A365D");

        ws.Cell("A2").Value = $"Rango del Reporte: {dateFrom:dd/MM/yyyy} al {dateTo:dd/MM/yyyy} | Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell("A2").Style.Font.Italic = true;
        ws.Cell("A2").Style.Font.FontSize = 10;
        ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#4A5568");

        // Headers
        string[] headers = {
            "ID Viaje", "Código Ruta", "Nombre Ruta", "Placa", 
            "Conductor", "Inicio Real", "Fin Real", "Estado", 
            "Score Conducción", "Desempeño Driver"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A365D");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data
        int rowIdx = 5;
        foreach (var t in trips)
        {
            var ao = t.AsignacionOperacion;

            ws.Cell(rowIdx, 1).Value = t.IdViaje;
            ws.Cell(rowIdx, 2).Value = ao.Ruta.CodigoRuta;
            ws.Cell(rowIdx, 3).Value = ao.Ruta.NombreRuta;
            ws.Cell(rowIdx, 4).Value = ao.Vehiculo.Placa;
            ws.Cell(rowIdx, 5).Value = $"{ao.Conductor.Nombres} {ao.Conductor.Apellidos}";
            ws.Cell(rowIdx, 6).Value = t.FechaInicioReal.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(rowIdx, 7).Value = t.FechaFinReal?.ToString("dd/MM/yyyy HH:mm:ss") ?? "En Ruta";
            ws.Cell(rowIdx, 8).Value = t.Estado;
            
            var scoreCell = ws.Cell(rowIdx, 9);
            scoreCell.Value = t.ScoreConduccion;
            scoreCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Driver scoring color criteria
            var scoreDescCell = ws.Cell(rowIdx, 10);
            if (t.ScoreConduccion >= 90)
            {
                scoreDescCell.Value = "Excelente";
                scoreDescCell.Style.Font.FontColor = XLColor.Green;
                scoreCell.Style.Font.FontColor = XLColor.Green;
            }
            else if (t.ScoreConduccion >= 70)
            {
                scoreDescCell.Value = "Aceptable";
                scoreDescCell.Style.Font.FontColor = XLColor.Orange;
                scoreCell.Style.Font.FontColor = XLColor.Orange;
            }
            else
            {
                scoreDescCell.Value = "Riesgoso";
                scoreDescCell.Style.Font.FontColor = XLColor.Red;
                scoreCell.Style.Font.FontColor = XLColor.Red;
            }

            // Gridlines border
            for (int col = 1; col <= headers.Length; col++)
            {
                ws.Cell(rowIdx, col).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Cell(rowIdx, col).Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
            }

            rowIdx++;
        }

        // Auto-fit columns
        ws.Columns(1, headers.Length).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateRoutesPdfReportAsync(CancellationToken cancellationToken = default)
    {
        var routes = await _context.Rutas
            .Include(r => r.RutaParaderos.OrderBy(rp => rp.Orden))
                .ThenInclude(rp => rp.Paradero)
            .Where(r => r.Activa)
            .OrderBy(r => r.CodigoRuta)
            .ToListAsync(cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Column(column =>
                {
                    column.Item().Text("SIMCRUL - CATALOGO DE RUTAS PARA PASAJEROS")
                        .Bold().FontSize(16).FontColor("#1A365D");
                    column.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor("#718096");
                });

                page.Content().PaddingVertical(0.8f, Unit.Centimetre).Column(column =>
                {
                    foreach (var route in routes)
                    {
                        column.Item().PaddingBottom(12).Border(1).BorderColor("#D9E2EC").Padding(12).Column(routeColumn =>
                        {
                            routeColumn.Item().Text($"{route.CodigoRuta} - {route.NombreRuta}")
                                .Bold().FontSize(13).FontColor("#0F4C81");
                            routeColumn.Item().PaddingTop(4).Text($"Origen: {route.Origen} | Destino: {route.Destino}");
                            routeColumn.Item().Text($"Distancia: {route.DistanciaKm} km | Tiempo estimado: {route.TiempoEstimadoMin} min | Velocidad maxima: {route.VelocidadMaximaKmh} km/h");

                            var stops = route.RutaParaderos
                                .OrderBy(rp => rp.Orden)
                                .Select(rp => $"{rp.Orden}. {rp.Paradero.Nombre} ({rp.TiempoEstimadoDesdeInicioMin} min)")
                                .ToList();

                            routeColumn.Item().PaddingTop(6).Text("Paraderos principales:")
                                .Bold().FontColor("#1F2937");

                            foreach (var stop in stops)
                            {
                                routeColumn.Item().PaddingLeft(8).Text($"- {stop}");
                            }
                        });
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Pagina ").FontSize(8).FontColor("#A0AEC0");
                    text.CurrentPageNumber().FontSize(8).FontColor("#A0AEC0");
                    text.Span(" de ").FontSize(8).FontColor("#A0AEC0");
                    text.TotalPages().FontSize(8).FontColor("#A0AEC0");
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
