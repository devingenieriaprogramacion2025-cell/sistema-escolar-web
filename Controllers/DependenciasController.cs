using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Controllers;

[Authorize]
public class DependenciasController : Controller
{
    private readonly ApplicationDbContext _context;

    public DependenciasController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string ordenar = "nombre", string direccion = "asc", int pagina = 1)
    {
        var dependencias = await _context.Dependencias.AsNoTracking().ToListAsync();
        var asc = !ListadoPaginado.EsDescendente(direccion);
        dependencias = ordenar?.ToLowerInvariant() switch
        {
            "id" => asc ? dependencias.OrderBy(d => d.IdDependencia).ToList() : dependencias.OrderByDescending(d => d.IdDependencia).ToList(),
            "responsable" => asc ? dependencias.OrderBy(d => d.ResponsableDependencia).ToList() : dependencias.OrderByDescending(d => d.ResponsableDependencia).ToList(),
            _ => asc ? dependencias.OrderBy(d => d.NombreDependencia).ToList() : dependencias.OrderByDescending(d => d.NombreDependencia).ToList()
        };

        return View(ListadoPaginado.Crear(dependencias, ordenar, direccion, pagina));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Dependencia dependencia)
    {
        if (!InputValidationHelper.IsSafeText(dependencia.NombreDependencia, 160, required: true))
        {
            TempData["Error"] = "Debe ingresar un nombre de dependencia valido.";
            return RedirectToAction(nameof(Index));
        }

        if (!InputValidationHelper.IsSafeText(dependencia.ResponsableDependencia, 160, required: false))
        {
            TempData["Error"] = "El responsable contiene caracteres no permitidos o supera el largo permitido.";
            return RedirectToAction(nameof(Index));
        }

        var nombreNormalizado = InputValidationHelper.NormalizeKey(dependencia.NombreDependencia);
        var dependencias = await _context.Dependencias.AsNoTracking().ToListAsync();
        if (dependencias.Any(d => InputValidationHelper.NormalizeKey(d.NombreDependencia) == nombreNormalizado))
        {
            TempData["Error"] = "La dependencia ya existe. No se permiten duplicados por tildes o variaciones de escritura.";
            return RedirectToAction(nameof(Index));
        }

        var nuevaDependencia = new Dependencia
        {
            IdTipoDependencia = dependencia.IdTipoDependencia > 0 ? dependencia.IdTipoDependencia : 1,
            NombreDependencia = dependencia.NombreDependencia.Trim(),
            ResponsableDependencia = string.IsNullOrWhiteSpace(dependencia.ResponsableDependencia)
                ? null
                : dependencia.ResponsableDependencia.Trim()
        };

        _context.Dependencias.Add(nuevaDependencia);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Dependencia registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
