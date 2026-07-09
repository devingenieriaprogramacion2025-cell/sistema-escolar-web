using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CrearImpresionViewModel
{
    [Required(ErrorMessage = "Debe seleccionar una persona.")]
    [Display(Name = "Solicitante")]
    public string RutPersonal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe indicar la cantidad de paginas.")]
    [Range(1, 9999, ErrorMessage = "La cantidad de paginas debe estar entre 1 y 9999.")]
    [Display(Name = "Cantidad de paginas del documento")]
    public int CantidadPaginas { get; set; } = 1;

    [Required(ErrorMessage = "Debe indicar la cantidad de copias.")]
    [Range(1, 9999, ErrorMessage = "La cantidad de copias debe estar entre 1 y 9999.")]
    [Display(Name = "Cantidad de copias solicitadas")]
    public int CantidadCopias { get; set; } = 1;

    [Required(ErrorMessage = "Debe indicar el dia de entrega requerido.")]
    [DataType(DataType.Date)]
    [Display(Name = "Dia de entrega requerido")]
    public DateTime? FechaEntregaRequerida { get; set; } = DateTime.Today.AddDays(3);

    public int TotalImpresiones => CantidadPaginas * CantidadCopias;

    [Required(ErrorMessage = "Debe seleccionar el tipo de color.")]
    [RegularExpression(@"^(Blanco y negro|Color)$", ErrorMessage = "Debe seleccionar una opcion de color valida.")]
    public string Color { get; set; } = "Blanco y negro";

    [Display(Name = "Doble cara")]
    public bool DobleCara { get; set; }

    [Display(Name = "Archivo a imprimir")]
    [Required(ErrorMessage = "Debe seleccionar un archivo para imprimir.")]
    public IFormFile? Archivo { get; set; }

    [Display(Name = "Detalle")]
    [StringLength(500)]
    public string? Observacion { get; set; }

    public List<SelectListItem> PersonalDisponible { get; set; } = new();
    public List<SelectListItem> Colores { get; set; } = new();
}
