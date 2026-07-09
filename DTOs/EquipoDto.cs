namespace SistemaEscolarWeb.DTOs;

public class EquipoDto
{
    public int IdTecnologia { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;
    public int IdModelo { get; set; }
    public int IdTipoTecnologia { get; set; }
    public int? IdEntradaTecnologia { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string TipoTecnologia { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public DateTime? FechaEntrada { get; set; }
    public int? Cantidad { get; set; }
    public string? NumeroFactura { get; set; }
    public bool Activo { get; set; }
    public string EstadoOperativo { get; set; } = "Disponible";
    public DateTime? UltimaFechaMovimiento { get; set; }
    public string? UltimoComentario { get; set; }
    public bool PuedeEditar => EstadoOperativo != "Dado de Baja";
    public bool PuedeAsignar => EstadoOperativo == "Disponible";
}

public class EntradaTecnologiaDto
{
    public int IdEntradaTecnologia { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string TipoTecnologia { get; set; } = string.Empty;
    public string SkuGenerados { get; set; } = string.Empty;
    public DateTime FechaEntrada { get; set; }
    public int Cantidad { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
}
