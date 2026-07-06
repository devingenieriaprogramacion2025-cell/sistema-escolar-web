namespace SistemaEscolarWeb.DTOs;

public class ReparacionDto
{
    public int IdReparacion { get; set; }
    public int IdTecnologia { get; set; }
    public string CodigoEquipo { get; set; } = string.Empty;
    public string? Destino { get; set; }
    public DateTime FechaEnvio { get; set; }
    public DateTime? FechaRetorno { get; set; }
    public string? Detalle { get; set; }
    public string EstadoReparacion { get; set; } = string.Empty;
    public string? UsuarioSolicita { get; set; }
    public string? UsuarioAprueba { get; set; }
}
