using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Marca")]
public class Marca
{
    [Key]
    [Column("id_marca")]
    public int IdMarca { get; set; }

    [Column("nombre_marca")]
    public string NombreMarca { get; set; } = string.Empty;
}
