using System.Globalization;
using System.Text;

namespace SistemaEscolarWeb.Reports;

public class PdfGenerator
{
    private const double PageWidth = 842;
    private const double PageHeight = 595;
    private const double Margin = 28;
    private const double BottomMargin = 30;
    private const double HeaderHeight = 22;
    private const double LineHeight = 9;

    public byte[] Generate(ReporteExportData data)
    {
        var pages = BuildPages(data);
        return BuildDocument(pages);
    }

    private static List<string> BuildPages(ReporteExportData data)
    {
        var pages = new List<string>();
        var content = NewPage(data, true, out var y);
        var widths = CalculateWidths(data.Columnas.Count);

        foreach (var row in data.Filas)
        {
            var wrapped = WrapRow(row, widths);
            var rowHeight = Math.Max(18, wrapped.Max(c => c.Count) * LineHeight + 8);
            if (y - rowHeight < BottomMargin)
            {
                pages.Add(content.ToString());
                content = NewPage(data, false, out y);
            }

            DrawTableRow(content, y, widths, wrapped, false);
            y -= rowHeight;
        }

        pages.Add(content.ToString());
        return pages;
    }

    private static StringBuilder NewPage(ReporteExportData data, bool firstPage, out double y)
    {
        var content = new StringBuilder();
        y = PageHeight - Margin;

        Text(content, Margin, y, 17, data.Titulo, true);
        y -= 16;
        Text(content, Margin, y, 9, data.Descripcion, false);
        y -= 12;
        Text(content, Margin, y, 8, $"Generado: {DateTime.Now:dd-MM-yyyy HH:mm}", false);
        y -= 16;

        if (firstPage)
        {
            if (data.Filtros.Count > 0)
            {
                Text(content, Margin, y, 8, "Filtros aplicados: " + string.Join(" | ", data.Filtros.Select(f => $"{f.Nombre}: {f.Valor}")), false);
                y -= 12;
            }

            if (data.Resumen.Count > 0)
            {
                Text(content, Margin, y, 8, "Resumen: " + string.Join(" | ", data.Resumen.Select(f => $"{f.Nombre}: {f.Valor}")), false);
                y -= 14;
            }
        }

        DrawHeader(content, y, CalculateWidths(data.Columnas.Count), data.Columnas);
        y -= HeaderHeight;
        return content;
    }

    private static double[] CalculateWidths(int count)
    {
        count = Math.Max(1, count);
        var tableWidth = PageWidth - Margin * 2;
        var width = tableWidth / count;
        return Enumerable.Repeat(width, count).ToArray();
    }

    private static List<List<string>> WrapRow(IReadOnlyList<string> row, double[] widths)
    {
        var result = new List<List<string>>();
        for (var i = 0; i < widths.Length; i++)
            result.Add(Wrap(row.ElementAtOrDefault(i) ?? string.Empty, widths[i] - 8, 7));

        return result;
    }

    private static List<string> Wrap(string value, double width, double fontSize)
    {
        var maxChars = Math.Max(6, (int)Math.Floor(width / (fontSize * 0.46)));
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words.Length == 0 ? [string.Empty] : words)
        {
            if (word.Length > maxChars)
            {
                if (!string.IsNullOrWhiteSpace(current))
                {
                    lines.Add(current);
                    current = string.Empty;
                }

                for (var i = 0; i < word.Length; i += maxChars)
                    lines.Add(word.Substring(i, Math.Min(maxChars, word.Length - i)));
                continue;
            }

            var candidate = string.IsNullOrWhiteSpace(current) ? word : $"{current} {word}";
            if (candidate.Length > maxChars)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(current))
            lines.Add(current);

        return lines.Count == 0 ? ["-"] : lines;
    }

    private static void DrawHeader(StringBuilder content, double y, double[] widths, IReadOnlyList<string> columns)
    {
        Rect(content, Margin, y - HeaderHeight + 4, widths.Sum(), HeaderHeight, "0.04 0.22 0.52 rg", true);
        var x = Margin;
        for (var i = 0; i < widths.Length; i++)
        {
            Text(content, x + 4, y - 10, 7, columns.ElementAtOrDefault(i) ?? string.Empty, true, true);
            Line(content, x, y + 4, x, y - HeaderHeight + 4);
            x += widths[i];
        }

        Line(content, Margin + widths.Sum(), y + 4, Margin + widths.Sum(), y - HeaderHeight + 4);
    }

    private static void DrawTableRow(StringBuilder content, double y, double[] widths, IReadOnlyList<List<string>> cells, bool shade)
    {
        var height = Math.Max(18, cells.Max(c => c.Count) * LineHeight + 8);
        if (shade)
            Rect(content, Margin, y - height, widths.Sum(), height, "0.96 0.97 0.99 rg", true);

        Rect(content, Margin, y - height, widths.Sum(), height, "0.82 0.84 0.88 RG", false);
        var x = Margin;
        for (var i = 0; i < widths.Length; i++)
        {
            Line(content, x, y, x, y - height);
            var lineY = y - 10;
            foreach (var line in cells[i])
            {
                Text(content, x + 4, lineY, 7, line, false);
                lineY -= LineHeight;
            }

            x += widths[i];
        }

        Line(content, Margin + widths.Sum(), y, Margin + widths.Sum(), y - height);
    }

    private static void Text(StringBuilder content, double x, double y, double size, string text, bool bold, bool white = false)
    {
        content.Append("BT ");
        content.Append(white ? "1 1 1 rg " : "0 0 0 rg ");
        content.Append(bold ? "/F2 " : "/F1 ");
        content.Append(Format(size)).Append(" Tf ");
        content.Append(Format(x)).Append(' ').Append(Format(y)).Append(" Td ");
        content.Append(PdfString(text));
        content.Append(" Tj ET\n");
    }

    private static void Rect(StringBuilder content, double x, double y, double w, double h, string color, bool fill)
    {
        content.Append(color).Append(' ');
        content.Append(Format(x)).Append(' ').Append(Format(y)).Append(' ').Append(Format(w)).Append(' ').Append(Format(h)).Append(" re ");
        content.Append(fill ? "f\n" : "S\n");
    }

    private static void Line(StringBuilder content, double x1, double y1, double x2, double y2)
    {
        content.Append("0.82 0.84 0.88 RG 0.5 w ");
        content.Append(Format(x1)).Append(' ').Append(Format(y1)).Append(" m ");
        content.Append(Format(x2)).Append(' ').Append(Format(y2)).Append(" l S\n");
    }

    private static byte[] BuildDocument(IReadOnlyList<string> pageContents)
    {
        var objects = new List<byte[]>
        {
            Enc("<< /Type /Catalog /Pages 2 0 R >>"),
            Enc(""),
            Enc("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Enc("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
        };

        var pageObjectNumbers = new List<int>();
        foreach (var content in pageContents)
        {
            var stream = Enc(content);
            var contentNumber = objects.Count + 1;
            objects.Add(Enc($"<< /Length {stream.Length} >>\nstream\n{content}endstream"));
            var pageNumber = objects.Count + 1;
            pageObjectNumbers.Add(pageNumber);
            objects.Add(Enc($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Format(PageWidth)} {Format(PageHeight)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentNumber} 0 R >>"));
        }

        objects[1] = Enc($"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(n => $"{n} 0 R"))}] /Count {pageObjectNumbers.Count} >>");

        using var streamOutput = new MemoryStream();
        Write(streamOutput, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(streamOutput.Position);
            Write(streamOutput, $"{i + 1} 0 obj\n");
            streamOutput.Write(objects[i], 0, objects[i].Length);
            Write(streamOutput, "\nendobj\n");
        }

        var xref = streamOutput.Position;
        Write(streamOutput, $"xref\n0 {objects.Count + 1}\n");
        Write(streamOutput, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Write(streamOutput, $"{offset:0000000000} 00000 n \n");

        Write(streamOutput, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return streamOutput.ToArray();
    }

    private static string PdfString(string value)
    {
        var builder = new StringBuilder("(");
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '(':
                case ')':
                case '\\':
                    builder.Append('\\').Append(ch);
                    break;
                case '\r':
                case '\n':
                case '\t':
                    builder.Append(' ');
                    break;
                default:
                    if (ch is >= ' ' and <= '~')
                        builder.Append(ch);
                    else if (ch <= 255)
                        builder.Append('\\').Append(Convert.ToString(ch, 8).PadLeft(3, '0'));
                    else
                        builder.Append(RemoveDiacritics(ch.ToString()));
                    break;
            }
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark && ch <= 127)
                builder.Append(ch);
        }

        return builder.ToString();
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static byte[] Enc(string value) => Encoding.ASCII.GetBytes(value);
    private static void Write(Stream stream, string value)
    {
        var bytes = Enc(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
