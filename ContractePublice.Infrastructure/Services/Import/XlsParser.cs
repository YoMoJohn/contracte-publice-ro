using ContractePublice.Infrastructure.Services.Import.Models;
using NPOI.HSSF.UserModel;
using static ContractePublice.Infrastructure.Services.Import.XlsCellReader;

namespace ContractePublice.Infrastructure.Services.Import;

// Parser pentru rapoartele lunare "Achizitii directe" de pe data.gov.ro.
public static class XlsParser
{
    public static List<ContractImportRow> Parse(Stream stream)
    {
        var results = new List<ContractImportRow>();

        var workbook = new HSSFWorkbook(stream);
        var ws = workbook.GetSheetAt(0);
        if (ws == null) return results;

        var headerRow = ws.GetRow(0);
        if (headerRow == null) return results;

        var headers = ReadHeaders(headerRow);

        for (int rowIdx = 1; rowIdx <= ws.LastRowNum; rowIdx++)
        {
            var row = ws.GetRow(rowIdx);
            if (row == null) continue;

            var item = new ContractImportRow
            {
                SeapId = GetString(row, headers,
                    "Numar achizitie directa ",
                    "Numar achizitie directa",
                    "Numar anunt"),
                Title = GetString(row, headers,
                    "Denumire achizitie directa",
                    "Denumire contract",
                    "Obiect contract"),
                ContractingAuthorityName = GetString(row, headers,
                    "Autoritate contractanta"),
                ContractingAuthorityCUI = GetString(row, headers,
                    "CUI autoritate contractanta",
                    "CIF autoritate"),
                ContractingAuthorityCounty = GetString(row, headers,
                    "Judet autoritate contractanta",
                    "Localitate ofertant castigator"),
                SupplierName = GetString(row, headers,
                    "Ofertant castigator",
                    "Denumire ofertant"),
                SupplierCUI = GetString(row, headers,
                    "CUI ofertant castigator",
                    "CIF ofertant"),
                CpvCode = GetString(row, headers,
                    "Cod CPV",
                    "CPV Cod ID"),
                CpvDescription = GetString(row, headers,
                    "Descriere CPV",
                    "Descriere achizitie directa"),
                ContractType = "Achizitie Directa",
                AwardProcedure = GetString(row, headers,
                    "Tip procedura"),
                EstimatedValue = GetDecimal(row, headers,
                    "Valoare estimata I",
                    "Valoare estimata II",
                    "Valoare estimata"),
                AwardedValue = GetDecimal(row, headers,
                    "Valoare atribuita"),
                Currency = GetString(row, headers,
                    "Moneda valoare atribuita",
                    "Moneda valoare estimata I") ?? "RON",
                PublishedAt = GetDate(row, headers,
                    "Data publicare"),
                AwardedAt = GetDate(row, headers,
                    "Data finalizare",
                    "Data atribuire"),
            };

            if (string.IsNullOrWhiteSpace(item.Title) &&
                string.IsNullOrWhiteSpace(item.SeapId))
                continue;

            results.Add(item);
        }

        return results;
    }
}
