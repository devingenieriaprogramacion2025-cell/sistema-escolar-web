using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Insumo")]
public class Insumo
{
    [Key]
    [Column("id_insumo")]
    public int IdInsumo { get; set; }

    [Display(Name = "Tipo de insumo")]
    [Required(ErrorMessage = "El tipo de insumo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El tipo de insumo es obligatorio.")]
    [Column("id_tipoinsumo")]
    public int IdTipoInsumo { get; set; }

    [Display(Name = "Nombre del insumo")]
    [Required(ErrorMessage = "El nombre del insumo es obligatorio.")]
    [Column("nombre_insumo")]
    public string NombreInsumo { get; set; } = string.Empty;

    [Display(Name = "Descripcion")]
    [Column("descripcion_insumo")]
    public string? DescripcionInsumo { get; set; }

    [Display(Name = "Unidad de medida")]
    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [Column("unidad_medida")]
    public string UnidadMedida { get; set; } = "Unidad";

    [Display(Name = "Estado")]
    [Column("estado")]
    public bool Estado { get; set; } = true;

    [Display(Name = "Toxicidad")]
    [Column("toxicidad")]
    public string? Toxicidad { get; set; }

    [Display(Name = "Stock actual")]
    [Column("stock_actual")]
    public int StockActual { get; set; }

    [Display(Name = "Stock minimo")]
    [Column("stock_minimo")]
    public int StockMinimo { get; set; }
}
