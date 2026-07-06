using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Usuario")]
public class Usuario
{
    [Key]
    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("rut_personal")]
    public string RutPersonal { get; set; } = string.Empty;

    [Column("id_rol")]
    public int IdRol { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("ultimo_acceso")]
    public DateTime? UltimoAcceso { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    public Personal? Personal { get; set; }
    public Rol? Rol { get; set; }
}
