namespace ContractePublice.Domain.Entities;

public class AnomalyFlag
{
    public int Id { get; set; }
    public string FlagType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public int? ContractingAuthorityId { get; set; }
    public ContractingAuthority? ContractingAuthority { get; set; }
}
