namespace SistemaEscolarWeb.Services;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;

public class InsumoService
{
    private const string TipoLibreria = "Libreria";
    private const string TipoMaterialAseo = "Material de Aseo";

    private readonly ApplicationDbContext _context;

    public InsumoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Insumo>> ListarAsync()
    {
        return await _context.Insumos
            .AsNoTracking()
            .OrderBy(i => i.IdTipoInsumo)
            .ThenBy(i => i.NombreInsumo)
            .ToListAsync();
    }

    public Task<Insumo?> ObtenerAsync(int id)
    {
        return _context.Insumos.FirstOrDefaultAsync(i => i.IdInsumo == id);
    }

    public async Task<List<SelectListItem>> ObtenerTiposAsync()
    {
        var libreria = await ObtenerOCrearTipoAsync(TipoLibreria);
        var materialAseo = await ObtenerOCrearTipoAsync(TipoMaterialAseo);

        return
        [
            new SelectListItem { Value = libreria.IdTipoInsumo.ToString(), Text = TipoLibreria },
            new SelectListItem { Value = materialAseo.IdTipoInsumo.ToString(), Text = TipoMaterialAseo }
        ];
    }

    public async Task<bool> EsTipoPermitidoAsync(int idTipoInsumo)
    {
        var tipo = await _context.TiposInsumo.AsNoTracking()
            .Where(t => t.IdTipoInsumo == idTipoInsumo)
            .Select(t => t.NombreTipoInsumo)
            .FirstOrDefaultAsync();

        return EsTipoPermitido(tipo);
    }

    public async Task<Dictionary<int, string>> ObtenerMapaTiposAsync()
    {
        return await _context.TiposInsumo
            .AsNoTracking()
            .ToDictionaryAsync(t => t.IdTipoInsumo, t => NormalizarTipo(t.NombreTipoInsumo));
    }

    public async Task<bool> ExisteDuplicadoAsync(int idTipoInsumo, string nombreInsumo, int? excluirId = null)
    {
        var nombre = nombreInsumo.Trim();
        return await _context.Insumos.AnyAsync(i =>
            i.IdTipoInsumo == idTipoInsumo &&
            i.NombreInsumo == nombre &&
            (!excluirId.HasValue || i.IdInsumo != excluirId.Value));
    }

    public async Task<Insumo> CrearAsync(Insumo insumo)
    {
        if (insumo.IdTipoInsumo <= 0)
            insumo.IdTipoInsumo = await ObtenerTipoDefaultAsync();

        insumo.Estado = true;
        insumo.UnidadMedida = string.IsNullOrWhiteSpace(insumo.UnidadMedida) ? "Unidad" : insumo.UnidadMedida.Trim();
        insumo.NombreInsumo = insumo.NombreInsumo.Trim();
        insumo.DescripcionInsumo = insumo.DescripcionInsumo?.Trim();
        insumo.Toxicidad = string.IsNullOrWhiteSpace(insumo.Toxicidad) ? "No toxico" : insumo.Toxicidad.Trim();

        _context.Insumos.Add(insumo);
        await _context.SaveChangesAsync();

        return insumo;
    }

    public async Task ActualizarAsync(Insumo insumo)
    {
        var existente = await ObtenerAsync(insumo.IdInsumo);
        if (existente == null)
            throw new InvalidOperationException("El insumo no existe.");

        existente.IdTipoInsumo = insumo.IdTipoInsumo;
        existente.NombreInsumo = insumo.NombreInsumo.Trim();
        existente.DescripcionInsumo = string.IsNullOrWhiteSpace(insumo.DescripcionInsumo) ? null : insumo.DescripcionInsumo.Trim();
        existente.UnidadMedida = insumo.UnidadMedida.Trim();
        existente.StockActual = insumo.StockActual;
        existente.StockMinimo = insumo.StockMinimo;
        existente.Estado = insumo.Estado;
        existente.Toxicidad = string.IsNullOrWhiteSpace(insumo.Toxicidad) ? "No toxico" : insumo.Toxicidad.Trim();

        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> EliminarAsync(int id)
    {
        var insumo = await ObtenerAsync(id);
        if (insumo == null)
            return (false, "El insumo no existe.");

        insumo.Estado = false;
        await _context.SaveChangesAsync();
        return (true, "Insumo desactivado correctamente.");
    }

    private async Task<int> ObtenerTipoDefaultAsync()
    {
        var tipos = await ObtenerTiposAsync();
        return tipos
            .Select(t => int.TryParse(t.Value, out var value) ? value : 0)
            .First(value => value > 0);
    }

    private async Task<TipoInsumo> ObtenerOCrearTipoAsync(string nombreCanonico)
    {
        var clave = NormalizarClave(nombreCanonico);
        var tipos = await _context.TiposInsumo.ToListAsync();
        var tipo = tipos
            .OrderBy(t => t.IdTipoInsumo)
            .FirstOrDefault(t => NormalizarClave(t.NombreTipoInsumo) == clave);

        if (tipo != null)
            return tipo;

        tipo = new TipoInsumo { NombreTipoInsumo = nombreCanonico };
        _context.TiposInsumo.Add(tipo);
        await _context.SaveChangesAsync();

        return tipo;
    }

    private static bool EsTipoPermitido(string? tipo)
    {
        var clave = NormalizarClave(tipo);
        return clave is "LIBRERIA" or "MATERIAL DE ASEO";
    }

    private static string NormalizarTipo(string tipo)
    {
        return NormalizarClave(tipo) switch
        {
            "LIBRERIA" => TipoLibreria,
            "MATERIAL DE ASEO" => TipoMaterialAseo,
            _ => tipo
        };
    }

    private static string NormalizarClave(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var normalizado = valor.Trim().Normalize(System.Text.NormalizationForm.FormD);
        var caracteres = normalizado
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(caracteres).Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
    }
}
