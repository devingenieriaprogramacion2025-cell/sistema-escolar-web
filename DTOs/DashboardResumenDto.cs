namespace SistemaEscolarWeb.DTOs;

public class DashboardResumenDto
{
    public int UsuariosActivos { get; set; }
    public int TotalPersonal { get; set; }
    public int TotalEquipos { get; set; }
    public int EquiposAsignados { get; set; }
    public int EquiposDisponibles { get; set; }
    public int EquiposEnReparacion { get; set; }
    public int EquiposDadosDeBaja { get; set; }
    public int ReparacionesPendientes { get; set; }
    public int BajasPendientes { get; set; }
    public int ImpresionesPendientes { get; set; }
    public int ImpresionesEnProceso { get; set; }
    public int ImpresionesEntregadas { get; set; }
    public int ImpresionesRechazadas { get; set; }
    public List<ChartItemDto> EquiposPorEstado { get; set; } = new();
    public List<ChartItemDto> ImpresionesPorEstado { get; set; } = new();
    public List<ChartItemDto> PersonalPorRol { get; set; } = new();
    public List<ChartItemDto> MovimientosMensuales { get; set; } = new();
}

public class ChartItemDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}
