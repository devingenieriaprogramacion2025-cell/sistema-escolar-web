using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Proveedor")]
public class Proveedor
{
    [Key]
    [Column("id_proveedor")]
    public int IdProveedor { get; set; }

    [Column("nombre_proveedor")]
    public string NombreProveedor { get; set; } = string.Empty;

    [Column("rut_proveedor")]
    public string RutProveedor { get; set; } = string.Empty;

    [Column("correo")]
    public string? Correo { get; set; }

    [Column("telefono")]
    public string? Telefono { get; set; }
}
