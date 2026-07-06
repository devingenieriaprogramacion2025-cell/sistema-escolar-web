using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Permissions;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class PermisoRolService
{
    private readonly ApplicationDbContext _context;

    public PermisoRolService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Rol>> ListarRolesAsync()
    {
        var roles = await _context.Roles.AsNoTracking().ToListAsync();
        return roles
            .OrderBy(r => RoleNames.Order(r.NombreRol))
            .ThenBy(r => r.NombreRol)
            .ToList();
    }

    public async Task<AdministrarPermisosRolViewModel?> ObtenerFormularioAsync(int idRol)
    {
        var rol = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.IdRol == idRol);
        if (rol == null)
            return null;

        var permisos = await _context.Permisos.AsNoTracking()
            .OrderBy(p => p.IdPermiso)
            .ToListAsync();

        var asignados = await _context.RolesPermisos.AsNoTracking()
            .Where(rp => rp.IdRol == idRol && rp.Activo)
            .Select(rp => rp.IdPermiso)
            .ToListAsync();
        var asignadosSet = asignados.ToHashSet();

        return new AdministrarPermisosRolViewModel
        {
            IdRol = rol.IdRol,
            NombreRol = RoleNames.Normalize(rol.NombreRol),
            Permisos = permisos.Select(p => new PermisoCheckboxViewModel
            {
                IdPermiso = p.IdPermiso,
                NombrePermiso = p.NombrePermiso,
                Seleccionado = asignadosSet.Contains(p.IdPermiso)
            }).ToList()
        };
    }

    public async Task GuardarAsync(AdministrarPermisosRolViewModel model)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == model.IdRol);
        if (rol == null)
            throw new InvalidOperationException("El rol no existe.");

        var permisosValidos = await _context.Permisos
            .AsNoTracking()
            .Select(p => p.IdPermiso)
            .ToListAsync();
        var permisosValidosSet = permisosValidos.ToHashSet();
        var seleccionados = model.Permisos
            .Where(p => p.Seleccionado && permisosValidosSet.Contains(p.IdPermiso))
            .Select(p => p.IdPermiso)
            .ToHashSet();

        if (RoleNames.Normalize(rol.NombreRol) == RoleNames.Administrador)
            seleccionados = permisosValidosSet;

        var registros = await _context.RolesPermisos
            .Where(rp => rp.IdRol == model.IdRol)
            .ToListAsync();

        foreach (var idPermiso in permisosValidosSet)
        {
            var registro = registros.FirstOrDefault(rp => rp.IdPermiso == idPermiso);
            var activo = seleccionados.Contains(idPermiso);

            if (registro == null)
            {
                _context.RolesPermisos.Add(new RolPermiso
                {
                    IdRol = model.IdRol,
                    IdPermiso = idPermiso,
                    FechaRol = DateTime.Now,
                    Activo = activo
                });
            }
            else
            {
                registro.Activo = activo;
                if (activo)
                    registro.FechaRol = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
    }
}
