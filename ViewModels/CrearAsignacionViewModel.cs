using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CrearAsignacionViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Debe seleccionar un equipo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un equipo.")]
    [Display(Name = "Equipo")]
    public int IdTecnologia { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el tipo de destinatario.")]
    [Display(Name = "Tipo de destinatario")]
    [RegularExpression("^(Persona|Dependencia)$", ErrorMessage = "Debe seleccionar un tipo de destinatario valido.")]
    public string TipoDestinatario { get; set; } = "Persona";

    [Display(Name = "Persona")]
    [RegularExpression(@"^[0-9]{7,8}-[0-9Kk]$", ErrorMessage = "Debe seleccionar un RUT valido.")]
    public string? RutPersonal { get; set; }

    [Display(Name = "Dependencia")]
    public int? IdDependencia { get; set; }

    [Required(ErrorMessage = "Debe indicar el tipo de asignacion.")]
    [StringLength(80)]
    [Display(Name = "Tipo de asignacion")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "El tipo de asignacion contiene caracteres no permitidos.")]
    public string TipoAsignacion { get; set; } = "Uso institucional";

    [Display(Name = "Observacion")]
    [StringLength(250)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,;:#()?!%&'""/\r\n_-]*$", ErrorMessage = "La observacion contiene caracteres no permitidos.")]
    public string? Observacion { get; set; }

    public List<SelectListItem> EquiposDisponibles { get; set; } = new();
    public List<SelectListItem> PersonalDisponible { get; set; } = new();
    public List<SelectListItem> Dependencias { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var tienePersona = !string.IsNullOrWhiteSpace(RutPersonal);
        var tieneDependencia = IdDependencia.HasValue;

        if (tienePersona && tieneDependencia)
        {
            yield return new ValidationResult(
                "Debe seleccionar solo una persona o una dependencia, no ambas.",
                new[] { nameof(RutPersonal), nameof(IdDependencia) });
        }

        if (!tienePersona && !tieneDependencia)
        {
            yield return new ValidationResult(
                "Debe seleccionar una persona o una dependencia.",
                new[] { nameof(TipoDestinatario) });
        }

        if (TipoDestinatario == "Persona" && !tienePersona)
        {
            yield return new ValidationResult(
                "Debe seleccionar una persona.",
                new[] { nameof(RutPersonal) });
        }

        if (TipoDestinatario == "Dependencia" && !tieneDependencia)
        {
            yield return new ValidationResult(
                "Debe seleccionar una dependencia.",
                new[] { nameof(IdDependencia) });
        }
    }
}
