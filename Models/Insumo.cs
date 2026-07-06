using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Insumo")]
public class Insumo
{
    [Key]
    [Column("id_insumo")]
    public int IdInsumo { get; set; }

    [Column("id_tipoinsumo")]
    public int IdTipoInsumo { get; set; }

    [Column("nombre_insumo")]
    public string NombreInsumo { get; set; } = string.Empty;

    [Column("descripcion_insumo")]
    public string? DescripcionInsumo { get; set; }

    [Column("unidad_medida")]
    public string UnidadMedida { get; set; } = "Unidad";

    [Column("estado")]
    public bool Estado { get; set; } = true;

    [Column("toxicidad")]
    public string? Toxicidad { get; set; }

    [Column("stock_actual")]
    public int StockActual { get; set; }

    [Column("stock_minimo")]
    public int StockMinimo { get; set; }
}
