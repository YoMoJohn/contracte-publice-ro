namespace ContractePublice.Domain.Entities;

public class DataImportLog
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public int RecordsImported { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
