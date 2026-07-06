namespace SistemaEscolarWeb.Reports;

public sealed class ReporteExportData
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public List<ReporteExportItem> Filtros { get; set; } = [];
    public List<ReporteExportItem> Resumen { get; set; } = [];
    public List<string> Columnas { get; set; } = [];
    public List<List<string>> Filas { get; set; } = [];
}

public sealed record ReporteExportItem(string Nombre, string Valor);
