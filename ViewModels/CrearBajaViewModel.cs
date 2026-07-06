using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CrearBajaViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un equipo.")]
    [Display(Name = "Equipo")]
    public int IdTecnologia { get; set; }

    [Required(ErrorMessage = "Debe indicar un motivo.")]
    [Range(1, 4, ErrorMessage = "Debe seleccionar un motivo valido.")]
    [Display(Name = "Motivo")]
    public int IdMotivo { get; set; } = 1;

    [Display(Name = "Detalle")]
    [StringLength(500)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,;:#()?!%&'""/\r\n_-]*$", ErrorMessage = "El detalle contiene caracteres no permitidos.")]
    public string? Observacion { get; set; }

    public List<SelectListItem> EquiposDisponibles { get; set; } = new();
    public List<SelectListItem> Motivos { get; set; } = new();
}
