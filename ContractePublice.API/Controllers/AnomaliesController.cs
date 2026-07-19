using ContractePublice.Infrastructure.Persistence;
using ContractePublice.Infrastructure.Services.Anomalies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractePublice.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnomaliesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnomaliesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? flagType,
        [FromQuery] string? severity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.AnomalyFlags
            .Include(f => f.Contract)
            .Include(f => f.ContractingAuthority)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(flagType))
            query = query.Where(f => f.FlagType == flagType);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(f => f.Severity == severity);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.DetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id, f.FlagType, f.Severity, f.Description, f.DetectedAt,
                Contract = f.Contract != null ? f.Contract.Title : null,
                Authority = f.ContractingAuthority != null ? f.ContractingAuthority.Name : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromServices] AnomalyDetectionService service, CancellationToken ct)
    {
        var created = await service.RunAsync(ct);
        return Ok(new { created });
    }
}
