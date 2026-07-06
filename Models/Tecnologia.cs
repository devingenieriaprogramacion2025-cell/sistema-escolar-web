using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Tecnologia")]
public class Tecnologia
{
    [Key]
    [Column("id_tecnologia")]
    public int IdTecnologia { get; set; }
    [Column("id_modelo")]
    public int IdModelo { get; set; }
    [Column("id_entradatecnologia")]
    public int? IdEntradaTecnologia { get; set; }
    [Column("id_tipotecnologia")]
    public int IdTipoTecnologia { get; set; }
    [Column("estado")]
    public bool Estado { get; set; }
    [Column("sku_codigo_inventario")]
    public string SkuCodigoInventario { get; set; } = string.Empty;
}
