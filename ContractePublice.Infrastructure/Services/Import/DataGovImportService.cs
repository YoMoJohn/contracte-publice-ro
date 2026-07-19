using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ContractePublice.Domain.Entities;
using ContractePublice.Domain.Enums;
using ContractePublice.Infrastructure.Persistence;
using ContractePublice.Infrastructure.Services.Import.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractePublice.Infrastructure.Services.Import;

public enum ReportKind
{
    DirectAcquisition,
    Contract
}

public record DiscoveredReport(string Name, string Url, int Year, int Month, ReportKind Kind);

public class DataGovImportService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly ILogger<DataGovImportService> _logger;

    // Primul an pentru care exista date publicate in acest set (achizitii directe, unitati sanitare publice)
    private const int FirstAvailableYear = 2023;

    private static readonly Dictionary<string, int> RomanianMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ianuarie"] = 1, ["februarie"] = 2, ["martie"] = 3, ["aprilie"] = 4,
        ["mai"] = 5, ["iunie"] = 6, ["iulie"] = 7, ["august"] = 8,
        ["septembrie"] = 9, ["octombrie"] = 10, ["noiembrie"] = 11, ["decembrie"] = 12
    };

    public DataGovImportService(AppDbContext db, HttpClient http, ILogger<DataGovImportService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    // Interoghează API-ul CKAN al data.gov.ro pentru a descoperi rapoartele lunare publicate
    // pentru un an, în loc să ținem hardcodate URL-uri (fiecare fișier are un id opac, iar
    // denumirile nu sunt perfect consistente de la o lună la alta).
    public async Task<List<DiscoveredReport>> DiscoverReportsAsync(int year, CancellationToken ct)
    {
        var found = new List<DiscoveredReport>();
        var apiUrl = $"https://data.gov.ro/api/3/action/package_show?id=achizitii-derulate-de-unitatile-sanitare-publice-in-anul-{year}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(apiUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Nu am putut contacta data.gov.ro pentru anul {Year}: {Error}", year, ex.Message);
            return found;
        }

        if (!response.IsSuccessStatusCode) return found;

        JsonDocument doc;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Raspuns invalid de la CKAN pentru anul {Year}: {Error}", year, ex.Message);
            return found;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("result", out var result)) return found;
            if (!result.TryGetProperty("resources", out var resources)) return found;

            foreach (var r in resources.EnumerateArray())
            {
                var resUrl = r.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(resUrl)) continue;

                var lower = resUrl.ToLowerInvariant();
                ReportKind? kind = lower.Contains("achizitii-directe") || lower.Contains("achizitii_directe")
                    ? ReportKind.DirectAcquisition
                    : lower.Contains("raport-contracte") || lower.Contains("raport_contracte")
                        ? ReportKind.Contract
                        : null;
                if (kind == null) continue;

                var name = r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var fileName = resUrl.Split('/')[^1];
                var month = ExtractMonth(fileName, name);

                found.Add(new DiscoveredReport(name, resUrl, year, month, kind.Value));
            }
        }

        return found;
    }

    private static int ExtractMonth(string fileName, string name)
    {
        var digitsMatch = Regex.Match(fileName, @"20\d{2}[_-]?(\d{2})");
        if (digitsMatch.Success)
        {
            var m = int.Parse(digitsMatch.Groups[1].Value);
            if (m is >= 1 and <= 12) return m;
        }

        foreach (var (monthName, monthNumber) in RomanianMonths)
        {
            if (fileName.Contains(monthName, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(monthName, StringComparison.OrdinalIgnoreCase))
                return monthNumber;
        }

        return 0;
    }

    // Descoperă și importă toate rapoartele disponibile, din toți anii publicați până acum.
    // Rapoartele deja importate cu succes sunt sărite, cu excepția celui mai recent — pe acela
    // îl reimportăm mereu, pentru că luna curentă se actualizează pe parcurs. Contractele sunt
    // oricum deduplicate după SeapId, deci re-importul e sigur (nu creează dubluri).
    public async Task ImportAllAvailableAsync(CancellationToken ct = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        var allReports = new List<DiscoveredReport>();

        for (var year = FirstAvailableYear; year <= currentYear; year++)
        {
            var reports = await DiscoverReportsAsync(year, ct);
            allReports.AddRange(reports);
        }

        if (allReports.Count == 0)
        {
            _logger.LogWarning("Nu am gasit niciun raport pe data.gov.ro.");
            return;
        }

        // reimportam mereu cel mai recent raport din fiecare tip, pentru ca luna curenta se
        // completeaza pe parcurs
        var latestByKind = allReports
            .GroupBy(r => r.Kind)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).First().Url);

        var alreadyImported = await _db.DataImportLogs
            .Where(l => l.Status == "Success")
            .Select(l => l.Source)
            .ToHashSetAsync(ct);

        foreach (var report in allReports.OrderBy(r => r.Year).ThenBy(r => r.Month))
        {
            var isLatestOfKind = latestByKind.TryGetValue(report.Kind, out var latestUrl) && latestUrl == report.Url;
            if (!isLatestOfKind && alreadyImported.Contains(report.Url.Truncate(500)))
                continue;

            _logger.LogInformation("Import raport {Kind}: {Name} ({Year}-{Month:00})", report.Kind, report.Name, report.Year, report.Month);
            await ImportAsync(report.Url, report.Kind);
        }
    }

    public async Task ImportAsync(string url, ReportKind kind = ReportKind.DirectAcquisition)
    {
        // Folosim CancellationToken.None — importul rulează până la capăt indiferent de HTTP
        var ct = CancellationToken.None;

        _logger.LogInformation("Începe importul din: {Url}", url);

        var log = new DataImportLog
        {
            Source = url.Truncate(500),
            ImportedAt = DateTime.UtcNow,
            Status = "InProgress"
        };
        _db.DataImportLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var rows = kind == ReportKind.Contract ? ContracteXlsParser.Parse(stream) : XlsParser.Parse(stream);
            _logger.LogInformation("Parsate {Count} rânduri din fișier ({Kind})", rows.Count, kind);

            // Preîncărcăm autoritățile și furnizorii existenți în memorie
            var existingAuthorities = await _db.ContractingAuthorities
                .ToDictionaryAsync(a => a.CUI, ct);
            var existingSuppliers = await _db.Suppliers
                .ToDictionaryAsync(s => s.CUI, ct);
            var existingSeapIds = await _db.Contracts
                .Select(c => c.SeapId)
                .ToHashSetAsync(ct);

            int imported = 0;
            int skipped = 0;
            const int batchSize = 500;
            var batch = new List<Contract>();

            foreach (var row in rows)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(row.Title) &&
                        string.IsNullOrWhiteSpace(row.SeapId))
                    { skipped++; continue; }

                    var seapId = (row.SeapId ?? $"GEN-{Guid.NewGuid():N}").Truncate(50);

                    if (existingSeapIds.Contains(seapId))
                    { skipped++; continue; }

                    existingSeapIds.Add(seapId);

                    // Autoritate
                    var authCui = (row.ContractingAuthorityCUI
                        ?? $"NOCUI-{Math.Abs(row.ContractingAuthorityName?.GetHashCode() ?? 0)}")
                        .Truncate(20);

                    if (!existingAuthorities.TryGetValue(authCui, out var authority))
                    {
                        authority = new ContractingAuthority
                        {
                            Name = (row.ContractingAuthorityName ?? "Necunoscut").Truncate(500),
                            CUI = authCui,
                            County = (row.ContractingAuthorityCounty ?? string.Empty).Truncate(100),
                            Type = string.Empty
                        };
                        _db.ContractingAuthorities.Add(authority);
                        await _db.SaveChangesAsync(ct);
                        existingAuthorities[authCui] = authority;
                    }

                    // Furnizor
                    Supplier? supplier = null;
                    if (!string.IsNullOrWhiteSpace(row.SupplierName))
                    {
                        var supCui = (row.SupplierCUI
                            ?? $"NOCUI-{Math.Abs(row.SupplierName.GetHashCode())}")
                            .Truncate(20);

                        if (!existingSuppliers.TryGetValue(supCui, out supplier))
                        {
                            supplier = new Supplier
                            {
                                Name = row.SupplierName.Truncate(500),
                                CUI = supCui,
                                County = (row.SupplierCounty ?? string.Empty).Truncate(100)
                            };
                            _db.Suppliers.Add(supplier);
                            await _db.SaveChangesAsync(ct);
                            existingSuppliers[supCui] = supplier;
                        }
                    }

                    batch.Add(new Contract
                    {
                        SeapId = seapId,
                        Title = (row.Title ?? "Fără titlu").Truncate(1000),
                        CpvCode = (row.CpvCode ?? string.Empty).Truncate(20),
                        CpvDescription = (row.CpvDescription ?? row.Description ?? string.Empty).Truncate(500),
                        ContractType = ParseContractType(row.ContractType),
                        AwardProcedure = ParseAwardProcedure(row.AwardProcedure),
                        EstimatedValue = row.EstimatedValue ?? 0,
                        AwardedValue = row.AwardedValue ?? 0,
                        Currency = (row.Currency ?? "RON").Truncate(10),
                        PublishedAt = row.PublishedAt ?? DateTime.UtcNow,
                        AwardedAt = row.AwardedAt,
                        ContractStartDate = row.ContractStartDate,
                        ContractEndDate = row.ContractEndDate,
                        County = (row.ContractingAuthorityCounty ?? string.Empty).Truncate(100),
                        ContractingAuthorityId = authority.Id,
                        SupplierId = supplier?.Id,
                        ContractNumber = row.ContractNumber?.Truncate(100),
                        MinValue = row.MinValue,
                        MaxValue = row.MaxValue,
                        EuFunded = row.EuFunded,
                        FundingType = row.FundingType?.Truncate(200),
                        ReportSource = kind == ReportKind.Contract ? "Contract" : "AchizitieDirecta"
                    });

                    if (batch.Count >= batchSize)
                    {
                        _db.Contracts.AddRange(batch);
                        await _db.SaveChangesAsync(ct);
                        imported += batch.Count;
                        _logger.LogInformation("Batch salvat: {Count} contracte", imported);
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Eroare rând: {Error}", ex.Message);
                    skipped++;
                }
            }

            // Salvăm ce a mai rămas
            if (batch.Count > 0)
            {
                _db.Contracts.AddRange(batch);
                await _db.SaveChangesAsync(ct);
                imported += batch.Count;
            }

            log.Status = "Success";
            log.RecordsImported = imported;
            log.Notes = $"Importate: {imported}, Sărite: {skipped}";
            _logger.LogInformation("Import finalizat. Importate: {Imported}, Sărite: {Skipped}", imported, skipped);
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.Notes = ex.Message;
            _logger.LogError(ex, "Import eșuat din: {Url}", url);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static ContractType ParseContractType(string? value) => value?.ToLower() switch
    {
        var v when v?.Contains("lucrari") == true || v?.Contains("lucrări") == true => ContractType.Lucrari,
        var v when v?.Contains("servicii") == true => ContractType.Servicii,
        var v when v?.Contains("produse") == true || v?.Contains("furnizare") == true => ContractType.Produse,
        _ => ContractType.Servicii
    };

    private static AwardProcedure ParseAwardProcedure(string? value) => value?.ToLower() switch
    {
        var v when v?.Contains("deschis") == true => AwardProcedure.LicitatieDeschia,
        var v when v?.Contains("restrâns") == true || v?.Contains("restrans") == true => AwardProcedure.LicitatieRestransa,
        var v when v?.Contains("simplificat") == true => AwardProcedure.ProceduraSimplificata,
        var v when v?.Contains("direct") == true || v?.Contains("cumpărare") == true => AwardProcedure.AchizitieDirecta,
        var v when v?.Contains("negociere") == true => AwardProcedure.Negociere,
        _ => AwardProcedure.Alta
    };
}

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
