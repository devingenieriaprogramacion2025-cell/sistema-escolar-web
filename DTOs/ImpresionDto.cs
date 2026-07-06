namespace SistemaEscolarWeb.DTOs;

public class ImpresionDto
{
    public int IdSolicitudImpresion { get; set; }
    public string RutPersonal { get; set; } = string.Empty;
    public string NombrePersonal { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public string? Archivo { get; set; }
    public int CantidadPaginas { get; set; }
    public int CantidadCopias { get; set; }
    public int TotalImpresiones => CantidadPaginas * CantidadCopias;
    public string Color { get; set; } = string.Empty;
    public bool DobleCara { get; set; }
    public string? Detalle { get; set; }
}
