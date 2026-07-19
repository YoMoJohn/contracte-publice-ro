using ContractePublice.Domain.Entities;
using ContractePublice.Domain.Enums;
using ContractePublice.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractePublice.Infrastructure.Services.Anomalies;

public class AnomalyDetectionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AnomalyDetectionService> _logger;

    // Prag legal aproximativ pentru achiziție directă (RON, fără TVA) — verifică valoarea curentă în legislație
    private const decimal DirectAwardThreshold = 270_120m;
    private const decimal OverrunRatio = 1.2m;
    private const decimal DominanceShare = 0.5m;
    private const int DominanceMinContracts = 5;

    public AnomalyDetectionService(AppDbContext db, ILogger<AnomalyDetectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var created = 0;
        created += await DetectValueOverrunsAsync(ct);
        created += await DetectPossibleContractSplittingAsync(ct);
        created += await DetectSupplierDominanceAsync(ct);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Detectare anomalii finalizată: {Count} flag-uri noi", created);
        return created;
    }

    private async Task<int> DetectValueOverrunsAsync(CancellationToken ct)
    {
        const string flagType = "ValueOverrun";

        var flaggedContractIds = await _db.AnomalyFlags
            .Where(f => f.FlagType == flagType && f.ContractId != null)
            .Select(f => f.ContractId!.Value)
            .ToHashSetAsync(ct);

        var candidates = await _db.Contracts
            .Where(c => c.EstimatedValue > 0 && c.AwardedValue > c.EstimatedValue * OverrunRatio)
            .Select(c => new { c.Id, c.EstimatedValue, c.AwardedValue })
            .ToListAsync(ct);

        var added = 0;
        foreach (var c in candidates)
        {
            if (flaggedContractIds.Contains(c.Id)) continue;

            var overrunPct = (c.AwardedValue / c.EstimatedValue - 1) * 100;
            _db.AnomalyFlags.Add(new AnomalyFlag
            {
                FlagType = flagType,
                Severity = overrunPct >= 100 ? "Critical" : overrunPct >= 50 ? "Warning" : "Info",
                Description = $"Valoarea atribuită ({c.AwardedValue:N2}) depășește valoarea estimată " +
                               $"({c.EstimatedValue:N2}) cu {overrunPct:N0}%.",
                ContractId = c.Id
            });
            added++;
        }

        return added;
    }

    private async Task<int> DetectPossibleContractSplittingAsync(CancellationToken ct)
    {
        const string flagType = "PossibleSplitting";

        var existingKeys = await GetExistingDedupeKeysAsync(flagType, ct);

        var groups = await _db.Contracts
            .Where(c => c.AwardProcedure == AwardProcedure.AchizitieDirecta && c.SupplierId != null)
            .GroupBy(c => new
            {
                c.ContractingAuthorityId,
                c.SupplierId,
                c.PublishedAt.Year,
                c.PublishedAt.Month
            })
            .Select(g => new
            {
                g.Key.ContractingAuthorityId,
                SupplierId = g.Key.SupplierId!.Value,
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                Total = g.Sum(c => c.AwardedValue)
            })
            .Where(g => g.Count >= 2 && g.Total > DirectAwardThreshold)
            .ToListAsync(ct);

        if (groups.Count == 0) return 0;

        var authorityNames = await _db.ContractingAuthorities.ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var supplierNames = await _db.Suppliers.ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var added = 0;
        foreach (var g in groups)
        {
            var key = $"{g.ContractingAuthorityId}-{g.SupplierId}-{g.Year}-{g.Month:00}";
            if (!existingKeys.Add(key)) continue;

            var authorityName = authorityNames.GetValueOrDefault(g.ContractingAuthorityId, "necunoscută");
            var supplierName = supplierNames.GetValueOrDefault(g.SupplierId, "necunoscut");

            _db.AnomalyFlags.Add(new AnomalyFlag
            {
                FlagType = flagType,
                Severity = "Warning",
                Description = $"[dedupe:{key}] Autoritatea '{authorityName}' a atribuit {g.Count} achiziții " +
                               $"directe furnizorului '{supplierName}' în {g.Month:00}/{g.Year}, totalizând " +
                               $"{g.Total:N2} RON, peste pragul de achiziție directă ({DirectAwardThreshold:N0} RON). " +
                               "Posibilă fragmentare a contractului pentru evitarea licitației.",
                ContractingAuthorityId = g.ContractingAuthorityId
            });
            added++;
        }

        return added;
    }

    private async Task<int> DetectSupplierDominanceAsync(CancellationToken ct)
    {
        const string flagType = "SupplierDominance";

        var existingKeys = await GetExistingDedupeKeysAsync(flagType, ct);

        var authorityTotals = await _db.Contracts
            .Where(c => c.SupplierId != null)
            .GroupBy(c => c.ContractingAuthorityId)
            .Select(g => new { AuthorityId = g.Key, Count = g.Count(), Total = g.Sum(c => c.AwardedValue) })
            .Where(g => g.Count >= DominanceMinContracts && g.Total > 0)
            .ToListAsync(ct);

        if (authorityTotals.Count == 0) return 0;

        var authorityIds = authorityTotals.Select(a => a.AuthorityId).ToList();

        var supplierBreakdown = await _db.Contracts
            .Where(c => c.SupplierId != null && authorityIds.Contains(c.ContractingAuthorityId))
            .GroupBy(c => new { c.ContractingAuthorityId, c.SupplierId })
            .Select(g => new
            {
                g.Key.ContractingAuthorityId,
                SupplierId = g.Key.SupplierId!.Value,
                Count = g.Count(),
                Total = g.Sum(c => c.AwardedValue)
            })
            .Where(g => g.Count >= 2)
            .ToListAsync(ct);

        var authorityNames = await _db.ContractingAuthorities.ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var supplierNames = await _db.Suppliers.ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var totalsByAuthority = authorityTotals.ToDictionary(a => a.AuthorityId, a => a.Total);

        var added = 0;
        foreach (var s in supplierBreakdown)
        {
            if (!totalsByAuthority.TryGetValue(s.ContractingAuthorityId, out var authorityTotal)) continue;

            var share = s.Total / authorityTotal;
            if (share < DominanceShare) continue;

            var key = $"{s.ContractingAuthorityId}-{s.SupplierId}";
            if (!existingKeys.Add(key)) continue;

            var authorityName = authorityNames.GetValueOrDefault(s.ContractingAuthorityId, "necunoscută");
            var supplierName = supplierNames.GetValueOrDefault(s.SupplierId, "necunoscut");

            _db.AnomalyFlags.Add(new AnomalyFlag
            {
                FlagType = flagType,
                Severity = share >= 0.8m ? "Critical" : "Warning",
                Description = $"[dedupe:{key}] Furnizorul '{supplierName}' deține {share:P0} din valoarea totală " +
                               $"a contractelor atribuite de '{authorityName}' ({s.Total:N2} din {authorityTotal:N2} RON, " +
                               $"{s.Count} contracte).",
                ContractingAuthorityId = s.ContractingAuthorityId
            });
            added++;
        }

        return added;
    }

    private async Task<HashSet<string>> GetExistingDedupeKeysAsync(string flagType, CancellationToken ct)
    {
        var descriptions = await _db.AnomalyFlags
            .Where(f => f.FlagType == flagType)
            .Select(f => f.Description)
            .ToListAsync(ct);

        var keys = new HashSet<string>();
        foreach (var d in descriptions)
        {
            var start = d.IndexOf("[dedupe:", StringComparison.Ordinal);
            if (start < 0) continue;
            var end = d.IndexOf(']', start);
            if (end < 0) continue;
            keys.Add(d[(start + 8)..end]);
        }
        return keys;
    }
}
