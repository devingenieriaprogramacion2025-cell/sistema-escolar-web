namespace SistemaEscolarWeb.DTOs;

public class BajaDto
{
    public int IdDeBaja { get; set; }
    public int IdTecnologia { get; set; }
    public string CodigoEquipo { get; set; } = string.Empty;
    public DateTime FechaBaja { get; set; }
    public string? Detalle { get; set; }
    public string? UsuarioRegistraBaja { get; set; }
    public string? UsuarioAutorizaBaja { get; set; }
    public string Estado { get; set; } = string.Empty;
}
