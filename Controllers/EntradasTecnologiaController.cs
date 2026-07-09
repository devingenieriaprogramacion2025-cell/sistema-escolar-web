using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class EntradasTecnologiaController : Controller
{
    private readonly TecnologiaService _tecnologiaService;

    public EntradasTecnologiaController(TecnologiaService tecnologiaService)
    {
        _tecnologiaService = tecnologiaService;
    }

    public async Task<IActionResult> Index(string ordenar = "fecha", string direccion = "desc", int pagina = 1, int? editarId = null)
    {
        ViewData["EditandoEntradaTecnologia"] = editarId.HasValue;
        var entradas = await _tecnologiaService.ListarEntradasTecnologiaAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        entradas = ordenar?.ToLowerInvariant() switch
        {
            "proveedor" => asc ? entradas.OrderBy(e => e.Proveedor).ToList() : entradas.OrderByDescending(e => e.Proveedor).ToList(),
            "marca" => asc ? entradas.OrderBy(e => e.Marca).ToList() : entradas.OrderByDescending(e => e.Marca).ToList(),
            "modelo" => asc ? entradas.OrderBy(e => e.Modelo).ToList() : entradas.OrderByDescending(e => e.Modelo).ToList(),
            "tipo" => asc ? entradas.OrderBy(e => e.TipoTecnologia).ToList() : entradas.OrderByDescending(e => e.TipoTecnologia).ToList(),
            "sku" => asc ? entradas.OrderBy(e => e.SkuGenerados).ToList() : entradas.OrderByDescending(e => e.SkuGenerados).ToList(),
            "cantidad" => asc ? entradas.OrderBy(e => e.Cantidad).ToList() : entradas.OrderByDescending(e => e.Cantidad).ToList(),
            "factura" => asc ? entradas.OrderBy(e => e.NumeroFactura).ToList() : entradas.OrderByDescending(e => e.NumeroFactura).ToList(),
            _ => asc ? entradas.OrderBy(e => e.FechaEntrada).ToList() : entradas.OrderByDescending(e => e.FechaEntrada).ToList()
        };
        var listado = ListadoPaginado.Crear(entradas, ordenar, direccion, pagina);

        var formulario = editarId.HasValue
            ? await _tecnologiaService.ObtenerFormularioEntradaTecnologiaAsync(editarId.Value) ?? new EntradaTecnologiaFormViewModel()
            : new EntradaTecnologiaFormViewModel();
        formulario.Proveedores = await _tecnologiaService.ObtenerProveedoresSelectAsync(formulario.IdProveedor);

        return View(new GestionEntradasTecnologiaViewModel
        {
            Formulario = formulario,
            Entradas = listado.Items,
            PaginaActual = listado.PaginaActual,
            TotalPaginas = listado.TotalPaginas,
            TotalRegistros = listado.TotalRegistros,
            RegistrosPorPagina = listado.RegistrosPorPagina,
            Ordenar = listado.Ordenar,
            Direccion = listado.Direccion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([Bind(Prefix = "Formulario")] EntradaTecnologiaFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["EditandoEntradaTecnologia"] = false;
            return View(nameof(Index), await CrearGestionViewModelAsync(model));
        }

        await _tecnologiaService.CrearEntradaTecnologiaAsync(model);
        TempData["Success"] = "Entrada tecnologica registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Editar(int id)
    {
        return RedirectToAction(nameof(Index), new { editarId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar([Bind(Prefix = "Formulario")] EntradaTecnologiaFormViewModel model)
    {
        if (!model.IdEntradaTecnologia.HasValue) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["EditandoEntradaTecnologia"] = true;
            return View(nameof(Index), await CrearGestionViewModelAsync(model));
        }

        try
        {
            await _tecnologiaService.ActualizarEntradaTecnologiaAsync(model);
            TempData["Success"] = "Entrada tecnologica actualizada correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<GestionEntradasTecnologiaViewModel> CrearGestionViewModelAsync(EntradaTecnologiaFormViewModel formulario)
    {
        formulario.Proveedores = await _tecnologiaService.ObtenerProveedoresSelectAsync(formulario.IdProveedor);
        var entradas = await _tecnologiaService.ListarEntradasTecnologiaAsync();
        var listado = ListadoPaginado.Crear(entradas, "fecha", "desc", 1);

        return new GestionEntradasTecnologiaViewModel
        {
            Formulario = formulario,
            Entradas = listado.Items,
            PaginaActual = listado.PaginaActual,
            TotalPaginas = listado.TotalPaginas,
            TotalRegistros = listado.TotalRegistros,
            RegistrosPorPagina = listado.RegistrosPorPagina,
            Ordenar = listado.Ordenar,
            Direccion = listado.Direccion
        };
    }
}
