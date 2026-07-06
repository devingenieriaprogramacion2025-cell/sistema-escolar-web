namespace SistemaEscolarWeb.Services;

using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;

public class BitacoraService
{
    private readonly ApplicationDbContext _context;

    public BitacoraService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Bitacora>> ListarAsync()
    {
        return await _context.Bitacoras
            .OrderByDescending(b => b.Fecha)
            .Take(200)
            .ToListAsync();
    }

    public async Task RegistrarAsync(string usuario, string rol, string modulo, string accion)
    {
        _context.Bitacoras.Add(new Bitacora
        {
            Usuario = usuario,
            Rol = rol,
            Modulo = modulo,
            Accion = accion,
            Fecha = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }
}
