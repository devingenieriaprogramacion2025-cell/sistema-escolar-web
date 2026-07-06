using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class PersonalService
{
    private readonly ApplicationDbContext _context;

    public PersonalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Personal>> ListarAsync(string? busqueda = null)
    {
        var query = _context.Personal
            .Include(p => p.Rol)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            var buscarActivo = "Activo".Contains(termino, StringComparison.OrdinalIgnoreCase);
            var buscarInactivo = "Inactivo".Contains(termino, StringComparison.OrdinalIgnoreCase);

            query = query.Where(p =>
                p.RutPersonal.Contains(termino) ||
                p.Nombre.Contains(termino) ||
                p.Apellido.Contains(termino) ||
                p.Correo.Contains(termino) ||
                (p.Cargo != null && p.Cargo.Contains(termino)) ||
                (p.Rol != null && p.Rol.NombreRol.Contains(termino)) ||
                (buscarActivo && p.Activo) ||
                (buscarInactivo && !p.Activo));
        }

        return await query
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Personal?> ObtenerAsync(string rut)
    {
        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(rut);
        return await _context.Personal
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p =>
                p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
    }

    public async Task<bool> ExisteRutAsync(string rut)
    {
        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(rut);
        return await _context.Personal.AnyAsync(p =>
            p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
    }

    public async Task<bool> ExisteCorreoAsync(string correo, string? rutExcluir = null)
    {
        var rutExcluirKey = ChileanFormatHelper.NormalizeRutLookupKey(rutExcluir);
        return await _context.Personal.AnyAsync(p =>
            p.Correo == correo &&
            (string.IsNullOrWhiteSpace(rutExcluir) ||
                p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) != rutExcluirKey));
    }

    public async Task CrearAsync(PersonalFormViewModel model)
    {
        var personal = new Personal
        {
            RutPersonal = ChileanFormatHelper.FormatRutWithDots(model.RutPersonal),
            IdRol = model.IdRol,
            Nombre = model.Nombre.Trim(),
            Apellido = model.Apellido.Trim(),
            Correo = model.Correo.Trim(),
            Telefono = ChileanFormatHelper.NormalizePhone(model.Telefono),
            Cargo = model.Cargo,
            Activo = model.Activo,
            PasswordLegacy = null
        };

        _context.Personal.Add(personal);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(PersonalFormViewModel model)
    {
        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(model.RutPersonal);
        var personal = await _context.Personal.FirstAsync(p =>
            p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
        personal.IdRol = model.IdRol;
        personal.Nombre = model.Nombre.Trim();
        personal.Apellido = model.Apellido.Trim();
        personal.Correo = model.Correo.Trim();
        personal.Telefono = ChileanFormatHelper.NormalizePhone(model.Telefono);
        personal.Cargo = model.Cargo;
        personal.Activo = model.Activo;
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> EliminarAsync(string rut, string? rutUsuarioActual)
    {
        if (string.IsNullOrWhiteSpace(rut))
            return (false, "Debe seleccionar una persona valida.");

        var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(rut);
        var rutUsuarioActualKey = ChileanFormatHelper.NormalizeRutLookupKey(rutUsuarioActual);

        if (rutKey == rutUsuarioActualKey)
            return (false, "No puede eliminar su propio registro de personal mientras esta usando el sistema.");

        var personal = await _context.Personal.FirstOrDefaultAsync(p =>
            p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
        if (personal == null)
            return (false, "La persona seleccionada no existe.");

        var rutRegistrado = personal.RutPersonal;
        var tieneAsignaciones = await _context.Asignaciones.AnyAsync(a => a.RutPersonal == rutRegistrado);
        var tieneImpresiones = await _context.SolicitudesImpresion.AnyAsync(s => s.RutPersonal == rutRegistrado);
        var tieneSalidasInsumo = await TieneSalidasInsumoAsync(rutRegistrado);

        if (tieneAsignaciones || tieneImpresiones || tieneSalidasInsumo)
        {
            personal.Activo = false;

            var usuarioConHistorial = await _context.Usuarios.FirstOrDefaultAsync(u => u.RutPersonal == rutRegistrado);
            if (usuarioConHistorial != null)
                usuarioConHistorial.Activo = false;

            await _context.SaveChangesAsync();
            return (false, "La persona tiene historial asociado. Por integridad de datos fue desactivada, no eliminada.");
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.RutPersonal == rutRegistrado);
        if (usuario != null)
            _context.Usuarios.Remove(usuario);

        _context.Personal.Remove(personal);
        await _context.SaveChangesAsync();

        return (true, "Persona eliminada correctamente.");
    }

    private async Task<bool> TieneSalidasInsumoAsync(string rut)
    {
        var existeTabla = await _context.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'Salida_insumo', N'U') IS NULL THEN 0 ELSE 1 END AS Value")
            .SingleAsync();

        if (existeTabla == 0)
            return false;

        var total = await _context.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM Salida_insumo WHERE rut_personal = @rut",
                new SqlParameter("@rut", rut))
            .SingleAsync();

        return total > 0;
    }
}
