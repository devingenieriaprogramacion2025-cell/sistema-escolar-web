using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class SalidasInsumoController : Controller
{
    private readonly MovimientoInsumoService _service;

    public SalidasInsumoController(MovimientoInsumoService service) => _service = service;

    public async Task<IActionResult> Index(string ordenar = "fecha", string direccion = "desc", int pagina = 1)
    {
        var salidas = await _service.ListarSalidasAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        salidas = ordenar?.ToLowerInvariant() switch
        {
            "insumo" => asc ? salidas.OrderBy(s => s.Insumo).ToList() : salidas.OrderByDescending(s => s.Insumo).ToList(),
            "dependencia" => asc ? salidas.OrderBy(s => s.Dependencia).ToList() : salidas.OrderByDescending(s => s.Dependencia).ToList(),
            "responsable" => asc ? salidas.OrderBy(s => s.Responsable).ToList() : salidas.OrderByDescending(s => s.Responsable).ToList(),
            "cantidad" => asc ? salidas.OrderBy(s => s.Cantidad).ToList() : salidas.OrderByDescending(s => s.Cantidad).ToList(),
            _ => asc ? salidas.OrderBy(s => s.FechaSalida).ToList() : salidas.OrderByDescending(s => s.FechaSalida).ToList()
        };

        return View(ListadoPaginado.Crear(salidas, ordenar, direccion, pagina));
    }

    [Authorize]
    public async Task<IActionResult> Crear(int? idInsumo = null)
    {
        var model = await _service.CrearFormularioSalidaAsync();
        model.IdInsumo = idInsumo ?? model.IdInsumo;
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearSalidaInsumoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _service.CargarCombosAsync(model);
            return View(model);
        }

        try
        {
            await _service.RegistrarSalidaAsync(model);
            TempData["Success"] = "Salida registrada correctamente. El stock del insumo fue actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await _service.CargarCombosAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var model = (await _service.ListarSalidasAsync()).FirstOrDefault(s => s.IdSalidaInsumo == id);
        if (model == null)
            return NotFound();

        return View(model);
    }
}
