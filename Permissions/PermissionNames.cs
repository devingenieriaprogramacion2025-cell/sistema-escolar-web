using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SistemaEscolarWeb.Permissions;

public static class PermissionNames
{
    public const string ClaimType = "Permission";

    public static bool HasPermission(this ClaimsPrincipal user, params string[] permissions)
    {
        if (user.IsInRole(RoleNames.Administrador))
            return true;

        var userPermissions = user.FindAll(ClaimType)
            .Select(c => Normalize(c.Value))
            .ToHashSet();

        return permissions.Any(p => userPermissions.Contains(Normalize(p)));
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private static readonly Dictionary<string, string[]> ControllerPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dashboard"] = ["dashboard.ver"],
        ["Insumos"] = ["insumos.gestionar", "Ver insumos"],
        ["EntradasInsumo"] = ["insumos.gestionar", "Crear insumos"],
        ["SalidasInsumo"] = ["insumos.gestionar", "Editar insumos"],
        ["Proveedores"] = ["insumos.gestionar", "tecnologia.gestionar"],
        ["Tecnologia"] = ["tecnologia.gestionar", "Ver tecnología", "Ver tecnologÃ­a"],
        ["EntradasTecnologia"] = ["tecnologia.gestionar", "Crear tecnología", "Crear tecnologÃ­a"],
        ["Asignaciones"] = ["tecnologia.asignar", "Ver asignaciones"],
        ["Reparaciones"] = ["reparaciones.solicitar", "reparaciones.aprobar", "Ver reparaciones"],
        ["Bajas"] = ["bajas.solicitar", "bajas.aprobar", "Ver bajas"],
        ["Impresiones"] = ["impresiones.solicitar", "impresiones.gestionar", "Ver impresiones"],
        ["Personal"] = ["personas.ver"],
        ["Dependencias"] = ["dependencias.ver"],
        ["Usuarios"] = ["usuarios.gestionar", "Ver usuarios"],
        ["Roles"] = ["usuarios.gestionar"],
        ["PermisosRol"] = ["usuarios.gestionar"],
        ["Bitacora"] = ["Ver bitácora", "Ver bitÃ¡cora"],
        ["Reportes"] = ["Ver reportes"]
    };

    private static readonly Dictionary<string, Dictionary<string, string[]>> ActionPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Insumos"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Crear"] = ["insumos.gestionar", "Crear insumos"],
            ["Editar"] = ["insumos.gestionar", "Editar insumos"],
            ["Eliminar"] = ["insumos.gestionar", "Eliminar insumos"],
            ["Desactivar"] = ["insumos.gestionar", "Eliminar insumos"]
        },
        ["Tecnologia"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Crear"] = ["tecnologia.gestionar", "Crear tecnología", "Crear tecnologÃ­a"],
            ["Editar"] = ["tecnologia.gestionar", "Editar tecnología", "Editar tecnologÃ­a"],
            ["Eliminar"] = ["tecnologia.gestionar", "Eliminar tecnología", "Eliminar tecnologÃ­a"]
        },
        ["Asignaciones"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Crear"] = ["tecnologia.asignar", "Crear asignaciones"],
            ["RegistrarDevolucion"] = ["tecnologia.asignar", "Aprobar asignaciones"]
        },
        ["Reparaciones"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Crear"] = ["reparaciones.solicitar", "Crear reparaciones"],
            ["Aprobar"] = ["reparaciones.aprobar", "Aprobar reparaciones"],
            ["Rechazar"] = ["reparaciones.aprobar", "Aprobar reparaciones"],
            ["RegistrarRetorno"] = ["reparaciones.solicitar", "reparaciones.aprobar", "Crear reparaciones", "Aprobar reparaciones"]
        },
        ["Bajas"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Crear"] = ["bajas.solicitar", "Solicitar bajas"],
            ["Aprobar"] = ["bajas.aprobar", "Aprobar bajas"],
            ["Rechazar"] = ["bajas.aprobar", "Aprobar bajas"]
        },
        ["Impresiones"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Crear"] = ["impresiones.solicitar", "Solicitar impresiones"],
            ["EnProceso"] = ["impresiones.gestionar", "Aprobar impresiones"],
            ["Entregar"] = ["impresiones.gestionar", "Aprobar impresiones"],
            ["Rechazar"] = ["impresiones.gestionar", "Aprobar impresiones"]
        },
        ["Usuarios"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CrearAcceso"] = ["usuarios.gestionar", "Crear usuarios"],
            ["CambiarRol"] = ["usuarios.gestionar", "Editar usuarios"],
            ["ResetPassword"] = ["usuarios.gestionar", "Editar usuarios"],
            ["Desactivar"] = ["usuarios.gestionar", "Eliminar usuarios"],
            ["Reactivar"] = ["usuarios.gestionar", "Editar usuarios"]
        }
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return Task.CompletedTask;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        if (string.IsNullOrWhiteSpace(controller))
            return Task.CompletedTask;

        var required = RequiredPermissions(controller, action);
        if (required.Length > 0 && !user.HasPermission(required))
            context.Result = new ForbidResult();

        return Task.CompletedTask;
    }

    private static string[] RequiredPermissions(string controller, string? action)
    {
        if (!string.IsNullOrWhiteSpace(action) &&
            ActionPermissions.TryGetValue(controller, out var actions) &&
            actions.TryGetValue(action, out var actionPermissions))
        {
            return actionPermissions;
        }

        return ControllerPermissions.GetValueOrDefault(controller, []);
    }
}
