using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class AsignacionesController : Controller
{
    private readonly AsignacionService _asignacionService;

    public AsignacionesController(AsignacionService asignacionService)
    {
        _asignacionService = asignacionService;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "fecha", string direccion = "desc", int pagina = 1)
    {
        ViewBag.Busqueda = busqueda;
        var asignaciones = await _asignacionService.ListarAsync(User.GetRol(), User.GetRutPersonal(), busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        asignaciones = ordenar?.ToLowerInvariant() switch
        {
            "equipo" => asc ? asignaciones.OrderBy(a => a.CodigoEquipo).ToList() : asignaciones.OrderByDescending(a => a.CodigoEquipo).ToList(),
            "marca" => asc ? asignaciones.OrderBy(a => a.MarcaEquipo).ToList() : asignaciones.OrderByDescending(a => a.MarcaEquipo).ToList(),
            "modelo" => asc ? asignaciones.OrderBy(a => a.ModeloEquipo).ToList() : asignaciones.OrderByDescending(a => a.ModeloEquipo).ToList(),
            "destinatario" => asc ? asignaciones.OrderBy(a => a.AsignadoA).ToList() : asignaciones.OrderByDescending(a => a.AsignadoA).ToList(),
            "tipo-destinatario" => asc ? asignaciones.OrderBy(a => a.TipoDestinatario).ToList() : asignaciones.OrderByDescending(a => a.TipoDestinatario).ToList(),
            "estado" => asc ? asignaciones.OrderBy(a => a.EstadoAsignacion).ToList() : asignaciones.OrderByDescending(a => a.EstadoAsignacion).ToList(),
            _ => asc ? asignaciones.OrderBy(a => a.FechaAsignacion).ToList() : asignaciones.OrderByDescending(a => a.FechaAsignacion).ToList()
        };

        return View(ListadoPaginado.Crear(asignaciones, ordenar, direccion, pagina, busqueda));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var asignacion = await _asignacionService.ObtenerDetalleAsync(id, User.GetRol(), User.GetRutPersonal());
        if (asignacion == null) return NotFound();
        return View(asignacion);
    }

    [Authorize]
    public async Task<IActionResult> Crear()
    {
        var model = await _asignacionService.CrearFormularioAsync();
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearAsignacionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _asignacionService.CargarCombosAsync(model);
            return View(model);
        }

        try
        {
            await _asignacionService.CrearAsync(model);
            TempData["Success"] = "Asignación registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await _asignacionService.CargarCombosAsync(model);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarDevolucion(int id, string? comentario)
    {
        try
        {
            await _asignacionService.RegistrarDevolucionAsync(id, comentario, User.Identity?.Name ?? "Sistema");
            TempData["Success"] = "Devolución registrada correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = $"No se pudo registrar la devolucion: {ex.GetBaseException().Message}";
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }
}
