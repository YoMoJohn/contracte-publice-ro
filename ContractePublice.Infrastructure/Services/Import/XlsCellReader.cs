using NPOI.SS.UserModel;

namespace ContractePublice.Infrastructure.Services.Import;

// Helpere comune de citire celule NPOI, reutilizate de toti parserii de rapoarte .xls.
// Evita apeluri la cell.ToString() (NPOI il ruteaza prin DataFormatter, care poate arunca
// exceptii cand lipseste dependenta manageda SkiaSharp) si parseaza explicit datele in
// formatul romanesc dd/MM/yyyy in loc sa lase interpretarea pe seama culturii implicite.
public static class XlsCellReader
{
    private static readonly string[] DateFormats = { "dd/MM/yyyy", "d/M/yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };

    public static Dictionary<string, int> ReadHeaders(IRow headerRow)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 0; col < headerRow.LastCellNum; col++)
        {
            var header = headerRow.GetCell(col)?.StringCellValue?.Trim();
            if (!string.IsNullOrEmpty(header) && !headers.ContainsKey(header))
                headers[header] = col;
        }
        return headers;
    }

    public static string? GetCellText(ICell? cell)
    {
        if (cell == null) return null;
        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue?.Trim(),
            CellType.Numeric => cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Formula => cell.CachedFormulaResultType == CellType.String
                ? cell.StringCellValue?.Trim()
                : cell.CachedFormulaResultType == CellType.Numeric
                    ? cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : null,
            _ => null
        };
    }

    public static string? GetString(IRow row, Dictionary<string, int> headers, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!headers.TryGetValue(key, out var col)) continue;
            var val = GetCellText(row.GetCell(col))?.Trim();
            if (!string.IsNullOrEmpty(val)) return val;
        }
        return null;
    }

    public static decimal? GetDecimal(IRow row, Dictionary<string, int> headers, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!headers.TryGetValue(key, out var col)) continue;
            var cell = row.GetCell(col);
            if (cell == null) continue;

            if (cell.CellType == CellType.Numeric)
                return (decimal)cell.NumericCellValue;

            var text = GetCellText(cell)?.Replace(",", ".");
            if (decimal.TryParse(text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result))
                return result;
        }
        return null;
    }

    public static DateTime? GetDate(IRow row, Dictionary<string, int> headers, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!headers.TryGetValue(key, out var col)) continue;
            var cell = row.GetCell(col);
            if (cell == null) continue;

            if (cell.CellType == CellType.Numeric)
                return cell.DateCellValue;

            var text = GetCellText(cell);
            if (string.IsNullOrWhiteSpace(text)) continue;

            if (DateTime.TryParseExact(text, DateFormats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var exact))
                return exact;

            if (DateTime.TryParse(text,
                System.Globalization.CultureInfo.GetCultureInfo("ro-RO"),
                System.Globalization.DateTimeStyles.None, out var date))
                return date;
        }
        return null;
    }

    public static bool? GetBool(IRow row, Dictionary<string, int> headers, params string[] keys)
    {
        var text = GetString(row, headers, keys);
        if (text == null) return null;
        return text.Trim().ToLowerInvariant() switch
        {
            "da" or "yes" or "true" or "1" => true,
            "nu" or "no" or "false" or "0" => false,
            _ => null
        };
    }
}
