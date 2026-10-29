using OfficeOpenXml;

namespace Esma;

public static class ExcelReader
{
    /// <summary>
    /// Читает (защищённый паролем) xlsx: колонка A — имя, колонка B — текст.
    /// Пустые строки пропускаются.
    /// </summary>
    public static List<(string Name, string Text)> Read(string filePath, string password)
    {
        // Копируем в память с FileShare.ReadWrite, чтобы читать файл, даже если он открыт в Excel
        using var ms = new MemoryStream();
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            fs.CopyTo(ms);
        ms.Position = 0;

        using var package = string.IsNullOrEmpty(password)
            ? new ExcelPackage(ms)
            : new ExcelPackage(ms, password);

        var rows = new List<(string, string)>();
        var sheet = package.Workbook.Worksheets.FirstOrDefault();
        if (sheet?.Dimension == null)
            return rows;

        for (int r = 1; r <= sheet.Dimension.End.Row; r++)
        {
            var name = sheet.Cells[r, 1].Value?.ToString();
            var text = sheet.Cells[r, 2].Value?.ToString();
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text))
                rows.Add((name, text));
        }
        return rows;
    }
}
