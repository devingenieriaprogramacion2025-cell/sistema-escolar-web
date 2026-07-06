using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Personal")]
public class Personal
{
    [Key]
    [Column("rut_personal")]
    public string RutPersonal { get; set; } = string.Empty;

    [Column("id_rol")]
    public int IdRol { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Column("correo")]
    public string Correo { get; set; } = string.Empty;

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("cargo")]
    public string? Cargo { get; set; }

    [Column("password_legacy")]
    public string? PasswordLegacy { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    public Rol? Rol { get; set; }
}
