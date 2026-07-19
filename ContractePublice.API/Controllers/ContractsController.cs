using ContractePublice.Infrastructure.Persistence;
using ContractePublice.Infrastructure.Services.News;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractePublice.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ContractsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? county,
        [FromQuery] string? cpv,
        [FromQuery] string? authority,
        [FromQuery] string? supplier,
        [FromQuery] string? sort = "date",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _db.Contracts
            .Include(c => c.ContractingAuthority)
            .Include(c => c.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(county))
            query = query.Where(c => c.County == county);

        if (!string.IsNullOrWhiteSpace(cpv))
            query = query.Where(c => c.CpvCode.StartsWith(cpv));

        if (!string.IsNullOrWhiteSpace(authority))
            query = query.Where(c => c.ContractingAuthority.Name == authority);

        if (!string.IsNullOrWhiteSpace(supplier))
            query = query.Where(c => c.Supplier != null && c.Supplier.Name == supplier);

        var total = await query.CountAsync();

        query = sort switch
        {
            "value" => query.OrderByDescending(c => c.AwardedValue),
            _ => query.OrderByDescending(c => c.PublishedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id, c.SeapId, c.Title, c.CpvCode, c.CpvDescription,
                c.AwardedValue, c.EstimatedValue, c.Currency, c.PublishedAt, c.County,
                c.ContractType, c.AwardProcedure, c.ReportSource,
                Authority = c.ContractingAuthority.Name,
                Supplier = c.Supplier != null ? c.Supplier.Name : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _db.Contracts
            .Include(c => c.ContractingAuthority)
            .Include(c => c.Supplier)
            .Include(c => c.AnomalyFlags)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id, c.SeapId, c.Title, c.CpvCode, c.CpvDescription,
                c.ContractType, c.AwardProcedure,
                c.EstimatedValue, c.AwardedValue, c.Currency,
                c.PublishedAt, c.AwardedAt, c.ContractStartDate, c.ContractEndDate, c.County,
                c.ContractNumber, c.MinValue, c.MaxValue, c.EuFunded, c.FundingType, c.ReportSource,
                c.ContractingAuthorityId,
                Authority = new { c.ContractingAuthority.Id, c.ContractingAuthority.Name, c.ContractingAuthority.CUI, c.ContractingAuthority.County },
                SupplierId = c.SupplierId,
                Supplier = c.Supplier != null ? new { c.Supplier.Id, c.Supplier.Name, c.Supplier.CUI, c.Supplier.County } : null,
                Anomalies = c.AnomalyFlags.Select(f => new { f.FlagType, f.Severity, f.Description })
            })
            .FirstOrDefaultAsync();

        if (contract == null) return NotFound();

        var authorityStats = await _db.Contracts
            .Where(c => c.ContractingAuthorityId == contract.ContractingAuthorityId)
            .GroupBy(c => 1)
            .Select(g => new { Count = g.Count(), TotalValue = g.Sum(c => c.AwardedValue) })
            .FirstOrDefaultAsync();

        var otherFromAuthority = await _db.Contracts
            .Where(c => c.ContractingAuthorityId == contract.ContractingAuthorityId && c.Id != id)
            .OrderByDescending(c => c.AwardedValue)
            .Take(5)
            .Select(c => new { c.Id, c.Title, c.AwardedValue, c.Currency, c.PublishedAt })
            .ToListAsync();

        var supplierCount = 0;
        var supplierTotalValue = 0m;
        var otherFromSupplier = new List<object>();
        if (contract.SupplierId != null)
        {
            var sStats = await _db.Contracts
                .Where(c => c.SupplierId == contract.SupplierId)
                .GroupBy(c => 1)
                .Select(g => new { Count = g.Count(), TotalValue = g.Sum(c => c.AwardedValue) })
                .FirstOrDefaultAsync();
            supplierCount = sStats?.Count ?? 0;
            supplierTotalValue = sStats?.TotalValue ?? 0;

            otherFromSupplier = (await _db.Contracts
                .Where(c => c.SupplierId == contract.SupplierId && c.Id != id)
                .OrderByDescending(c => c.AwardedValue)
                .Take(5)
                .Select(c => new { c.Id, c.Title, c.AwardedValue, c.Currency, c.PublishedAt })
                .ToListAsync())
                .Cast<object>()
                .ToList();
        }

        return Ok(new
        {
            contract.Id, contract.SeapId, contract.Title, contract.CpvCode, contract.CpvDescription,
            contract.ContractType, contract.AwardProcedure,
            contract.EstimatedValue, contract.AwardedValue, contract.Currency,
            contract.PublishedAt, contract.AwardedAt, contract.ContractStartDate, contract.ContractEndDate, contract.County,
            contract.ContractNumber, contract.MinValue, contract.MaxValue, contract.EuFunded, contract.FundingType, contract.ReportSource,
            Authority = new
            {
                contract.Authority.Id, contract.Authority.Name, contract.Authority.CUI, contract.Authority.County,
                TotalContracts = authorityStats?.Count ?? 0,
                TotalValue = authorityStats?.TotalValue ?? 0,
                OtherContracts = otherFromAuthority
            },
            Supplier = contract.Supplier == null ? null : new
            {
                contract.Supplier.Id, contract.Supplier.Name, contract.Supplier.CUI, contract.Supplier.County,
                TotalContracts = supplierCount,
                TotalValue = supplierTotalValue,
                OtherContracts = otherFromSupplier
            },
            contract.Anomalies
        });
    }

    [HttpGet("{id:int}/news")]
    public async Task<IActionResult> GetNews(int id, [FromServices] INewsSearchService newsService, CancellationToken ct)
    {
        var contract = await _db.Contracts
            .Include(c => c.ContractingAuthority)
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (contract == null) return NotFound();

        var query = contract.Supplier != null
            ? $"{contract.Supplier.Name} {contract.ContractingAuthority.Name}"
            : contract.ContractingAuthority.Name;

        var result = await newsService.SearchAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("stats/counties")]
    public async Task<IActionResult> StatsByCounty()
    {
        var stats = await _db.Contracts
            .GroupBy(c => c.County)
            .Select(g => new { County = g.Key, Count = g.Count(), TotalValue = g.Sum(c => c.AwardedValue) })
            .OrderByDescending(x => x.TotalValue)
            .ToListAsync();
        return Ok(stats);
    }

    [HttpGet("stats/top-suppliers")]
    public async Task<IActionResult> TopSuppliers([FromQuery] int top = 20)
    {
        var stats = await _db.Contracts
            .Where(c => c.SupplierId != null)
            .GroupBy(c => new { c.SupplierId, c.Supplier!.Name })
            .Select(g => new { Supplier = g.Key.Name, Count = g.Count(), TotalValue = g.Sum(c => c.AwardedValue) })
            .OrderByDescending(x => x.TotalValue)
            .Take(top)
            .ToListAsync();
        return Ok(stats);
    }

    [HttpGet("stats/top-authorities")]
    public async Task<IActionResult> TopAuthorities([FromQuery] int top = 20)
    {
        var stats = await _db.Contracts
            .GroupBy(c => new { c.ContractingAuthorityId, c.ContractingAuthority.Name })
            .Select(g => new { Authority = g.Key.Name, Count = g.Count(), TotalValue = g.Sum(c => c.AwardedValue) })
            .OrderByDescending(x => x.TotalValue)
            .Take(top)
            .ToListAsync();
        return Ok(stats);
    }
}
