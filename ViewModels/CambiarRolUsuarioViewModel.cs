using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CambiarRolUsuarioViewModel
{
    public int IdUsuario { get; set; }
    public string RutPersonal { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un rol.")]
    public int IdRol { get; set; }

    public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();
}
