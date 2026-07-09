using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class BajasController : Controller
{
    private readonly BajaService _bajaService;
    private readonly BitacoraService _bitacoraService;
    private readonly ILogger<BajasController> _logger;

    public BajasController(
        BajaService bajaService,
        BitacoraService bitacoraService,
        ILogger<BajasController> logger)
    {
        _bajaService = bajaService;
        _bitacoraService = bitacoraService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "fecha", string direccion = "desc", int pagina = 1)
    {
        ViewBag.Busqueda = busqueda;
        var bajas = await _bajaService.ListarAsync(busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        bajas = ordenar?.ToLowerInvariant() switch
        {
            "equipo" => asc ? bajas.OrderBy(b => b.CodigoEquipo).ToList() : bajas.OrderByDescending(b => b.CodigoEquipo).ToList(),
            "detalle" => asc ? bajas.OrderBy(b => b.Detalle).ToList() : bajas.OrderByDescending(b => b.Detalle).ToList(),
            "registra" => asc ? bajas.OrderBy(b => b.UsuarioRegistraBaja).ToList() : bajas.OrderByDescending(b => b.UsuarioRegistraBaja).ToList(),
            "autoriza" => asc ? bajas.OrderBy(b => b.UsuarioAutorizaBaja).ToList() : bajas.OrderByDescending(b => b.UsuarioAutorizaBaja).ToList(),
            "estado" => asc ? bajas.OrderBy(b => b.Estado).ToList() : bajas.OrderByDescending(b => b.Estado).ToList(),
            _ => asc ? bajas.OrderBy(b => b.FechaBaja).ToList() : bajas.OrderByDescending(b => b.FechaBaja).ToList()
        };

        return View(ListadoPaginado.Crear(bajas, ordenar, direccion, pagina, busqueda));
    }

    [Authorize]
    public async Task<IActionResult> Crear(int? idTecnologia)
    {
        return View(await _bajaService.CrearFormularioAsync(idTecnologia));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearBajaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _bajaService.CargarCombosAsync(model);
            return View(model);
        }

        try
        {
            await _bajaService.CrearAsync(model, User.Identity?.Name ?? "Sistema");
            TempData["Success"] = "Solicitud de baja registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await _bajaService.CargarCombosAsync(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar la baja del equipo {IdTecnologia}.", model.IdTecnologia);
            var detalleError = ex.GetBaseException().Message;
            ModelState.AddModelError(string.Empty, $"No se pudo guardar la baja: {detalleError}");
            await _bajaService.CargarCombosAsync(model);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, string? comentario)
    {
        return await CambiarEstadoAsync(id, Estado.Aprobada, comentario, "Baja aprobada. El equipo quedo inactivo.", $"Baja #{id} aprobada");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string? comentario)
    {
        return await CambiarEstadoAsync(id, Estado.Rechazada, comentario, "Baja rechazada.", $"Baja #{id} rechazada");
    }

    private async Task<IActionResult> CambiarEstadoAsync(int id, string estado, string? comentario, string mensajeExito, string accionBitacora)
    {
        try
        {
            await _bajaService.CambiarEstadoAsync(id, estado, comentario, User.Identity?.Name ?? "Sistema");
            TempData["Success"] = mensajeExito;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = $"No se pudo registrar la accion: {ex.GetBaseException().Message}";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _bitacoraService.RegistrarAsync(User.Identity?.Name ?? "Sistema", User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "", "Bajas", accionBitacora);
        }
        catch { }

        return RedirectToAction(nameof(Index));
    }
}
