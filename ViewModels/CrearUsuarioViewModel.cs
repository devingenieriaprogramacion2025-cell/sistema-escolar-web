using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "Debe seleccionar una persona.")]
    [RegularExpression(@"^([0-9]{7,8}|[0-9]{1,2}\.[0-9]{3}\.[0-9]{3})-[0-9Kk]$", ErrorMessage = "Debe seleccionar un RUT valido.")]
    public string RutPersonal { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un rol.")]
    [Display(Name = "Rol de acceso")]
    public int IdRol { get; set; }

    [Required(ErrorMessage = "Debe ingresar una contrasena temporal.")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    [StringLength(80, ErrorMessage = "La contrasena no puede superar 80 caracteres.")]
    [Display(Name = "Contrasena temporal")]
    public string PasswordTemporal { get; set; } = "Admin123*";

    public bool Activo { get; set; } = true;
    public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();
}
