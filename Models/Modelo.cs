using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Modelo")]
public class Modelo
{
    [Key]
    [Column("id_modelo")]
    public int IdModelo { get; set; }

    [Column("id_marca")]
    public int IdMarca { get; set; }

    [Column("nombre_modelo")]
    public string NombreModelo { get; set; } = string.Empty;
}
