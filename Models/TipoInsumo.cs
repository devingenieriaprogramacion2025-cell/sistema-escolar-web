using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Tipo_insumo")]
public class TipoInsumo
{
    [Key]
    [Column("id_tipoinsumo")]
    public int IdTipoInsumo { get; set; }

    [Column("nombre_tipoinsumo")]
    public string NombreTipoInsumo { get; set; } = string.Empty;
}
