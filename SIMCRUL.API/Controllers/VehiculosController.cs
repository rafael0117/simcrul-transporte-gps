using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Shared;
using SIMCRUL.Common.DTOs.Vehicles;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiculosController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IReportService _reportService;

    public VehiculosController(ApplicationDbContext context, IReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Vehiculos.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(v => v.Estado);
        }

        var vehicles = await query
            .OrderBy(v => v.CodigoInterno)
            .ToListAsync(cancellationToken);

        return Ok(vehicles.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
        if (vehicle == null)
        {
            return NotFound(new { message = "Vehiculo no encontrado." });
        }

        return Ok(MapToDto(vehicle));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleManagementDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var normalizedPlate = model.Placa.Trim().ToUpperInvariant();
        var normalizedCode = model.CodigoInterno.Trim().ToUpperInvariant();

        if (await _context.Vehiculos.AnyAsync(v => v.Placa == normalizedPlate, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe un vehiculo con esa placa." });
        }

        if (await _context.Vehiculos.AnyAsync(v => v.CodigoInterno == normalizedCode, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe un vehiculo con ese codigo interno." });
        }

        var company = await _context.EmpresasTransporte.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync(cancellationToken);
        if (company == null)
        {
            return BadRequest(new { message = "No existe una empresa activa para asociar el vehiculo." });
        }

        var vehicle = new Vehiculo
        {
            IdEmpresa = company.IdEmpresa,
            Placa = normalizedPlate,
            CodigoInterno = normalizedCode,
            TipoVehiculo = model.TipoVehiculo.Trim().ToUpperInvariant(),
            Marca = NormalizeOptional(model.Marca),
            Modelo = NormalizeOptional(model.Modelo),
            Anio = model.Anio,
            CapacidadPasajeros = model.CapacidadPasajeros,
            VelocidadMaximaKmh = model.VelocidadMaximaKmh,
            KilometrajeActual = model.KilometrajeActual,
            EstadoOperativo = model.EstadoOperativo.Trim().ToUpperInvariant(),
            FechaUltimaInspeccion = model.FechaUltimaInspeccion,
            FechaUltimoMantenimiento = model.FechaUltimoMantenimiento,
            ObservacionesMantenimiento = NormalizeOptional(model.ObservacionesMantenimiento),
            Estado = model.Estado,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Vehiculos.Add(vehicle);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = vehicle.IdVehiculo }, MapToDto(vehicle));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleManagementDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        if (id != model.IdVehiculo)
        {
            return BadRequest(new { message = "El identificador del vehiculo no coincide." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
        if (vehicle == null)
        {
            return NotFound(new { message = "Vehiculo no encontrado." });
        }

        var normalizedPlate = model.Placa.Trim().ToUpperInvariant();
        var normalizedCode = model.CodigoInterno.Trim().ToUpperInvariant();

        if (await _context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.Placa == normalizedPlate, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe otro vehiculo con esa placa." });
        }

        if (await _context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.CodigoInterno == normalizedCode, cancellationToken))
        {
            return BadRequest(new { message = "Ya existe otro vehiculo con ese codigo interno." });
        }

        vehicle.Placa = normalizedPlate;
        vehicle.CodigoInterno = normalizedCode;
        vehicle.TipoVehiculo = model.TipoVehiculo.Trim().ToUpperInvariant();
        vehicle.Marca = NormalizeOptional(model.Marca);
        vehicle.Modelo = NormalizeOptional(model.Modelo);
        vehicle.Anio = model.Anio;
        vehicle.CapacidadPasajeros = model.CapacidadPasajeros;
        vehicle.VelocidadMaximaKmh = model.VelocidadMaximaKmh;
        vehicle.KilometrajeActual = model.KilometrajeActual;
        vehicle.EstadoOperativo = model.EstadoOperativo.Trim().ToUpperInvariant();
        vehicle.FechaUltimaInspeccion = model.FechaUltimaInspeccion;
        vehicle.FechaUltimoMantenimiento = model.FechaUltimoMantenimiento;
        vehicle.ObservacionesMantenimiento = NormalizeOptional(model.ObservacionesMantenimiento);
        vehicle.Estado = model.Estado;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(vehicle));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var vehicle = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id, cancellationToken);
        if (vehicle == null)
        {
            return NotFound(new { message = "Vehiculo no encontrado." });
        }

        vehicle.Estado = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Vehiculo desactivado correctamente." });
    }

    [HttpGet("template")]
    public IActionResult Template()
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var bytes = _reportService.GenerateVehiclesImportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-vehiculos.xlsx");
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "Debe seleccionar un archivo Excel." });
        }

        var company = await _context.EmpresasTransporte.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync(cancellationToken);
        if (company == null)
        {
            return BadRequest(new { message = "No existe una empresa activa para asociar los vehiculos." });
        }

        var result = new BulkImportResultDto();
        var existingPlates = await _context.Vehiculos.Select(v => v.Placa).ToListAsync(cancellationToken);
        var existingCodes = await _context.Vehiculos.Select(v => v.CodigoInterno).ToListAsync(cancellationToken);
        var plates = new HashSet<string>(existingPlates, StringComparer.OrdinalIgnoreCase);
        var codes = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 5;

        for (var row = 6; row <= lastRow; row++)
        {
            if (RowIsEmpty(ws, row, 11))
            {
                continue;
            }

            result.TotalRows++;
            var plate = GetCellText(ws, row, 1).ToUpperInvariant();
            var code = GetCellText(ws, row, 2).ToUpperInvariant();
            var type = GetCellText(ws, row, 3).ToUpperInvariant();
            var brand = GetCellText(ws, row, 4);
            var model = GetCellText(ws, row, 5);
            var operativeStatus = GetCellText(ws, row, 10).ToUpperInvariant();
            var activeText = GetCellText(ws, row, 11);

            if (string.IsNullOrWhiteSpace(plate) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(type) ||
                !TryGetInt(ws.Cell(row, 7), out var capacity) || !TryGetDecimal(ws.Cell(row, 8), out var maxSpeed) ||
                !TryGetDecimal(ws.Cell(row, 9), out var mileage))
            {
                AddImportError(result, row, "Campos obligatorios incompletos o valores numericos invalidos.");
                continue;
            }

            if (!plates.Add(plate) || !codes.Add(code))
            {
                AddImportError(result, row, "Placa o codigo interno duplicados.");
                continue;
            }

            _context.Vehiculos.Add(new Vehiculo
            {
                IdEmpresa = company.IdEmpresa,
                Placa = plate,
                CodigoInterno = code,
                TipoVehiculo = type,
                Marca = NormalizeOptional(brand),
                Modelo = NormalizeOptional(model),
                Anio = TryGetInt(ws.Cell(row, 6), out var year) ? year : null,
                CapacidadPasajeros = capacity,
                VelocidadMaximaKmh = maxSpeed,
                KilometrajeActual = mileage,
                EstadoOperativo = string.IsNullOrWhiteSpace(operativeStatus) ? "OPERATIVO" : operativeStatus,
                Estado = !string.Equals(activeText, "INACTIVO", StringComparison.OrdinalIgnoreCase),
                FechaRegistro = DateTime.UtcNow
            });

            result.CreatedRows++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    private static VehicleManagementDto MapToDto(Vehiculo vehicle)
    {
        return new VehicleManagementDto
        {
            IdVehiculo = vehicle.IdVehiculo,
            Placa = vehicle.Placa,
            CodigoInterno = vehicle.CodigoInterno,
            TipoVehiculo = vehicle.TipoVehiculo,
            Marca = vehicle.Marca,
            Modelo = vehicle.Modelo,
            Anio = vehicle.Anio,
            CapacidadPasajeros = vehicle.CapacidadPasajeros,
            VelocidadMaximaKmh = vehicle.VelocidadMaximaKmh,
            KilometrajeActual = vehicle.KilometrajeActual,
            EstadoOperativo = vehicle.EstadoOperativo,
            FechaUltimaInspeccion = vehicle.FechaUltimaInspeccion,
            FechaUltimoMantenimiento = vehicle.FechaUltimoMantenimiento,
            ObservacionesMantenimiento = vehicle.ObservacionesMantenimiento,
            Estado = vehicle.Estado
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool RowIsEmpty(IXLWorksheet ws, int row, int columns)
    {
        return Enumerable.Range(1, columns).All(col => string.IsNullOrWhiteSpace(GetCellText(ws, row, col)));
    }

    private static string GetCellText(IXLWorksheet ws, int row, int col)
    {
        return ws.Cell(row, col).GetFormattedString().Trim();
    }

    private static bool TryGetInt(IXLCell cell, out int value)
    {
        if (cell.TryGetValue(out value))
        {
            return true;
        }

        return int.TryParse(cell.GetFormattedString(), out value);
    }

    private static bool TryGetDecimal(IXLCell cell, out decimal value)
    {
        if (cell.TryGetValue(out value))
        {
            return true;
        }

        return decimal.TryParse(cell.GetFormattedString(), out value);
    }

    private static void AddImportError(BulkImportResultDto result, int row, string error)
    {
        result.SkippedRows++;
        result.Errors.Add($"Fila {row}: {error}");
    }
}
