using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Salida_insumo")]
public class SalidaInsumo
{
    [Key]
    [Column("id_salidainsumo")]
    public int IdSalidaInsumo { get; set; }

    [Column("id_insumo")]
    public int IdInsumo { get; set; }

    [Column("id_dependencia")]
    public int IdDependencia { get; set; }

    [Column("rut_personal")]
    public string RutPersonal { get; set; } = string.Empty;

    [Column("fecha_salida")]
    public DateTime FechaSalida { get; set; } = DateTime.Today;

    [Column("cantidad")]
    public int Cantidad { get; set; }
}
