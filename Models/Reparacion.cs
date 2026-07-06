using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Reparacion")]
public class Reparacion
{
    [Key]
    [Column("id_reparacion")]
    public int IdReparacion { get; set; }
    [Column("id_tecnologia")]
    public int IdTecnologia { get; set; }
    [Column("destino")]
    public string? Destino { get; set; }
    [Column("fecha_envio")]
    public DateTime FechaEnvio { get; set; }
    [Column("fecha_retorno")]
    public DateTime? FechaRetorno { get; set; }
    [Column("detalle")]
    public string? Detalle { get; set; }
    [Column("estado_reparacion")]
    public string EstadoReparacion { get; set; } = "Solicitada";
    [Column("usuario_solicita")]
    public string? UsuarioSolicita { get; set; }
    [Column("usuario_aprueba")]
    public string? UsuarioAprueba { get; set; }
}
