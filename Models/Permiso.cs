using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Permisos")]
public class Permiso
{
    [Key]
    [Column("id_permiso")]
    public int IdPermiso { get; set; }

    [Column("nombre_permiso")]
    public string NombrePermiso { get; set; } = string.Empty;
}
