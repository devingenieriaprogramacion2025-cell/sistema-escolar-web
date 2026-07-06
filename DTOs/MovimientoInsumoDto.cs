namespace SistemaEscolarWeb.DTOs;

public class EntradaInsumoDto
{
    public int IdEntradaInsumo { get; set; }
    public string Insumo { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEntrega { get; set; }
    public int Cantidad { get; set; }
}

public class SalidaInsumoDto
{
    public int IdSalidaInsumo { get; set; }
    public string Insumo { get; set; } = string.Empty;
    public string Dependencia { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaSalida { get; set; }
}
