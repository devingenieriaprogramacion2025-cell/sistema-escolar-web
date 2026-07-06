using System.Security.Claims;

namespace SistemaEscolarWeb.Helpers;

public static class SessionHelper
{
    public static string? GetRutPersonal(this ClaimsPrincipal user) => user.FindFirst("RutPersonal")?.Value;
    public static string? GetRol(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value;
}
