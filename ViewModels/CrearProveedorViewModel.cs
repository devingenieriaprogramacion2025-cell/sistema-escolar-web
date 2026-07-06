using System.ComponentModel.DataAnnotations;
using SistemaEscolarWeb.Helpers;

namespace SistemaEscolarWeb.ViewModels;

public class CrearProveedorViewModel : IValidatableObject
{
    public int? IdProveedor { get; set; }

    [Required(ErrorMessage = "Debe ingresar el nombre del proveedor.")]
    [Display(Name = "Nombre proveedor")]
    [StringLength(160)]
    public string NombreProveedor { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el RUT del proveedor.")]
    [Display(Name = "RUT proveedor")]
    [StringLength(30)]
    public string RutProveedor { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    [Display(Name = "Correo")]
    [StringLength(160)]
    public string? Correo { get; set; }

    [Display(Name = "Telefono")]
    [StringLength(12)]
    [RegularExpression(@"^\+56[0-9]{9}$", ErrorMessage = "Debe ingresar un telefono chileno valido. Debe comenzar con +56. Ejemplo: +56935315783.")]
    public string? Telefono { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ChileanFormatHelper.IsValidRut(RutProveedor))
        {
            yield return new ValidationResult(
                "Debe ingresar un RUT chileno valido. Ejemplo: 12.345.678-5.",
                new[] { nameof(RutProveedor) });
        }
        else if (!ChileanFormatHelper.HasRutDotsFormat(RutProveedor))
        {
            yield return new ValidationResult(
                "Debe ingresar el RUT con puntos. Ejemplo: 12.345.678-5.",
                new[] { nameof(RutProveedor) });
        }
    }
}
