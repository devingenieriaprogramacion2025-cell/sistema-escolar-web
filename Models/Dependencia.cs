using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Dependencia")]
public class Dependencia
{
    [Key]
    [Column("id_dependencia")]
    public int IdDependencia { get; set; }
    [Column("id_tipodependencia")]
    public int IdTipoDependencia { get; set; }
    [Column("nombre_dependencia")]
    public string NombreDependencia { get; set; } = string.Empty;
    [Column("responsable_dependencia")]
    public string? ResponsableDependencia { get; set; }
}
