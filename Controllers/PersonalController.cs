using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class PersonalController : Controller
{
    private readonly PersonalService _personalService;
    private readonly UsuarioService _usuarioService;
    private readonly RolService _rolService;

    public PersonalController(PersonalService personalService, UsuarioService usuarioService, RolService rolService)
    {
        _personalService = personalService;
        _usuarioService = usuarioService;
        _rolService = rolService;
    }

    public async Task<IActionResult> Index(string? busqueda, string ordenar = "nombre", string direccion = "asc", int pagina = 1)
    {
        ViewBag.Busqueda = busqueda;
        var personal = await _personalService.ListarAsync(busqueda);
        var asc = !ListadoPaginado.EsDescendente(direccion);
        personal = ordenar?.ToLowerInvariant() switch
        {
            "rut" => asc ? personal.OrderBy(p => p.RutPersonal).ToList() : personal.OrderByDescending(p => p.RutPersonal).ToList(),
            "correo" => asc ? personal.OrderBy(p => p.Correo).ToList() : personal.OrderByDescending(p => p.Correo).ToList(),
            "cargo" => asc ? personal.OrderBy(p => p.Cargo).ToList() : personal.OrderByDescending(p => p.Cargo).ToList(),
            "rol" => asc ? personal.OrderBy(p => p.Rol?.NombreRol).ToList() : personal.OrderByDescending(p => p.Rol?.NombreRol).ToList(),
            "estado" => asc ? personal.OrderBy(p => p.Activo).ToList() : personal.OrderByDescending(p => p.Activo).ToList(),
            _ => asc ? personal.OrderBy(p => p.Nombre).ThenBy(p => p.Apellido).ToList() : personal.OrderByDescending(p => p.Nombre).ThenByDescending(p => p.Apellido).ToList()
        };

        return View(ListadoPaginado.Crear(personal, ordenar, direccion, pagina, busqueda));
    }

    public async Task<IActionResult> Detalle(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var personal = await _personalService.ObtenerAsync(id);
        if (personal == null) return NotFound();
        ViewBag.TieneUsuario = await _usuarioService.TieneAccesoAsync(id);
        return View(personal);
    }

    [Authorize]
    public async Task<IActionResult> Crear()
    {
        var model = new PersonalFormViewModel { Roles = await _rolService.SelectListAsync() };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PersonalFormViewModel model)
    {
        if (!await _rolService.ExisteAsync(model.IdRol))
            ModelState.AddModelError(nameof(model.IdRol), "Debe seleccionar un rol valido.");

        if (await _personalService.ExisteRutAsync(model.RutPersonal))
            ModelState.AddModelError(nameof(model.RutPersonal), "Ya existe una persona registrada con este RUT.");

        if (await _personalService.ExisteCorreoAsync(model.Correo))
            ModelState.AddModelError(nameof(model.Correo), "Ya existe una persona registrada con este correo.");

        if (!ModelState.IsValid)
        {
            model.Roles = await _rolService.SelectListAsync(model.IdRol);
            return View(model);
        }

        await _personalService.CrearAsync(model);
        TempData["Success"] = "Persona registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Editar(string id)
    {
        var personal = await _personalService.ObtenerAsync(id);
        if (personal == null) return NotFound();

        var model = new PersonalFormViewModel
        {
            RutPersonal = ChileanFormatHelper.FormatRutWithDots(personal.RutPersonal),
            IdRol = personal.IdRol,
            Nombre = personal.Nombre,
            Apellido = personal.Apellido,
            Correo = personal.Correo,
            Telefono = personal.Telefono,
            Cargo = personal.Cargo,
            Activo = personal.Activo,
            Roles = await _rolService.SelectListAsync(personal.IdRol)
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(PersonalFormViewModel model)
    {
        if (!await _rolService.ExisteAsync(model.IdRol))
            ModelState.AddModelError(nameof(model.IdRol), "Debe seleccionar un rol valido.");

        if (await _personalService.ExisteCorreoAsync(model.Correo, model.RutPersonal))
            ModelState.AddModelError(nameof(model.Correo), "Ya existe otra persona registrada con este correo.");

        if (!ModelState.IsValid)
        {
            model.Roles = await _rolService.SelectListAsync(model.IdRol);
            return View(model);
        }

        await _personalService.ActualizarAsync(model);
        TempData["Success"] = "Datos de personal actualizados correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = model.RutPersonal });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string id)
    {
        var resultado = await _personalService.EliminarAsync(id, User.GetRutPersonal());
        TempData[resultado.Success ? "Success" : "Warning"] = resultado.Message;
        return RedirectToAction(nameof(Index));
    }
}
