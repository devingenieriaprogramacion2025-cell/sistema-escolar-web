using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Tipo_tecnologia")]
public class TipoTecnologia
{
    [Key]
    [Column("id_tipotecnologia")]
    public int IdTipoTecnologia { get; set; }

    [Column("nombre_tipotecnologia")]
    public string NombreTipoTecnologia { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }
}
