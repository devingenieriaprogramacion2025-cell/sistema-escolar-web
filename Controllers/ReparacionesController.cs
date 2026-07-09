using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class ReparacionesController : Controller
{
    private readonly ReparacionService _reparacionService;
    private readonly BitacoraService _bitacoraService;
    private readonly ILogger<ReparacionesController> _logger;

    public ReparacionesController(
        ReparacionService reparacionService,
        BitacoraService bitacoraService,
        ILogger<ReparacionesController> logger)
    {
        _reparacionService = reparacionService;
        _bitacoraService = bitacoraService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "salida", string direccion = "desc", int pagina = 1)
    {
        ViewBag.Busqueda = busqueda;
        var reparaciones = await _reparacionService.ListarAsync(busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        reparaciones = ordenar?.ToLowerInvariant() switch
        {
            "equipo" => asc ? reparaciones.OrderBy(r => r.CodigoEquipo).ToList() : reparaciones.OrderByDescending(r => r.CodigoEquipo).ToList(),
            "destino" => asc ? reparaciones.OrderBy(r => r.Destino).ToList() : reparaciones.OrderByDescending(r => r.Destino).ToList(),
            "retorno" => asc ? reparaciones.OrderBy(r => r.FechaRetorno).ToList() : reparaciones.OrderByDescending(r => r.FechaRetorno).ToList(),
            "estado" => asc ? reparaciones.OrderBy(r => r.EstadoReparacion).ToList() : reparaciones.OrderByDescending(r => r.EstadoReparacion).ToList(),
            _ => asc ? reparaciones.OrderBy(r => r.FechaEnvio).ToList() : reparaciones.OrderByDescending(r => r.FechaEnvio).ToList()
        };

        return View(ListadoPaginado.Crear(reparaciones, ordenar, direccion, pagina, busqueda));
    }

    [Authorize]
    public async Task<IActionResult> Crear() => View(await _reparacionService.CrearFormularioAsync());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearReparacionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _reparacionService.CargarCombosAsync(model);
            return View(model);
        }

        try
        {
            await _reparacionService.CrearAsync(model, User.Identity?.Name ?? "Sistema");

            try
            {
                await _bitacoraService.RegistrarAsync(
                    User.Identity?.Name ?? "Sistema",
                    User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "",
                    "Reparaciones",
                    "Registro de reparacion");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "La reparación fue registrada, pero no se pudo escribir su entrada en la bitácora.");
            }

            TempData["Success"] = "Reparacion registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await _reparacionService.CargarCombosAsync(model);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, string? comentario)
    {
        return await CambiarEstadoAsync(id, Estado.EnReparacion, comentario, "Reparacion aprobada y enviada a proceso.");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string? comentario)
    {
        return await CambiarEstadoAsync(id, Estado.Rechazada, comentario, "Reparacion rechazada.");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarRetorno(int id, string? comentario)
    {
        return await CambiarEstadoAsync(id, Estado.Reparada, comentario, "Retorno de equipo registrado correctamente.");
    }

    private async Task<IActionResult> CambiarEstadoAsync(int id, string estado, string? comentario, string mensajeExito)
    {
        try
        {
            await _reparacionService.CambiarEstadoAsync(id, estado, comentario, User.Identity?.Name ?? "Sistema");
            TempData["Success"] = mensajeExito;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = $"No se pudo registrar la accion: {ex.GetBaseException().Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
