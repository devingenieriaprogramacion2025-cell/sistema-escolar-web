using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class EntradasInsumoController : Controller
{
    private readonly MovimientoInsumoService _service;

    public EntradasInsumoController(MovimientoInsumoService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string ordenar = "fecha", string direccion = "desc", int pagina = 1)
    {
        var entradas = await _service.ListarEntradasAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        entradas = ordenar?.ToLowerInvariant() switch
        {
            "insumo" => asc ? entradas.OrderBy(e => e.Insumo).ToList() : entradas.OrderByDescending(e => e.Insumo).ToList(),
            "proveedor" => asc ? entradas.OrderBy(e => e.Proveedor).ToList() : entradas.OrderByDescending(e => e.Proveedor).ToList(),
            "factura" => asc ? entradas.OrderBy(e => e.NumeroFactura).ToList() : entradas.OrderByDescending(e => e.NumeroFactura).ToList(),
            "cantidad" => asc ? entradas.OrderBy(e => e.Cantidad).ToList() : entradas.OrderByDescending(e => e.Cantidad).ToList(),
            _ => asc ? entradas.OrderBy(e => e.FechaEntrega).ToList() : entradas.OrderByDescending(e => e.FechaEntrega).ToList()
        };

        return View(ListadoPaginado.Crear(entradas, ordenar, direccion, pagina));
    }

    [Authorize]
    public async Task<IActionResult> Crear(int? idInsumo = null)
    {
        var model = await _service.CrearFormularioEntradaAsync();
        model.IdInsumo = idInsumo ?? model.IdInsumo;
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearEntradaInsumoViewModel model)
    {
        if (!InputValidationHelper.IsSafeText(model.NumeroFactura, 80, required: true))
            ModelState.AddModelError(nameof(model.NumeroFactura), "Ingrese un numero de factura valido.");

        if (!model.IdProveedor.HasValue)
            ModelState.AddModelError(nameof(model.IdProveedor), "Seleccione un proveedor valido.");

        if (!ModelState.IsValid)
        {
            await _service.CargarCombosAsync(model);
            return View(model);
        }

        try
        {
            await _service.RegistrarEntradaAsync(model);
            TempData["Success"] = "Entrada registrada correctamente. El stock del insumo fue actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await _service.CargarCombosAsync(model);
            return View(model);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo guardar la entrada. Verifique el insumo, proveedor y cantidad.");
            await _service.CargarCombosAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var model = (await _service.ListarEntradasAsync()).FirstOrDefault(e => e.IdEntradaInsumo == id);
        if (model == null)
            return NotFound();

        return View(model);
    }
}
