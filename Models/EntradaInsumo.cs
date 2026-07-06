using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Entrada_insumo")]
public class EntradaInsumo
{
    [Key]
    [Column("id_entradainsumo")]
    public int IdEntradaInsumo { get; set; }

    [Column("id_insumo")]
    public int IdInsumo { get; set; }

    [Column("id_proveedor")]
    public int IdProveedor { get; set; }

    [Column("numero_factura")]
    public string NumeroFactura { get; set; } = string.Empty;

    [Column("fecha_entrega")]
    public DateTime FechaEntrega { get; set; } = DateTime.Today;

    [Column("cantidad")]
    public int Cantidad { get; set; }
}
