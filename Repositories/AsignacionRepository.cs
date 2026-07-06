using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;

namespace SistemaEscolarWeb.Repositories;

public class AsignacionRepository
{
    private readonly ApplicationDbContext _context;

    public AsignacionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Asignacion>> ListarAsync()
    {
        return await _context.Asignaciones
            .OrderByDescending(a => a.FechaAsignacion)
            .ToListAsync();
    }

    public async Task<List<Asignacion>> ListarPorPersonaAsync(string rutPersonal)
    {
        return await _context.Asignaciones
            .Where(a => a.RutPersonal == rutPersonal)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToListAsync();
    }

    public async Task<Asignacion?> ObtenerAsync(int id)
    {
        return await _context.Asignaciones.FirstOrDefaultAsync(a => a.IdAsignaciones == id);
    }

    public async Task<bool> EquipoTieneAsignacionActivaAsync(int idTecnologia)
    {
        return await _context.Asignaciones.AnyAsync(a =>
            a.IdTecnologia == idTecnologia &&
            a.FechaDevolucion == null &&
            (a.EstadoAsignacion == "Activa" || a.EstadoAsignacion == "Vigente"));
    }

    public async Task AgregarAsync(Asignacion asignacion)
    {
        _context.Asignaciones.Add(asignacion);
        await _context.SaveChangesAsync();
    }

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }
}
