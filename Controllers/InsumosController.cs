using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class InsumosController : Controller
{
    private readonly InsumoService _insumoService;

    public InsumosController(InsumoService insumoService)
    {
        _insumoService = insumoService;
    }

    public async Task<IActionResult> Index(string ordenar = "nombre", string direccion = "asc", int pagina = 1)
    {
        var tiposInsumo = await _insumoService.ObtenerMapaTiposAsync();
        var insumos = await _insumoService.ListarAsync();
        var ascendente = !string.Equals(direccion, "desc", StringComparison.OrdinalIgnoreCase);

        insumos = ordenar?.ToLowerInvariant() switch
        {
            "tipo" => ascendente
                ? insumos.OrderBy(i => tiposInsumo.GetValueOrDefault(i.IdTipoInsumo, string.Empty)).ThenBy(i => i.NombreInsumo).ToList()
                : insumos.OrderByDescending(i => tiposInsumo.GetValueOrDefault(i.IdTipoInsumo, string.Empty)).ThenBy(i => i.NombreInsumo).ToList(),
            "stock" => ascendente
                ? insumos.OrderBy(i => i.StockActual).ThenBy(i => i.NombreInsumo).ToList()
                : insumos.OrderByDescending(i => i.StockActual).ThenBy(i => i.NombreInsumo).ToList(),
            "unidad" => ascendente
                ? insumos.OrderBy(i => i.UnidadMedida).ThenBy(i => i.NombreInsumo).ToList()
                : insumos.OrderByDescending(i => i.UnidadMedida).ThenBy(i => i.NombreInsumo).ToList(),
            "toxicidad" => ascendente
                ? insumos.OrderBy(i => i.Toxicidad).ThenBy(i => i.NombreInsumo).ToList()
                : insumos.OrderByDescending(i => i.Toxicidad).ThenBy(i => i.NombreInsumo).ToList(),
            "estado" => ascendente
                ? insumos.OrderBy(i => i.Estado).ThenBy(i => i.NombreInsumo).ToList()
                : insumos.OrderByDescending(i => i.Estado).ThenBy(i => i.NombreInsumo).ToList(),
            _ => ascendente
                ? insumos.OrderBy(i => i.NombreInsumo).ToList()
                : insumos.OrderByDescending(i => i.NombreInsumo).ToList()
        };

        ViewBag.TiposInsumo = tiposInsumo;
        ViewBag.Ordenar = ordenar;
        ViewBag.Direccion = ascendente ? "asc" : "desc";
        return View(ListadoPaginado.Crear(insumos, ordenar, direccion, pagina));
    }

    [Authorize]
    public async Task<IActionResult> Crear()
    {
        await CargarTiposAsync();
        return View(new Insumo { Estado = true, UnidadMedida = "Unidad", Toxicidad = "No toxico" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Insumo insumo)
    {
        await ValidarInsumoAsync(insumo);
        if (!ModelState.IsValid)
        {
            await CargarTiposAsync();
            return View(insumo);
        }

        await _insumoService.CrearAsync(new Insumo
        {
            IdTipoInsumo = insumo.IdTipoInsumo,
            NombreInsumo = insumo.NombreInsumo.Trim(),
            DescripcionInsumo = string.IsNullOrWhiteSpace(insumo.DescripcionInsumo) ? null : insumo.DescripcionInsumo.Trim(),
            UnidadMedida = insumo.UnidadMedida.Trim(),
            StockActual = insumo.StockActual,
            StockMinimo = insumo.StockMinimo,
            Estado = true,
            Toxicidad = insumo.Toxicidad!.Trim()
        });

        TempData["Success"] = "Insumo registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Editar(int id)
    {
        var insumo = await _insumoService.ObtenerAsync(id);
        if (insumo == null)
            return NotFound();

        await CargarTiposAsync();
        return View(insumo);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Insumo insumo)
    {
        await ValidarInsumoAsync(insumo, insumo.IdInsumo);
        if (!ModelState.IsValid)
        {
            await CargarTiposAsync();
            return View(insumo);
        }

        await _insumoService.ActualizarAsync(insumo);
        TempData["Success"] = "Insumo actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var insumo = await _insumoService.ObtenerAsync(id);
        if (insumo == null)
            return NotFound();

        ViewBag.TiposInsumo = await _insumoService.ObtenerMapaTiposAsync();
        return View(insumo);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await _insumoService.EliminarAsync(id);
        TempData[resultado.Success ? "Success" : "Warning"] = resultado.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarTiposAsync()
    {
        ViewBag.TiposInsumo = await _insumoService.ObtenerTiposAsync();
    }

    private async Task ValidarInsumoAsync(Insumo insumo, int? excluirId = null)
    {
        if (!InputValidationHelper.IsSafeText(insumo.NombreInsumo, 120, required: true))
            ModelState.AddModelError(nameof(insumo.NombreInsumo), "Debe ingresar un nombre de insumo valido.");

        if (!InputValidationHelper.IsSafeText(insumo.DescripcionInsumo, 250, required: false))
            ModelState.AddModelError(nameof(insumo.DescripcionInsumo), "La descripcion contiene caracteres no permitidos o supera el largo permitido.");

        if (!InputValidationHelper.IsSafeText(insumo.UnidadMedida, 40, required: true))
            ModelState.AddModelError(nameof(insumo.UnidadMedida), "Debe ingresar una unidad de medida valida.");

        if (!InputValidationHelper.IsSafeText(insumo.Toxicidad, 40, required: true))
            ModelState.AddModelError(nameof(insumo.Toxicidad), "Debe ingresar una toxicidad valida.");

        if (!await _insumoService.EsTipoPermitidoAsync(insumo.IdTipoInsumo))
            ModelState.AddModelError(nameof(insumo.IdTipoInsumo), "Seleccione Material de aseo o Libreria como tipo de insumo.");

        if (insumo.StockActual < 0 || insumo.StockMinimo < 0 || insumo.StockActual > 100000 || insumo.StockMinimo > 100000)
            ModelState.AddModelError(nameof(insumo.StockActual), "El stock debe estar entre 0 y 100000.");

        if (InputValidationHelper.IsSafeText(insumo.NombreInsumo, 120, required: true) &&
            await _insumoService.ExisteDuplicadoAsync(insumo.IdTipoInsumo, insumo.NombreInsumo, excluirId))
            ModelState.AddModelError(nameof(insumo.NombreInsumo), "Ya existe un insumo con el mismo nombre y tipo.");
    }
}
