namespace ContractePublice.Infrastructure.Services.Import.Models;

public class ContractImportRow
{
    public string? SeapId { get; set; }
    public string? Title { get; set; }
    public string? ContractingAuthorityName { get; set; }
    public string? ContractingAuthorityCUI { get; set; }
    public string? ContractingAuthorityCounty { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCUI { get; set; }
    public string? CpvCode { get; set; }
    public string? CpvDescription { get; set; }
    public string? ContractType { get; set; }
    public string? AwardProcedure { get; set; }
    public decimal? EstimatedValue { get; set; }
    public decimal? AwardedValue { get; set; }
    public string? Currency { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? AwardedAt { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? SupplierCounty { get; set; }

    // Câmpuri disponibile doar în raportul "Contracte" (mai bogat decât cel de achiziții directe)
    public string? ContractNumber { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool? EuFunded { get; set; }
    public string? FundingType { get; set; }
    public string? Description { get; set; }
}
