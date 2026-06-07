using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Business.Interfaces;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly IFleetSimulationService _fleetSimulationService;

    public SimulationController(IFleetSimulationService fleetSimulationService)
    {
        _fleetSimulationService = fleetSimulationService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _fleetSimulationService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("start-demo")]
    public async Task<IActionResult> StartDemo([FromQuery] int intervalSeconds = 3, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _fleetSimulationService.StartAsync(intervalSeconds, cancellationToken);
            return Ok(status);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("stop-demo")]
    public async Task<IActionResult> StopDemo(CancellationToken cancellationToken)
    {
        var status = await _fleetSimulationService.StopAsync(cancellationToken);
        return Ok(status);
    }
}
