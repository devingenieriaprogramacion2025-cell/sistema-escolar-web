using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class ImpresionesController : Controller
{
    private readonly ImpresionService _impresionService;
    private readonly BitacoraService _bitacoraService;

    public ImpresionesController(ImpresionService impresionService, BitacoraService bitacoraService)
    {
        _impresionService = impresionService;
        _bitacoraService = bitacoraService;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "solicitud", string direccion = "desc", int pagina = 1)
    {
        ViewBag.Busqueda = busqueda;
        var impresiones = await _impresionService.ListarAsync(User.GetRol(), User.GetRutPersonal(), busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        impresiones = ordenar?.ToLowerInvariant() switch
        {
            "solicitante" => asc ? impresiones.OrderBy(i => i.NombrePersonal).ToList() : impresiones.OrderByDescending(i => i.NombrePersonal).ToList(),
            "archivo" => asc ? impresiones.OrderBy(i => i.Archivo).ToList() : impresiones.OrderByDescending(i => i.Archivo).ToList(),
            "paginas" => asc ? impresiones.OrderBy(i => i.CantidadPaginas).ToList() : impresiones.OrderByDescending(i => i.CantidadPaginas).ToList(),
            "copias" => asc ? impresiones.OrderBy(i => i.CantidadCopias).ToList() : impresiones.OrderByDescending(i => i.CantidadCopias).ToList(),
            "total" => asc ? impresiones.OrderBy(i => i.TotalImpresiones).ToList() : impresiones.OrderByDescending(i => i.TotalImpresiones).ToList(),
            "opciones" => asc ? impresiones.OrderBy(i => i.Color).ThenBy(i => i.DobleCara).ToList() : impresiones.OrderByDescending(i => i.Color).ThenByDescending(i => i.DobleCara).ToList(),
            "entrega" => asc ? impresiones.OrderBy(i => i.FechaEntrega).ToList() : impresiones.OrderByDescending(i => i.FechaEntrega).ToList(),
            "estado" => asc ? impresiones.OrderBy(i => i.Estado).ToList() : impresiones.OrderByDescending(i => i.Estado).ToList(),
            _ => asc ? impresiones.OrderBy(i => i.FechaSolicitud).ToList() : impresiones.OrderByDescending(i => i.FechaSolicitud).ToList()
        };

        return View(ListadoPaginado.Crear(impresiones, ordenar, direccion, pagina, busqueda));
    }

    public async Task<IActionResult> Archivo(int id)
    {
        try
        {
            var archivo = await _impresionService.ObtenerArchivoAsync(id, User.GetRol(), User.GetRutPersonal());
            if (archivo == null)
                return NotFound();

            return PhysicalFile(archivo.RutaFisica, archivo.ContentType, archivo.NombreDescarga);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    public async Task<IActionResult> Crear()
    {
        return View(await _impresionService.CrearFormularioAsync(User.GetRol(), User.GetRutPersonal()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearImpresionViewModel model)
    {
        if (User.IsInRole("Profesor"))
            model.RutPersonal = User.GetRutPersonal() ?? model.RutPersonal;

        if (!ModelState.IsValid)
        {
            await _impresionService.CargarCombosAsync(model, User.GetRol(), User.GetRutPersonal());
            return View(model);
        }

        try
        {
            await _impresionService.CrearAsync(model);
            await _bitacoraService.RegistrarAsync(User.Identity?.Name ?? "Sistema", User.GetRol() ?? "", "Impresiones", "Solicitud de impresion creada");
            TempData["Success"] = "Solicitud de impresion registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await _impresionService.CargarCombosAsync(model, User.GetRol(), User.GetRutPersonal());
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnProceso(int id)
    {
        await _impresionService.CambiarEstadoAsync(id, Estado.EnProceso);
        TempData["Success"] = "Solicitud aceptada y marcada en proceso.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Entregar(int id)
    {
        await _impresionService.CambiarEstadoAsync(id, Estado.Entregada);
        TempData["Success"] = "Solicitud entregada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id)
    {
        await _impresionService.CambiarEstadoAsync(id, Estado.Rechazada);
        TempData["Success"] = "Solicitud rechazada.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        TempData["Error"] = "No se permite eliminar solicitudes de impresion. El historial institucional debe conservarse.";
        return RedirectToAction(nameof(Index));
    }
}
