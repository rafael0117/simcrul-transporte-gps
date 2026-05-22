using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Business.Interfaces;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("alerts-pdf")]
    public async Task<IActionResult> DownloadAlertsPdf([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken cancellationToken)
    {
        var from = dateFrom ?? DateTime.Today;
        var to = dateTo ?? DateTime.Today.AddDays(1).AddSeconds(-1);

        try
        {
            var pdfBytes = await _reportService.GenerateAlertsPdfReportAsync(from, to, cancellationToken);
            var fileName = $"ReporteAlertas_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar reporte PDF.", details = ex.Message });
        }
    }

    [HttpGet("trips-excel")]
    public async Task<IActionResult> DownloadTripsExcel([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken cancellationToken)
    {
        var from = dateFrom ?? DateTime.Today.AddDays(-7); // Default to last 7 days for Excel performance
        var to = dateTo ?? DateTime.Today.AddDays(1).AddSeconds(-1);

        try
        {
            var excelBytes = await _reportService.GenerateTripsExcelReportAsync(from, to, cancellationToken);
            var fileName = $"ReporteViajes_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar reporte Excel.", details = ex.Message });
        }
    }
}
