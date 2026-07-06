using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class ProveedoresController : Controller
{
    private readonly ProveedorService _proveedorService;

    public ProveedoresController(ProveedorService proveedorService)
    {
        _proveedorService = proveedorService;
    }

    public async Task<IActionResult> Index(string ordenar = "nombre", string direccion = "asc", int pagina = 1)
    {
        var proveedores = await _proveedorService.ListarAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        proveedores = ordenar?.ToLowerInvariant() switch
        {
            "rut" => asc ? proveedores.OrderBy(p => p.RutProveedor).ToList() : proveedores.OrderByDescending(p => p.RutProveedor).ToList(),
            "correo" => asc ? proveedores.OrderBy(p => p.Correo).ToList() : proveedores.OrderByDescending(p => p.Correo).ToList(),
            "telefono" => asc ? proveedores.OrderBy(p => p.Telefono).ToList() : proveedores.OrderByDescending(p => p.Telefono).ToList(),
            _ => asc ? proveedores.OrderBy(p => p.NombreProveedor).ToList() : proveedores.OrderByDescending(p => p.NombreProveedor).ToList()
        };

        return View(ListadoPaginado.Crear(proveedores, ordenar, direccion, pagina));
    }

    public IActionResult Crear()
    {
        return View(new CrearProveedorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearProveedorViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.RutProveedor) && await _proveedorService.ExisteRutAsync(model.RutProveedor))
            ModelState.AddModelError(nameof(model.RutProveedor), "Ya existe un proveedor con el RUT ingresado.");

        if (!ModelState.IsValid)
            return View(model);

        await _proveedorService.CrearAsync(model);
        TempData["Success"] = "Proveedor registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var proveedor = await _proveedorService.ObtenerAsync(id);
        if (proveedor == null)
            return NotFound();

        return View(ProveedorService.MapearFormulario(proveedor));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(CrearProveedorViewModel model)
    {
        if (!model.IdProveedor.HasValue)
            return BadRequest();

        if (!string.IsNullOrWhiteSpace(model.RutProveedor) && await _proveedorService.ExisteRutAsync(model.RutProveedor, model.IdProveedor))
            ModelState.AddModelError(nameof(model.RutProveedor), "Ya existe otro proveedor con el RUT ingresado.");

        if (!ModelState.IsValid)
            return View(model);

        await _proveedorService.ActualizarAsync(model);
        TempData["Success"] = "Proveedor actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var proveedor = await _proveedorService.ObtenerAsync(id);
        if (proveedor == null)
            return NotFound();

        return View(proveedor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearRapido(CrearProveedorViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.RutProveedor) && await _proveedorService.ExisteRutAsync(model.RutProveedor))
            ModelState.AddModelError(nameof(model.RutProveedor), "Ya existe un proveedor con el RUT ingresado.");

        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            return Json(new
            {
                success = false,
                mensaje = errores.Count == 0 ? "Revise los datos ingresados." : string.Join(" ", errores)
            });
        }

        var proveedor = await _proveedorService.CrearAsync(model);
        return Json(new
        {
            success = true,
            idProveedor = proveedor.IdProveedor,
            nombreProveedor = proveedor.NombreProveedor,
            mensaje = "Proveedor registrado correctamente."
        });
    }
}
