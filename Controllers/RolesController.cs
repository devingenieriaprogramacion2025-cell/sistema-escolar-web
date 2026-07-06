using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class RolesController : Controller
{
    private readonly RolService _rolService;

    public RolesController(RolService rolService)
    {
        _rolService = rolService;
    }

    public async Task<IActionResult> Index(string ordenar = "rol", string direccion = "asc", int pagina = 1)
    {
        var roles = await _rolService.ListarConPermisosAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        roles = ordenar?.ToLowerInvariant() switch
        {
            "permisos" => asc ? roles.OrderBy(r => r.Permisos.Count).ToList() : roles.OrderByDescending(r => r.Permisos.Count).ToList(),
            "estado" => asc ? roles.OrderBy(r => r.Activo).ToList() : roles.OrderByDescending(r => r.Activo).ToList(),
            _ => asc ? roles.OrderBy(r => r.NombreRol).ToList() : roles.OrderByDescending(r => r.NombreRol).ToList()
        };

        return View(ListadoPaginado.Crear(roles, ordenar, direccion, pagina));
    }
}
