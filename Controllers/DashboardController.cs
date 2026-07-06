using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        var rut = User.FindFirst("RutPersonal")?.Value;
        var resumen = await _dashboardService.ObtenerResumenAsync(rol, rut);
        ViewBag.Rol = rol;
        ViewBag.Nombre = User.Identity?.Name ?? "Usuario";
        return View(resumen);
    }

    [HttpGet]
    public async Task<IActionResult> ImpresionesPorEstado()
    {
        var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        var rut = User.FindFirst("RutPersonal")?.Value;
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        return Json(await _dashboardService.ObtenerImpresionesPorEstadoAsync(rol, rut));
    }
}
