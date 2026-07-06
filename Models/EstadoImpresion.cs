using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Estado_impresion")]
public class EstadoImpresion
{
    [Key]
    [Column("id_estado_impresion")]
    public int IdEstadoImpresion { get; set; }
    [Column("estado_impresion")]
    public string Estado { get; set; } = string.Empty;
}
