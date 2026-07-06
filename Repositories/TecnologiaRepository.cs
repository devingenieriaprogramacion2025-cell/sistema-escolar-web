using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;

namespace SistemaEscolarWeb.Repositories;

public class TecnologiaRepository
{
    private readonly ApplicationDbContext _context;

    public TecnologiaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tecnologia>> ListarAsync()
    {
        return await _context.Tecnologias
            .OrderBy(t => t.SkuCodigoInventario)
            .ToListAsync();
    }

    public async Task<Tecnologia?> ObtenerAsync(int id)
    {
        return await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == id);
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null)
    {
        codigo = codigo.Trim();
        return await _context.Tecnologias.AnyAsync(t =>
            t.SkuCodigoInventario == codigo &&
            (!idExcluir.HasValue || t.IdTecnologia != idExcluir.Value));
    }

    public async Task AgregarAsync(Tecnologia tecnologia)
    {
        _context.Tecnologias.Add(tecnologia);
        await _context.SaveChangesAsync();
    }

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }
}
