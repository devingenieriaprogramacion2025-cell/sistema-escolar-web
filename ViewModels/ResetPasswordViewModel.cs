using System.ComponentModel.DataAnnotations;

namespace SistemaEscolarWeb.ViewModels;

public class ResetPasswordViewModel
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar una contrasena temporal.")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    [StringLength(80, ErrorMessage = "La contrasena no puede superar 80 caracteres.")]
    [Display(Name = "Nueva contrasena temporal")]
    public string PasswordTemporal { get; set; } = "Admin123*";
}
