using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;

namespace SistemaEscolarWeb.Repositories;

public class ProveedorRepository
{
    private readonly ApplicationDbContext _context;

    public ProveedorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Proveedor>> ListarAsync()
        => await _context.Proveedores.AsNoTracking().ToListAsync();

    public async Task<Proveedor?> ObtenerAsync(int id)
        => await _context.Proveedores.FirstOrDefaultAsync(p => p.IdProveedor == id);

    public async Task<bool> ExisteRutAsync(string rutProveedor, int? idExcluir = null)
    {
        var rut = rutProveedor.Trim().ToUpperInvariant().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
        return await _context.Proveedores.AnyAsync(p =>
            p.RutProveedor.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rut &&
            (!idExcluir.HasValue || p.IdProveedor != idExcluir.Value));
    }

    public async Task<bool> TieneEntradasAsync(int id)
        => await _context.EntradasInsumo.AnyAsync(e => e.IdProveedor == id) ||
           await _context.EntradasTecnologia.AnyAsync(e => e.IdProveedor == id);

    public async Task AgregarAsync(Proveedor proveedor)
    {
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();
    }

    public async Task GuardarAsync()
        => await _context.SaveChangesAsync();
}
