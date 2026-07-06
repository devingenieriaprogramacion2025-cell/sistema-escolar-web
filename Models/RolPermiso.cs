using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Rol_permisos")]
public class RolPermiso
{
    [Column("id_rol")]
    public int IdRol { get; set; }

    [Column("id_permiso")]
    public int IdPermiso { get; set; }

    [Column("fecha_rol")]
    public DateTime FechaRol { get; set; } = DateTime.Now;

    [Column("activo")]
    public bool Activo { get; set; } = true;
}
