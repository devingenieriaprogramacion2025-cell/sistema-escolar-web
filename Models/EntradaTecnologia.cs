using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Entrada_tecnologia")]
public class EntradaTecnologia
{
    [Key]
    [Column("id_entradatecnologia")]
    public int IdEntradaTecnologia { get; set; }

    [Column("id_proveedor")]
    public int IdProveedor { get; set; }

    [Column("fecha_entrada")]
    public DateTime FechaEntrada { get; set; } = DateTime.Today;

    [Column("cantidad")]
    public int Cantidad { get; set; } = 1;

    [Column("numero_factura")]
    public string NumeroFactura { get; set; } = string.Empty;
}
