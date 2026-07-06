using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class UsuarioService
{
    private readonly ApplicationDbContext _context;

    public UsuarioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> ListarAsync(string? busqueda = null)
    {
        var query = _context.Usuarios
            .Include(u => u.Personal)
            .Include(u => u.Rol)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            var buscarActivo = "Activo".Contains(termino, StringComparison.OrdinalIgnoreCase);
            var buscarInactivo = "Inactivo".Contains(termino, StringComparison.OrdinalIgnoreCase);
            query = query.Where(u =>
                u.RutPersonal.Contains(termino) ||
                (u.Personal != null && (
                    u.Personal.Nombre.Contains(termino) ||
                    u.Personal.Apellido.Contains(termino) ||
                    u.Personal.Correo.Contains(termino))) ||
                (u.Rol != null && u.Rol.NombreRol.Contains(termino)) ||
                (buscarActivo && u.Activo) ||
                (buscarInactivo && !u.Activo));
        }

        return await query
            .OrderBy(u => u.Personal!.Apellido)
            .ThenBy(u => u.Personal!.Nombre)
            .ToListAsync();
    }

    public async Task<Usuario?> ObtenerAsync(int id)
    {
        return await _context.Usuarios
            .Include(u => u.Personal)
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == id);
    }

    public async Task<Usuario?> ObtenerPorRutAsync(string rut)
    {
        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(rut);
        return await _context.Usuarios
            .Include(u => u.Personal)
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u =>
                u.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
    }

    public async Task<bool> TieneAccesoAsync(string rut)
    {
        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(rut);
        return await _context.Usuarios.AnyAsync(u =>
            u.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
    }

    public async Task CrearAccesoAsync(CrearUsuarioViewModel model)
    {
        if (await TieneAccesoAsync(model.RutPersonal))
            throw new InvalidOperationException("La persona seleccionada ya posee acceso al sistema.");

        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(model.RutPersonal);
        var personal = await _context.Personal.FirstOrDefaultAsync(p =>
            p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
        if (personal == null)
            throw new InvalidOperationException("La persona seleccionada no existe.");

        personal.IdRol = model.IdRol;
        personal.Activo = true;
        personal.PasswordLegacy = null;

        var usuario = new Usuario
        {
            RutPersonal = personal.RutPersonal,
            IdRol = model.IdRol,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordTemporal),
            Activo = model.Activo,
            CreadoEn = DateTime.Now
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task CambiarRolAsync(int idUsuario, int idRol)
    {
        var usuario = await _context.Usuarios.FirstAsync(u => u.IdUsuario == idUsuario);
        usuario.IdRol = idRol;

        var personal = await _context.Personal.FirstAsync(p => p.RutPersonal == usuario.RutPersonal);
        personal.IdRol = idRol;

        await _context.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int idUsuario, bool activo)
    {
        var usuario = await _context.Usuarios.FirstAsync(u => u.IdUsuario == idUsuario);
        usuario.Activo = activo;
        await _context.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(int idUsuario, string passwordTemporal)
    {
        var usuario = await _context.Usuarios.FirstAsync(u => u.IdUsuario == idUsuario);
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordTemporal);
        await _context.SaveChangesAsync();
    }
}
