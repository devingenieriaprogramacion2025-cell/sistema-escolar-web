using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEscolarWeb.Services;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class BitacoraController : Controller
{
    private readonly BitacoraService _bitacoraService;

    public BitacoraController(BitacoraService bitacoraService)
    {
        _bitacoraService = bitacoraService;
    }

    public async Task<IActionResult> Index(string ordenar = "fecha", string direccion = "desc", int pagina = 1)
    {
        var bitacora = await _bitacoraService.ListarAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        bitacora = ordenar?.ToLowerInvariant() switch
        {
            "usuario" => asc ? bitacora.OrderBy(b => b.Usuario).ToList() : bitacora.OrderByDescending(b => b.Usuario).ToList(),
            "rol" => asc ? bitacora.OrderBy(b => b.Rol).ToList() : bitacora.OrderByDescending(b => b.Rol).ToList(),
            "modulo" => asc ? bitacora.OrderBy(b => b.Modulo).ToList() : bitacora.OrderByDescending(b => b.Modulo).ToList(),
            "accion" => asc ? bitacora.OrderBy(b => b.Accion).ToList() : bitacora.OrderByDescending(b => b.Accion).ToList(),
            _ => asc ? bitacora.OrderBy(b => b.Fecha).ToList() : bitacora.OrderByDescending(b => b.Fecha).ToList()
        };

        return View(ListadoPaginado.Crear(bitacora, ordenar, direccion, pagina));
    }
}
