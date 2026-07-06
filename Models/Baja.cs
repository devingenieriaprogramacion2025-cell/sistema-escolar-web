using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("De_baja")]
public class Baja
{
    [Key]
    [Column("id_debaja")]
    public int IdDeBaja { get; set; }
    [Column("id_motivo")]
    public int IdMotivo { get; set; }
    [Column("id_tecnologia")]
    public int IdTecnologia { get; set; }
    [Column("fecha_baja")]
    public DateTime FechaBaja { get; set; }
    [Column("detalle")]
    public string? Detalle { get; set; }
    [Column("usuario_registra_baja")]
    public string? UsuarioRegistraBaja { get; set; }
    [Column("usuario_autoriza_baja")]
    public string? UsuarioAutorizaBaja { get; set; }
    [Column("estado")]
    public string Estado { get; set; } = "Pendiente";
}
