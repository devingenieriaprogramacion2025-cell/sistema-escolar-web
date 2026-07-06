using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Permissions;

namespace SistemaEscolarWeb.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthResult> ValidateUserAsync(string correo, string password)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Personal)
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Personal != null && u.Personal.Correo == correo);

        if (usuario == null)
            return AuthResult.Fail("Credenciales inválidas. Verifique su correo y contraseña.");

        if (!usuario.Activo || usuario.Personal == null || !usuario.Personal.Activo)
            return AuthResult.Fail("Su cuenta se encuentra deshabilitada. Contacte al administrador.");

        var valido = false;
        try
        {
            valido = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        }
        catch
        {
            valido = usuario.PasswordHash == password;
        }

        if (!valido)
            return AuthResult.Fail("Credenciales inválidas. Verifique su correo y contraseña.");

        usuario.UltimoAcceso = DateTime.Now;
        await _context.SaveChangesAsync();

        var nombreCompleto = $"{usuario.Personal.Nombre} {usuario.Personal.Apellido}".Trim();
        var rol = RoleNames.Normalize(usuario.Rol?.NombreRol ?? usuario.Personal.Rol?.NombreRol);
        var permisos = await _context.RolesPermisos
            .AsNoTracking()
            .Where(rp => rp.IdRol == usuario.IdRol && rp.Activo)
            .Join(_context.Permisos.AsNoTracking(),
                rolPermiso => rolPermiso.IdPermiso,
                permiso => permiso.IdPermiso,
                (_, permiso) => permiso.NombrePermiso)
            .Distinct()
            .ToListAsync();

        return AuthResult.Ok(usuario.IdUsuario, usuario.RutPersonal, nombreCompleto, usuario.Personal.Correo, rol, permisos);
    }
}

public class AuthResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int IdUsuario { get; private set; }
    public string RutPersonal { get; private set; } = string.Empty;
    public string NombreCompleto { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string Rol { get; private set; } = string.Empty;
    public List<string> Permisos { get; private set; } = [];

    public static AuthResult Fail(string message) => new() { Success = false, Message = message };

    public static AuthResult Ok(int idUsuario, string rutPersonal, string nombreCompleto, string correo, string rol, List<string> permisos) => new()
    {
        Success = true,
        IdUsuario = idUsuario,
        RutPersonal = rutPersonal,
        NombreCompleto = nombreCompleto,
        Correo = correo,
        Rol = rol,
        Permisos = permisos
    };
}
