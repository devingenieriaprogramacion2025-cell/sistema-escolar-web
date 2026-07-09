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

    public async Task<IActionResult> Index(int? mes, int? anio, int? idTipoInsumo, int? idDependencia)
    {
        var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        var rut = User.FindFirst("RutPersonal")?.Value;
        var dashboard = await _dashboardService.ObtenerDashboardAsync(rol, rut, mes, anio, idTipoInsumo, idDependencia);
        ViewBag.Rol = rol;
        ViewBag.Nombre = User.Identity?.Name ?? "Usuario";
        return View(dashboard);
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
