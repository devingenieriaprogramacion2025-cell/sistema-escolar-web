using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class UsuariosController : Controller
{
    private readonly UsuarioService _usuarioService;
    private readonly PersonalService _personalService;
    private readonly RolService _rolService;

    public UsuariosController(UsuarioService usuarioService, PersonalService personalService, RolService rolService)
    {
        _usuarioService = usuarioService;
        _personalService = personalService;
        _rolService = rolService;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "persona", string direccion = "asc", int pagina = 1)
    {
        ViewBag.Busqueda = busqueda;
        var usuarios = await _usuarioService.ListarAsync(busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        usuarios = ordenar?.ToLowerInvariant() switch
        {
            "correo" => asc ? usuarios.OrderBy(u => u.Personal?.Correo).ToList() : usuarios.OrderByDescending(u => u.Personal?.Correo).ToList(),
            "rol" => asc ? usuarios.OrderBy(u => u.Rol?.NombreRol).ToList() : usuarios.OrderByDescending(u => u.Rol?.NombreRol).ToList(),
            "acceso" => asc ? usuarios.OrderBy(u => u.UltimoAcceso).ToList() : usuarios.OrderByDescending(u => u.UltimoAcceso).ToList(),
            "estado" => asc ? usuarios.OrderBy(u => u.Activo).ToList() : usuarios.OrderByDescending(u => u.Activo).ToList(),
            _ => asc ? usuarios.OrderBy(u => u.Personal?.Nombre).ThenBy(u => u.Personal?.Apellido).ToList() : usuarios.OrderByDescending(u => u.Personal?.Nombre).ThenByDescending(u => u.Personal?.Apellido).ToList()
        };

        return View(ListadoPaginado.Crear(usuarios, ordenar, direccion, pagina, busqueda));
    }

    public async Task<IActionResult> CrearAcceso(string rut)
    {
        var personal = await _personalService.ObtenerAsync(rut);
        if (personal == null) return NotFound();

        if (await _usuarioService.TieneAccesoAsync(rut))
        {
            TempData["Warning"] = "La persona seleccionada ya posee acceso al sistema.";
            return RedirectToAction("Detalle", "Personal", new { id = rut });
        }

        var model = new CrearUsuarioViewModel
        {
            RutPersonal = ChileanFormatHelper.NormalizeRut(personal.RutPersonal),
            NombreCompleto = $"{personal.Nombre} {personal.Apellido}",
            Correo = personal.Correo,
            Cargo = personal.Cargo ?? string.Empty,
            IdRol = personal.IdRol,
            Roles = await _rolService.SelectListAsync(personal.IdRol)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAcceso(CrearUsuarioViewModel model)
    {
        var personal = await _personalService.ObtenerAsync(model.RutPersonal);
        if (personal == null) return NotFound();

        if (!await _rolService.ExisteAsync(model.IdRol))
            ModelState.AddModelError(nameof(model.IdRol), "Debe seleccionar un rol valido.");

        if (!ModelState.IsValid)
        {
            model.NombreCompleto = $"{personal.Nombre} {personal.Apellido}";
            model.Correo = personal.Correo;
            model.Cargo = personal.Cargo ?? string.Empty;
            model.Roles = await _rolService.SelectListAsync(model.IdRol);
            return View(model);
        }

        try
        {
            await _usuarioService.CrearAccesoAsync(model);
            TempData["Success"] = "Acceso de usuario creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.NombreCompleto = $"{personal.Nombre} {personal.Apellido}";
            model.Correo = personal.Correo;
            model.Cargo = personal.Cargo ?? string.Empty;
            model.Roles = await _rolService.SelectListAsync(model.IdRol);
            return View(model);
        }
    }

    public async Task<IActionResult> CambiarRol(int id)
    {
        var usuario = await _usuarioService.ObtenerAsync(id);
        if (usuario == null || usuario.Personal == null) return NotFound();

        var model = new CambiarRolUsuarioViewModel
        {
            IdUsuario = usuario.IdUsuario,
            RutPersonal = usuario.RutPersonal,
            NombreCompleto = $"{usuario.Personal.Nombre} {usuario.Personal.Apellido}",
            Correo = usuario.Personal.Correo,
            IdRol = usuario.IdRol,
            Roles = await _rolService.SelectListAsync(usuario.IdRol)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(CambiarRolUsuarioViewModel model)
    {
        if (!await _rolService.ExisteAsync(model.IdRol))
            ModelState.AddModelError(nameof(model.IdRol), "Debe seleccionar un rol valido.");

        if (!ModelState.IsValid)
        {
            model.Roles = await _rolService.SelectListAsync(model.IdRol);
            return View(model);
        }

        await _usuarioService.CambiarRolAsync(model.IdUsuario, model.IdRol);
        TempData["Success"] = "Rol de usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(int id)
    {
        var usuario = await _usuarioService.ObtenerAsync(id);
        if (usuario == null || usuario.Personal == null) return NotFound();

        return View(new ResetPasswordViewModel
        {
            IdUsuario = usuario.IdUsuario,
            NombreCompleto = $"{usuario.Personal.Nombre} {usuario.Personal.Apellido}",
            Correo = usuario.Personal.Correo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        await _usuarioService.ResetPasswordAsync(model.IdUsuario, model.PasswordTemporal);
        TempData["Success"] = "Contraseña restablecida correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _usuarioService.CambiarEstadoAsync(id, false);
        TempData["Success"] = "Usuario desactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        await _usuarioService.CambiarEstadoAsync(id, true);
        TempData["Success"] = "Usuario reactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
