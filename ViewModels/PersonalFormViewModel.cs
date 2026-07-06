using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolarWeb.Helpers;

namespace SistemaEscolarWeb.ViewModels;

public class PersonalFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Debe ingresar el RUT.")]
    [Display(Name = "RUT")]
    [StringLength(12)]
    public string RutPersonal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un rol.")]
    [Display(Name = "Rol")]
    public int IdRol { get; set; }

    [Required(ErrorMessage = "Debe ingresar el nombre.")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ '-]+$", ErrorMessage = "El nombre contiene caracteres no permitidos.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el apellido.")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ '-]+$", ErrorMessage = "El apellido contiene caracteres no permitidos.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el correo.")]
    [EmailAddress(ErrorMessage = "Debe ingresar un correo valido.")]
    [StringLength(160)]
    public string Correo { get; set; } = string.Empty;

    [StringLength(30)]
    [RegularExpression(@"^\+56[0-9]{9}$", ErrorMessage = "Debe ingresar un telefono chileno valido. Debe comenzar con +56. Ejemplo: +56935315783.")]
    public string? Telefono { get; set; }

    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]*$", ErrorMessage = "El cargo contiene caracteres no permitidos.")]
    public string? Cargo { get; set; }

    public bool Activo { get; set; } = true;
    public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ChileanFormatHelper.IsValidRut(RutPersonal))
        {
            yield return new ValidationResult(
                "Debe ingresar un RUT chileno valido. Ejemplo: 12.345.678-5.",
                new[] { nameof(RutPersonal) });
        }
        else if (!ChileanFormatHelper.HasRutDotsFormat(RutPersonal))
        {
            yield return new ValidationResult(
                "Debe ingresar el RUT con puntos. Ejemplo: 12.345.678-5.",
                new[] { nameof(RutPersonal) });
        }
    }
}
