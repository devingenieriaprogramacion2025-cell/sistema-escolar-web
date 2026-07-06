namespace SistemaEscolarWeb.Models;

public class Bitacora
{
    [System.ComponentModel.DataAnnotations.Key]
    public int IdBitacora { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
}
