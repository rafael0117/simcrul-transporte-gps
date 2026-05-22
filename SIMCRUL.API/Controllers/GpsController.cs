using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.DTOs.Telemetry;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GpsController : ControllerBase
{
    private readonly IGpsProcessingService _gpsProcessingService;

    public GpsController(IGpsProcessingService gpsProcessingService)
    {
        _gpsProcessingService = gpsProcessingService;
    }

    [HttpPost("telemetry")]
    public async Task<IActionResult> PostTelemetry([FromBody] TelemetryDto telemetryDto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _gpsProcessingService.ProcessTelemetryAsync(telemetryDto, cancellationToken);
            return Ok(new { status = "success", message = "Telemetría procesada y transmitida correctamente." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { status = "error", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = "Error interno al procesar telemetría.", details = ex.Message });
        }
    }
}
