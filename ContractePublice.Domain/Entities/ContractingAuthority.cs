namespace ContractePublice.Domain.Entities;

public class ContractingAuthority
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CUI { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
