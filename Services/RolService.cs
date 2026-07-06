using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Permissions;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class RolService
{
    private readonly ApplicationDbContext _context;

    public RolService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Rol>> ListarAsync()
    {
        var roles = await _context.Roles.ToListAsync();
        return roles
            .OrderBy(r => RoleNames.Order(r.NombreRol))
            .ThenBy(r => r.NombreRol)
            .ToList();
    }

    public async Task<IEnumerable<SelectListItem>> SelectListAsync(int? seleccionado = null)
    {
        var roles = await ListarAsync();
        return roles.Select(r => new SelectListItem
        {
            Value = r.IdRol.ToString(),
            Text = r.NombreRol,
            Selected = seleccionado.HasValue && seleccionado.Value == r.IdRol
        });
    }

    public async Task<bool> ExisteAsync(int idRol)
    {
        return await _context.Roles.AnyAsync(r => r.IdRol == idRol);
    }

    public async Task<RolFormViewModel?> ObtenerFormularioAsync(int idRol)
    {
        var rol = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.IdRol == idRol);
        if (rol == null)
            return null;

        return new RolFormViewModel
        {
            IdRol = rol.IdRol,
            NombreRol = RoleNames.Normalize(rol.NombreRol)
        };
    }

    public async Task CrearAsync(RolFormViewModel model)
    {
        var nombre = NormalizarNombre(model.NombreRol);
        await ValidarNombreDisponibleAsync(nombre, null);

        _context.Roles.Add(new Rol { NombreRol = nombre });
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(RolFormViewModel model)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == model.IdRol);
        if (rol == null)
            throw new InvalidOperationException("El rol no existe.");

        var nombre = NormalizarNombre(model.NombreRol);
        var rolBaseActual = RoleNames.Normalize(rol.NombreRol);
        if (RoleNames.All.Contains(rolBaseActual) && nombre != rolBaseActual)
            throw new InvalidOperationException("No se puede cambiar el nombre de un rol base del sistema.");

        await ValidarNombreDisponibleAsync(nombre, model.IdRol);

        rol.NombreRol = nombre;
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(int idRol)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == idRol);
        if (rol == null)
            throw new InvalidOperationException("El rol no existe.");

        if (RoleNames.All.Contains(RoleNames.Normalize(rol.NombreRol)))
            throw new InvalidOperationException("No se puede eliminar un rol base del sistema.");

        var estaEnUso = await _context.Personal.AnyAsync(p => p.IdRol == idRol)
            || await _context.Usuarios.AnyAsync(u => u.IdRol == idRol);

        if (estaEnUso)
            throw new InvalidOperationException("No se puede eliminar el rol porque tiene personal o usuarios asociados.");

        var permisos = await _context.RolesPermisos.Where(rp => rp.IdRol == idRol).ToListAsync();
        _context.RolesPermisos.RemoveRange(permisos);
        _context.Roles.Remove(rol);
        await _context.SaveChangesAsync();
    }

    public async Task<List<RolPermisosViewModel>> ListarConPermisosAsync()
    {
        var roles = await ListarAsync();
        var rolIds = roles.Select(r => r.IdRol).ToList();

        var permisosActivos = await (
            from rolPermiso in _context.RolesPermisos.AsNoTracking()
            join permiso in _context.Permisos.AsNoTracking()
                on rolPermiso.IdPermiso equals permiso.IdPermiso
            where rolPermiso.Activo && rolIds.Contains(rolPermiso.IdRol)
            orderby permiso.NombrePermiso
            select new
            {
                rolPermiso.IdRol,
                permiso.NombrePermiso
            })
            .ToListAsync();

        var permisosPorRol = permisosActivos
            .GroupBy(p => p.IdRol)
            .ToDictionary(g => g.Key, g => g.Select(p => p.NombrePermiso).Distinct().ToList());

        return roles.Select(r => new RolPermisosViewModel
        {
            NombreRol = RoleNames.Normalize(r.NombreRol),
            Activo = true,
            Permisos = permisosPorRol.GetValueOrDefault(r.IdRol, [])
        }).ToList();
    }

    private static string NormalizarNombre(string nombre)
        => RoleNames.Normalize(nombre).Trim();

    private async Task ValidarNombreDisponibleAsync(string nombre, int? idActual)
    {
        if (!InputValidationHelper.IsSafeText(nombre, 80, required: true))
            throw new InvalidOperationException("El nombre del rol contiene caracteres no validos o supera el largo permitido.");

        var normalizado = InputValidationHelper.NormalizeKey(nombre);
        var roles = await _context.Roles.AsNoTracking().ToListAsync();
        var existe = roles.Any(r =>
            (!idActual.HasValue || r.IdRol != idActual.Value) &&
            InputValidationHelper.NormalizeKey(RoleNames.Normalize(r.NombreRol)) == normalizado);

        if (existe)
            throw new InvalidOperationException("Ya existe un rol con ese nombre.");
    }
}
