using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Reports;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly ReporteService _reporteService;
    private readonly PdfGenerator _pdfGenerator;
    private readonly ExcelGenerator _excelGenerator;

    public ReportesController(ReporteService reporteService, PdfGenerator pdfGenerator, ExcelGenerator excelGenerator)
    {
        _reporteService = reporteService;
        _pdfGenerator = pdfGenerator;
        _excelGenerator = excelGenerator;
    }

    public IActionResult Index()
        => View(_reporteService.ObtenerMenuReportes());

    public async Task<IActionResult> Ejecutivo()
        => View(await _reporteService.ObtenerEjecutivoAsync());

    public async Task<IActionResult> InventarioTecnologico(ReporteInventarioTecnologicoFiltro filtro)
        => View(await _reporteService.ObtenerInventarioTecnologicoAsync(filtro));

    public async Task<IActionResult> MovimientosInsumos(ReporteMovimientosInsumosFiltro filtro)
        => View(await _reporteService.ObtenerMovimientosInsumosAsync(filtro));

    public async Task<IActionResult> Asignaciones(ReporteAsignacionesFiltro filtro)
        => View(await _reporteService.ObtenerAsignacionesAsync(filtro));

    public async Task<IActionResult> Reparaciones(ReporteReparacionesFiltro filtro)
        => View(await _reporteService.ObtenerReparacionesAsync(filtro));

    public async Task<IActionResult> Bajas(ReporteBajasFiltro filtro)
        => View(await _reporteService.ObtenerBajasAsync(filtro));

    public async Task<IActionResult> Impresiones(ReporteImpresionesFiltro filtro)
        => View(await _reporteService.ObtenerImpresionesAsync(filtro));

    public async Task<IActionResult> Personal(ReportePersonalFiltro filtro)
        => View(await _reporteService.ObtenerPersonalAsync(filtro));

    public async Task<IActionResult> ExportarPdf(string reporte)
    {
        var data = await _reporteService.ObtenerExportacionAsync(reporte, QueryParameters());
        if (data == null)
            return NotFound();

        var bytes = _pdfGenerator.Generate(data);
        return File(bytes, "application/pdf", $"{NombreArchivo(data.Titulo)}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    public async Task<IActionResult> ExportarExcel(string reporte)
    {
        var data = await _reporteService.ObtenerExportacionAsync(reporte, QueryParameters());
        if (data == null)
            return NotFound();

        var bytes = _excelGenerator.Generate(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{NombreArchivo(data.Titulo)}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    private IReadOnlyDictionary<string, string?> QueryParameters()
        => Request.Query.ToDictionary(item => item.Key, item => (string?)item.Value.ToString(), StringComparer.OrdinalIgnoreCase);

    private static string NombreArchivo(string value)
    {
        var chars = value
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();

        return string.Join('_', new string(chars).Split('_', StringSplitOptions.RemoveEmptyEntries));
    }
}
