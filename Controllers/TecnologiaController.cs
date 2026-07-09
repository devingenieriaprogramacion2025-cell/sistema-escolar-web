using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class TecnologiaController : Controller
{
    private readonly TecnologiaService _tecnologiaService;

    public TecnologiaController(TecnologiaService tecnologiaService)
    {
        _tecnologiaService = tecnologiaService;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "codigo", string direccion = "asc", int pagina = 1, int? editarId = null)
    {
        const int registrosPorPagina = 15;
        var equipos = await _tecnologiaService.ListarAsync(busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        equipos = ordenar?.ToLowerInvariant() switch
        {
            "marca" => asc ? equipos.OrderBy(e => e.Marca).ToList() : equipos.OrderByDescending(e => e.Marca).ToList(),
            "modelo" => asc ? equipos.OrderBy(e => e.Modelo).ToList() : equipos.OrderByDescending(e => e.Modelo).ToList(),
            "tipo" => asc ? equipos.OrderBy(e => e.TipoTecnologia).ToList() : equipos.OrderByDescending(e => e.TipoTecnologia).ToList(),
            "descripcion" => asc ? equipos.OrderBy(e => e.Descripcion).ToList() : equipos.OrderByDescending(e => e.Descripcion).ToList(),
            "estado" => asc ? equipos.OrderBy(e => e.EstadoOperativo).ToList() : equipos.OrderByDescending(e => e.EstadoOperativo).ToList(),
            "ultimo-movimiento" => asc ? equipos.OrderBy(e => e.UltimaFechaMovimiento).ToList() : equipos.OrderByDescending(e => e.UltimaFechaMovimiento).ToList(),
            "comentario" => asc ? equipos.OrderBy(e => e.UltimoComentario).ToList() : equipos.OrderByDescending(e => e.UltimoComentario).ToList(),
            _ => asc ? equipos.OrderBy(e => e.CodigoInventario).ToList() : equipos.OrderByDescending(e => e.CodigoInventario).ToList()
        };
        var totalRegistros = equipos.Count;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        ViewBag.Busqueda = busqueda;
        ViewData["EditandoTecnologia"] = editarId.HasValue;

        return View(new GestionTecnologiaViewModel
        {
            Formulario = editarId.HasValue
                ? await _tecnologiaService.ObtenerFormularioTecnologiaAsync(editarId.Value) ?? new TecnologiaFormViewModel { Estado = true }
                : new TecnologiaFormViewModel { Estado = true },
            EntradasTecnologia = await _tecnologiaService.ObtenerEntradasSelectAsync(),
            Equipos = equipos.Skip((pagina - 1) * registrosPorPagina).Take(registrosPorPagina),
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalRegistros = totalRegistros,
            RegistrosPorPagina = registrosPorPagina,
            Ordenar = ordenar,
            Direccion = asc ? "asc" : "desc",
            Busqueda = busqueda
        });
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var equipo = await _tecnologiaService.ObtenerDetalleAsync(id);
        if (equipo == null) return NotFound();
        return View(equipo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(TecnologiaFormViewModel model)
    {
        if (await _tecnologiaService.ExisteCodigoAsync(model.SkuCodigoInventario))
            ModelState.AddModelError(nameof(model.SkuCodigoInventario), "Ya existe un equipo con el codigo ingresado.");

        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        await _tecnologiaService.CrearTecnologiaAsync(model);
        TempData["Success"] = "Equipo tecnologico registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Editar(int id)
    {
        return RedirectToAction(nameof(Index), new { editarId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(TecnologiaFormViewModel model)
    {
        if (!model.IdTecnologia.HasValue) return BadRequest();

        if (await _tecnologiaService.ExisteCodigoAsync(model.SkuCodigoInventario, model.IdTecnologia))
            ModelState.AddModelError(nameof(model.SkuCodigoInventario), "Ya existe otro equipo con el codigo ingresado.");

        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index), new { editarId = model.IdTecnologia });

        try
        {
            await _tecnologiaService.ActualizarTecnologiaAsync(model);
            TempData["Success"] = "Equipo tecnologico actualizado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
