using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Asignaciones")]
public class Asignacion
{
    [Key]
    [Column("id_asignaciones")]
    public int IdAsignaciones { get; set; }
    [Column("id_tecnologia")]
    public int IdTecnologia { get; set; }
    [Column("id_dependencia")]
    public int? IdDependencia { get; set; }
    [Column("rut_personal")]
    public string? RutPersonal { get; set; }
    [Column("fecha_asignacion")]
    public DateTime FechaAsignacion { get; set; }
    [Column("fecha_devolucion")]
    public DateTime? FechaDevolucion { get; set; }
    [Column("tipo_asignacion")]
    public string TipoAsignacion { get; set; } = string.Empty;
    [Column("estado_asignacion")]
    public string EstadoAsignacion { get; set; } = "Vigente";
}
