using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CrearReparacionViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un equipo.")]
    [Display(Name = "Equipo")]
    public int IdTecnologia { get; set; }

    [Required(ErrorMessage = "Debe indicar el destino.")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "El destino contiene caracteres no permitidos.")]
    public string Destino { get; set; } = "Servicio tecnico";

    [Display(Name = "Detalle")]
    [StringLength(500)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,;:#()?!%&'""/\r\n_-]*$", ErrorMessage = "El detalle contiene caracteres no permitidos.")]
    public string? Observacion { get; set; }

    public List<SelectListItem> EquiposDisponibles { get; set; } = new();
}
