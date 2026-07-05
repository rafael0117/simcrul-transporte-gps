namespace SIMCRUL.Common.DTOs.Shared;

public class BulkImportResultDto
{
    public int TotalRows { get; set; }
    public int CreatedRows { get; set; }
    public int SkippedRows { get; set; }
    public List<string> Errors { get; set; } = [];
}
