using ContractePublice.Domain.Enums;

namespace ContractePublice.Domain.Entities;

public class Contract
{
    public int Id { get; set; }
    public string SeapId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CpvCode { get; set; } = string.Empty;
    public string CpvDescription { get; set; } = string.Empty;
    public ContractType ContractType { get; set; }
    public AwardProcedure AwardProcedure { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal AwardedValue { get; set; }
    public string Currency { get; set; } = "RON";
    public DateTime PublishedAt { get; set; }
    public DateTime? AwardedAt { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string County { get; set; } = string.Empty;

    public int ContractingAuthorityId { get; set; }
    public ContractingAuthority ContractingAuthority { get; set; } = null!;

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public ICollection<AnomalyFlag> AnomalyFlags { get; set; } = new List<AnomalyFlag>();
}
