using ContractePublice.Infrastructure.Services.Import.Models;
using NPOI.HSSF.UserModel;
using static ContractePublice.Infrastructure.Services.Import.XlsCellReader;

namespace ContractePublice.Infrastructure.Services.Import;

// Parser pentru rapoartele lunare "Contracte" de pe data.gov.ro — mult mai bogate decat cele de
// achizitii directe (includ tip procedura, numar/data contract, valori minime/maxime, fonduri UE).
public static class ContracteXlsParser
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

            var contractNumber = GetString(row, headers, "Numar contract");
            var announcementNumber = GetString(row, headers, "Numar anunt atribuire");

            // Prefixat ca sa nu se poata suprapune accidental cu SeapId-urile din raportul de
            // achizitii directe (care folosesc alt format de identificator).
            var seapId = contractNumber != null ? $"CN-{contractNumber}" : announcementNumber != null ? $"AN-{announcementNumber}" : null;

            var item = new ContractImportRow
            {
                SeapId = seapId,
                ContractNumber = contractNumber,
                Title = GetString(row, headers, "Titlu contract"),
                ContractingAuthorityName = GetString(row, headers, "Autoritate contractanta"),
                ContractingAuthorityCUI = GetString(row, headers, "CUI autoritate contractanta"),
                SupplierName = GetString(row, headers, "Castigator"),
                SupplierCUI = GetString(row, headers, "CUI castigator"),
                SupplierCounty = GetString(row, headers, "Castigator localitate"),
                CpvCode = GetString(row, headers, "Cod CPV", "CPV Code ID"),
                Description = GetString(row, headers, "Descriere succinta"),
                ContractType = GetString(row, headers, "Tip contract"),
                AwardProcedure = GetString(row, headers, "Tip procedura"),
                EstimatedValue = GetDecimal(row, headers, "Valoare estimata participare"),
                AwardedValue = GetDecimal(row, headers, "Valoare contract"),
                MinValue = GetDecimal(row, headers, "Valoare minima contract"),
                MaxValue = GetDecimal(row, headers, "Valoare maxima contract"),
                Currency = GetString(row, headers, "Moneda") ?? "RON",
                PublishedAt = GetDate(row, headers, "Data anunt participare", "Data anunt atribuire"),
                AwardedAt = GetDate(row, headers, "Data contract", "Data anunt atribuire"),
                EuFunded = GetBool(row, headers, "Fond european", "Fonduri comunitare?"),
                FundingType = GetString(row, headers, "Tip finantare", "Modalitati finantare"),
            };

            if (string.IsNullOrWhiteSpace(item.Title) && string.IsNullOrWhiteSpace(item.SeapId))
                continue;

            results.Add(item);
        }

        return results;
    }
}
