namespace SIMCRUL.Business.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateAlertsPdfReportAsync(DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateTripsExcelReportAsync(DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default);
}
