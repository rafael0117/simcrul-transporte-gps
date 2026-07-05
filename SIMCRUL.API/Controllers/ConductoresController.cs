using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Common.Constants;
using SIMCRUL.Common.DTOs.Drivers;
using SIMCRUL.Common.DTOs.Shared;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;

namespace SIMCRUL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConductoresController : MaintenanceApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IReportService _reportService;

    public ConductoresController(ApplicationDbContext context, IReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var query = _context.Conductores
            .Include(c => c.Usuario)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.Estado);
        }

        var drivers = await query
            .OrderBy(c => c.Apellidos)
            .ThenBy(c => c.Nombres)
            .ToListAsync(cancellationToken);

        return Ok(drivers.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var driver = await _context.Conductores
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);

        return driver == null
            ? NotFound(new { message = "Conductor no encontrado." })
            : Ok(MapToDto(driver));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ConductorManagementDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var validation = await ValidateDriverAsync(model, null, cancellationToken);
        if (validation != null)
        {
            return BadRequest(new { message = validation });
        }

        var company = await _context.EmpresasTransporte.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync(cancellationToken);
        if (company == null)
        {
            return BadRequest(new { message = "No existe una empresa activa para asociar el conductor." });
        }

        var driver = new Conductor
        {
            IdEmpresa = company.IdEmpresa,
            IdUsuario = model.IdUsuario,
            Nombres = model.Nombres.Trim(),
            Apellidos = model.Apellidos.Trim(),
            Dni = model.Dni.Trim(),
            NumeroLicencia = model.NumeroLicencia.Trim().ToUpperInvariant(),
            CategoriaLicencia = model.CategoriaLicencia.Trim().ToUpperInvariant(),
            FechaVencimientoLicencia = model.FechaVencimientoLicencia.Date,
            Telefono = NormalizeOptional(model.Telefono),
            Estado = model.Estado,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Conductores.Add(driver);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = driver.IdConductor }, MapToDto(driver));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ConductorManagementDto model, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        if (id != model.IdConductor)
        {
            return BadRequest(new { message = "El identificador del conductor no coincide." });
        }

        var driver = await _context.Conductores.FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);
        if (driver == null)
        {
            return NotFound(new { message = "Conductor no encontrado." });
        }

        var validation = await ValidateDriverAsync(model, id, cancellationToken);
        if (validation != null)
        {
            return BadRequest(new { message = validation });
        }

        driver.IdUsuario = model.IdUsuario;
        driver.Nombres = model.Nombres.Trim();
        driver.Apellidos = model.Apellidos.Trim();
        driver.Dni = model.Dni.Trim();
        driver.NumeroLicencia = model.NumeroLicencia.Trim().ToUpperInvariant();
        driver.CategoriaLicencia = model.CategoriaLicencia.Trim().ToUpperInvariant();
        driver.FechaVencimientoLicencia = model.FechaVencimientoLicencia.Date;
        driver.Telefono = NormalizeOptional(model.Telefono);
        driver.Estado = model.Estado;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(driver));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var driver = await _context.Conductores.FirstOrDefaultAsync(c => c.IdConductor == id, cancellationToken);
        if (driver == null)
        {
            return NotFound(new { message = "Conductor no encontrado." });
        }

        driver.Estado = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Conductor desactivado correctamente." });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken = default)
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var bytes = await _reportService.GenerateDriversExcelReportAsync(cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"conductores-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet("template")]
    public IActionResult Template()
    {
        if (!UserHasAnyRole(Roles.Administrador))
        {
            return Forbid();
        }

        var bytes = _reportService.GenerateDriversImportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-conductores.xlsx");
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
            return BadRequest(new { message = "No existe una empresa activa para asociar los conductores." });
        }

        var result = new BulkImportResultDto();
        var existingDnis = await _context.Conductores.Select(c => c.Dni).ToListAsync(cancellationToken);
        var existingLicenses = await _context.Conductores.Select(c => c.NumeroLicencia).ToListAsync(cancellationToken);
        var dnis = new HashSet<string>(existingDnis, StringComparer.OrdinalIgnoreCase);
        var licenses = new HashSet<string>(existingLicenses, StringComparer.OrdinalIgnoreCase);

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 5;

        for (var row = 6; row <= lastRow; row++)
        {
            if (RowIsEmpty(ws, row, 8))
            {
                continue;
            }

            result.TotalRows++;
            var names = GetCellText(ws, row, 1);
            var lastNames = GetCellText(ws, row, 2);
            var dni = GetCellText(ws, row, 3);
            var license = GetCellText(ws, row, 4).ToUpperInvariant();
            var category = GetCellText(ws, row, 5).ToUpperInvariant();
            var phone = GetCellText(ws, row, 7);
            var activeText = GetCellText(ws, row, 8);

            if (string.IsNullOrWhiteSpace(names) || string.IsNullOrWhiteSpace(lastNames) || string.IsNullOrWhiteSpace(dni) ||
                string.IsNullOrWhiteSpace(license) || string.IsNullOrWhiteSpace(category) || !TryGetDate(ws.Cell(row, 6), out var expiration))
            {
                AddImportError(result, row, "Campos obligatorios incompletos o fecha invalida.");
                continue;
            }

            if (!dnis.Add(dni) || !licenses.Add(license))
            {
                AddImportError(result, row, "DNI o licencia duplicados.");
                continue;
            }

            _context.Conductores.Add(new Conductor
            {
                IdEmpresa = company.IdEmpresa,
                Nombres = names,
                Apellidos = lastNames,
                Dni = dni,
                NumeroLicencia = license,
                CategoriaLicencia = category,
                FechaVencimientoLicencia = expiration.Date,
                Telefono = NormalizeOptional(phone),
                Estado = !string.Equals(activeText, "INACTIVO", StringComparison.OrdinalIgnoreCase),
                FechaRegistro = DateTime.UtcNow
            });

            result.CreatedRows++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    private async Task<string?> ValidateDriverAsync(ConductorManagementDto model, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Nombres) || string.IsNullOrWhiteSpace(model.Apellidos) ||
            string.IsNullOrWhiteSpace(model.Dni) || string.IsNullOrWhiteSpace(model.NumeroLicencia) ||
            string.IsNullOrWhiteSpace(model.CategoriaLicencia) || model.FechaVencimientoLicencia == default)
        {
            return "Complete los campos obligatorios del conductor.";
        }

        if (await _context.Conductores.AnyAsync(c => (!currentId.HasValue || c.IdConductor != currentId.Value) && c.Dni == model.Dni.Trim(), cancellationToken))
        {
            return "Ya existe un conductor con ese DNI.";
        }

        var license = model.NumeroLicencia.Trim().ToUpperInvariant();
        if (await _context.Conductores.AnyAsync(c => (!currentId.HasValue || c.IdConductor != currentId.Value) && c.NumeroLicencia == license, cancellationToken))
        {
            return "Ya existe un conductor con esa licencia.";
        }

        return null;
    }

    private static ConductorManagementDto MapToDto(Conductor driver)
    {
        return new ConductorManagementDto
        {
            IdConductor = driver.IdConductor,
            IdEmpresa = driver.IdEmpresa,
            IdUsuario = driver.IdUsuario,
            Nombres = driver.Nombres,
            Apellidos = driver.Apellidos,
            Dni = driver.Dni,
            NumeroLicencia = driver.NumeroLicencia,
            CategoriaLicencia = driver.CategoriaLicencia,
            FechaVencimientoLicencia = driver.FechaVencimientoLicencia,
            Telefono = driver.Telefono,
            Estado = driver.Estado,
            FechaRegistro = driver.FechaRegistro,
            UsernameUsuario = driver.Usuario?.Username,
            NombreUsuario = driver.Usuario == null ? null : (driver.Usuario.Nombres + " " + driver.Usuario.Apellidos).Trim()
        };
    }

    private static bool RowIsEmpty(IXLWorksheet ws, int row, int columns)
    {
        return Enumerable.Range(1, columns).All(col => string.IsNullOrWhiteSpace(GetCellText(ws, row, col)));
    }

    private static string GetCellText(IXLWorksheet ws, int row, int col)
    {
        return ws.Cell(row, col).GetFormattedString().Trim();
    }

    private static bool TryGetDate(IXLCell cell, out DateTime value)
    {
        if (cell.TryGetValue(out value))
        {
            return true;
        }

        return DateTime.TryParse(cell.GetFormattedString(), out value);
    }

    private static void AddImportError(BulkImportResultDto result, int row, string error)
    {
        result.SkippedRows++;
        result.Errors.Add($"Fila {row}: {error}");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
