namespace SistemaEscolarWeb.DTOs;

public class AsignacionDto
{
    public int IdAsignaciones { get; set; }
    public int IdTecnologia { get; set; }
    public string CodigoEquipo { get; set; } = string.Empty;
    public string MarcaEquipo { get; set; } = string.Empty;
    public string ModeloEquipo { get; set; } = string.Empty;
    public string TipoTecnologia { get; set; } = string.Empty;
    public string? RutPersonal { get; set; }
    public string NombrePersonal { get; set; } = string.Empty;
    public int? IdDependencia { get; set; }
    public string Dependencia { get; set; } = string.Empty;
    public string TipoDestinatario { get; set; } = string.Empty;
    public string AsignadoA { get; set; } = string.Empty;
    public DateTime FechaAsignacion { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string TipoAsignacion { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public string EstadoAsignacion { get; set; } = string.Empty;
    public bool EstaActiva => FechaDevolucion == null && (EstadoAsignacion == "Activa" || EstadoAsignacion == "Vigente");
}
