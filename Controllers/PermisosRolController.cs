using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class PermisosRolController : Controller
{
    private readonly PermisoRolService _service;
    private readonly RolService _rolService;

    public PermisosRolController(PermisoRolService service, RolService rolService)
    {
        _service = service;
        _rolService = rolService;
    }

    public async Task<IActionResult> Index(string ordenar = "rol", string direccion = "asc", int pagina = 1)
    {
        var roles = await _service.ListarRolesAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        roles = asc
            ? roles.OrderBy(r => r.NombreRol).ToList()
            : roles.OrderByDescending(r => r.NombreRol).ToList();
        var listado = ListadoPaginado.Crear(roles, ordenar, direccion, pagina);

        return View(new PermisosRolIndexViewModel
        {
            Roles = listado.Items,
            PaginaActual = listado.PaginaActual,
            TotalPaginas = listado.TotalPaginas,
            TotalRegistros = listado.TotalRegistros,
            RegistrosPorPagina = listado.RegistrosPorPagina,
            Ordenar = listado.Ordenar,
            Direccion = listado.Direccion
        });
    }

    public IActionResult Crear()
    {
        return View(new RolFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RolFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _rolService.CrearAsync(model);
            TempData["Success"] = "Rol creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Editar(int id)
    {
        var model = await _rolService.ObtenerFormularioAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(RolFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _rolService.ActualizarAsync(model);
            TempData["Success"] = "Rol actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Eliminar(int id)
    {
        var model = await _rolService.ObtenerFormularioAsync(id);
        if (model == null)
            return NotFound();

        return View(new EliminarRolViewModel
        {
            IdRol = model.IdRol,
            NombreRol = model.NombreRol
        });
    }

    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar(int idRol)
    {
        try
        {
            await _rolService.EliminarAsync(idRol);
            TempData["Success"] = "Rol eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Administrar(int id)
    {
        var model = await _service.ObtenerFormularioAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Administrar(AdministrarPermisosRolViewModel model)
    {
        try
        {
            await _service.GuardarAsync(model);
            TempData["Success"] = "Permisos del rol actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var formulario = await _service.ObtenerFormularioAsync(model.IdRol);
            return formulario == null ? NotFound() : View(formulario);
        }
    }
}
