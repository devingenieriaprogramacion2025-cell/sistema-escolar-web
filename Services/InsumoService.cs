namespace SistemaEscolarWeb.Services;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;

public class InsumoService
{
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
        var tipos = await _context.TiposInsumo.AsNoTracking()
            .Where(t => t.NombreTipoInsumo == "Material de aseo" ||
                t.NombreTipoInsumo == "Libreria" ||
                t.NombreTipoInsumo == "Librería" ||
                t.NombreTipoInsumo == "LibrerÃ­a")
            .OrderBy(t => t.NombreTipoInsumo)
            .ToListAsync();

        var libreria = tipos
            .Where(t => t.NombreTipoInsumo == "Libreria" ||
                t.NombreTipoInsumo == "Librería" ||
                t.NombreTipoInsumo == "LibrerÃ­a")
            .OrderBy(t => t.IdTipoInsumo)
            .FirstOrDefault();
        var materialAseo = tipos.FirstOrDefault(t => t.NombreTipoInsumo == "Material de aseo");

        var resultado = new List<SelectListItem>();
        if (libreria != null)
            resultado.Add(new SelectListItem { Value = libreria.IdTipoInsumo.ToString(), Text = "Librería" });
        if (materialAseo != null)
            resultado.Add(new SelectListItem { Value = materialAseo.IdTipoInsumo.ToString(), Text = materialAseo.NombreTipoInsumo });

        return resultado;
    }

    public Task<bool> EsTipoPermitidoAsync(int idTipoInsumo)
    {
        return _context.TiposInsumo.AnyAsync(t => t.IdTipoInsumo == idTipoInsumo &&
            (t.NombreTipoInsumo == "Material de aseo" ||
             t.NombreTipoInsumo == "Libreria" ||
             t.NombreTipoInsumo == "Librería" ||
             t.NombreTipoInsumo == "LibrerÃ­a"));
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

    private static string NormalizarTipo(string tipo)
    {
        return tipo is "Libreria" or "LibrerÃ­a" ? "Librería" : tipo;
    }

    private async Task<int> ObtenerTipoDefaultAsync()
    {
        var id = await _context.Database.SqlQueryRaw<int>("SELECT TOP 1 id_tipoinsumo AS Value FROM Tipo_insumo ORDER BY id_tipoinsumo").FirstOrDefaultAsync();
        if (id > 0) return id;

        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Tipo_insumo (nombre_tipoinsumo) VALUES (N'General')");
        return await _context.Database.SqlQueryRaw<int>("SELECT TOP 1 id_tipoinsumo AS Value FROM Tipo_insumo ORDER BY id_tipoinsumo").FirstAsync();
    }
}
