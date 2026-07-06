using Microsoft.AspNetCore.Authorization;

namespace SistemaEscolarWeb.Permissions;

public static class AppPolicies
{
    public const string PuedeGestionarUsuarios = "PuedeGestionarUsuarios";
    public const string PuedeGestionarTecnologia = "PuedeGestionarTecnologia";
    public const string PuedeAprobarReparacion = "PuedeAprobarReparacion";
    public const string PuedeAprobarBaja = "PuedeAprobarBaja";
    public const string PuedeGestionarImpresiones = "PuedeGestionarImpresiones";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(PuedeGestionarUsuarios, p => p.RequireRole(RoleNames.Administrador, RoleNames.Director));
        options.AddPolicy(PuedeGestionarTecnologia, p => p.RequireRole(RoleNames.Administrador, RoleNames.Director, RoleNames.Inspector));
        options.AddPolicy(PuedeAprobarReparacion, p => p.RequireRole(RoleNames.Administrador, RoleNames.Director, RoleNames.Inspector));
        options.AddPolicy(PuedeAprobarBaja, p => p.RequireRole(RoleNames.Administrador, RoleNames.Director, RoleNames.Inspector));
        options.AddPolicy(PuedeGestionarImpresiones, p => p.RequireRole(RoleNames.Administrador, RoleNames.Director, RoleNames.EncargadoBiblioteca));
    }
}
