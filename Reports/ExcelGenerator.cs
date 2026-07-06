using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace SistemaEscolarWeb.Reports;

public class ExcelGenerator
{
    public byte[] Generate(ReporteExportData data)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", ContentTypes());
            AddEntry(archive, "_rels/.rels", PackageRelationships());
            AddEntry(archive, "xl/workbook.xml", Workbook());
            AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            AddEntry(archive, "xl/styles.xml", Styles());
            AddEntry(archive, "xl/worksheets/sheet1.xml", Worksheet(data));
        }

        return stream.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Worksheet(ReporteExportData data)
    {
        var columnCount = Math.Max(1, data.Columnas.Count);
        var rowIndex = 1;
        var tableHeaderRow = 0;
        var builder = new StringBuilder();
        var mergeCells = string.Empty;
        var totalRows = 3
            + (data.Filtros.Count > 0 ? data.Filtros.Count + 2 : 0)
            + (data.Resumen.Count > 0 ? data.Resumen.Count + 2 : 0)
            + 2
            + data.Filas.Count;

        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""");
        builder.Append(CultureInfo.InvariantCulture, $"<dimension ref=\"A1:{ColumnName(columnCount)}{Math.Max(1, totalRows)}\"/>");
        builder.Append("""<sheetViews><sheetView workbookViewId="0"/></sheetViews><sheetFormatPr defaultRowHeight="15"/>""");
        builder.Append(CalculateColumns(data, columnCount));
        builder.Append("""<sheetData>""");
        AppendRow(builder, rowIndex++, [Cell(data.Titulo, 1)]);
        AppendRow(builder, rowIndex++, [Cell(data.Descripcion, 2)]);
        AppendRow(builder, rowIndex++, [Cell($"Generado: {DateTime.Now:dd-MM-yyyy HH:mm}", 2)]);

        if (data.Filtros.Count > 0)
        {
            AppendRow(builder, rowIndex++, []);
            AppendRow(builder, rowIndex++, [Cell("Filtros aplicados", 3)]);
            foreach (var filtro in data.Filtros)
                AppendRow(builder, rowIndex++, [Cell(filtro.Nombre, 3), Cell(filtro.Valor, 0)]);
        }

        if (data.Resumen.Count > 0)
        {
            AppendRow(builder, rowIndex++, []);
            AppendRow(builder, rowIndex++, [Cell("Resumen", 3)]);
            foreach (var item in data.Resumen)
                AppendRow(builder, rowIndex++, [Cell(item.Nombre, 3), Cell(item.Valor, 0)]);
        }

        AppendRow(builder, rowIndex++, []);
        tableHeaderRow = rowIndex;
        AppendRow(builder, rowIndex++, data.Columnas.Select(c => Cell(c, 4)).ToList());

        foreach (var fila in data.Filas)
            AppendRow(builder, rowIndex++, fila.Select(v => Cell(v, 0)).ToList());

        builder.Append("""</sheetData>""");
        if (columnCount > 1)
        {
            var mergeBuilder = new StringBuilder("<mergeCells count=\"2\">");
            mergeBuilder.Append(CultureInfo.InvariantCulture, $"<mergeCell ref=\"A1:{ColumnName(columnCount)}1\"/>");
            mergeBuilder.Append(CultureInfo.InvariantCulture, $"<mergeCell ref=\"A2:{ColumnName(columnCount)}2\"/>");
            mergeBuilder.Append("</mergeCells>");
            mergeCells = mergeBuilder.ToString();
        }

        if (data.Filas.Count > 0)
            builder.Append(CultureInfo.InvariantCulture, $"<autoFilter ref=\"A{tableHeaderRow}:{ColumnName(columnCount)}{rowIndex - 1}\"/>");

        builder.Append(mergeCells);
        builder.Append("""<pageMargins left="0.25" right="0.25" top="0.5" bottom="0.5" header="0.3" footer="0.3"/>""");
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    private static string CalculateColumns(ReporteExportData data, int columnCount)
    {
        var builder = new StringBuilder("<cols>");
        for (var i = 0; i < columnCount; i++)
        {
            var max = data.Columnas.ElementAtOrDefault(i)?.Length ?? 10;
            foreach (var row in data.Filas)
                max = Math.Max(max, row.ElementAtOrDefault(i)?.Length ?? 0);

            var width = Math.Clamp(max + 3, 10, 38);
            builder.Append(CultureInfo.InvariantCulture, $"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{width}\" customWidth=\"1\"/>");
        }

        builder.Append("</cols>");
        return builder.ToString();
    }

    private static (string Value, int Style) Cell(string? value, int style) => (value ?? string.Empty, style);

    private static void AppendRow(StringBuilder builder, int rowIndex, IReadOnlyList<(string Value, int Style)> cells)
    {
        builder.Append(CultureInfo.InvariantCulture, $"<row r=\"{rowIndex}\">");
        for (var i = 0; i < cells.Count; i++)
        {
            var cellRef = $"{ColumnName(i + 1)}{rowIndex}";
            var value = XmlEscape(cells[i].Value);
            var space = cells[i].Value.Length != cells[i].Value.Trim().Length ? " xml:space=\"preserve\"" : string.Empty;
            builder.Append(CultureInfo.InvariantCulture, $"<c r=\"{cellRef}\" t=\"inlineStr\" s=\"{cells[i].Style}\"><is><t{space}>{value}</t></is></c>");
        }

        builder.Append("</row>");
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }

        return name;
    }

    private static string XmlEscape(string value) => SecurityElementEscape(value);

    private static string SecurityElementEscape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => XmlConvert.IsXmlChar(ch) ? ch : ' '
            });
        }

        return builder.ToString();
    }

    private static string ContentTypes() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>""";

    private static string PackageRelationships() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private static string Workbook() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Reporte" sheetId="1" r:id="rId1"/></sheets></workbook>""";

    private static string WorkbookRelationships() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""";

    private static string Styles() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="5"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="16"/><color rgb="FF0B3D91"/><name val="Calibri"/></font><font><sz val="10"/><color rgb="FF5F6B7A"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF0B3D91"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="2"><border><left/><right/><top/><bottom/><diagonal/></border><border><left style="thin"><color rgb="FFD9DEE8"/></left><right style="thin"><color rgb="FFD9DEE8"/></right><top style="thin"><color rgb="FFD9DEE8"/></top><bottom style="thin"><color rgb="FFD9DEE8"/></bottom><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="5"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="3" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="4" fillId="2" borderId="1" xfId="0" applyFill="1" applyBorder="1"/></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>""";
}
