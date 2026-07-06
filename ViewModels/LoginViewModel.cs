using System.ComponentModel.DataAnnotations;

namespace SistemaEscolarWeb.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Debe ingresar un correo electronico.")]
    [EmailAddress(ErrorMessage = "Debe ingresar un correo valido.")]
    [StringLength(160, ErrorMessage = "El correo no puede superar 160 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar una contrasena.")]
    [DataType(DataType.Password)]
    [StringLength(80, MinimumLength = 6, ErrorMessage = "La contrasena debe tener entre 6 y 80 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
