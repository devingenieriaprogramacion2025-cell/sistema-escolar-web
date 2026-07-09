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
    public int InsumosBajoStockTotal { get; set; }
    public int TotalDependencias { get; set; }
    public List<ChartItemDto> EquiposPorEstado { get; set; } = new();
    public List<ChartItemDto> ImpresionesPorEstado { get; set; } = new();
    public List<ChartItemDto> PersonalPorRol { get; set; } = new();
    public List<ChartItemDto> UsuariosConAccesoPorRol { get; set; } = new();
    public List<MovimientoInsumoPeriodoDto> InsumosDiarios { get; set; } = new();
    public List<MovimientoInsumoPeriodoDto> InsumosSemanales { get; set; } = new();
    public List<MovimientoInsumoPeriodoDto> InsumosMensuales { get; set; } = new();
    public List<MovimientoInsumoPeriodoDto> InsumosAnuales { get; set; } = new();
    public List<MovimientoTecnologiaPeriodoDto> TecnologiaDiaria { get; set; } = new();
    public List<MovimientoTecnologiaPeriodoDto> TecnologiaSemanal { get; set; } = new();
    public List<MovimientoTecnologiaPeriodoDto> TecnologiaMensual { get; set; } = new();
    public List<MovimientoTecnologiaPeriodoDto> TecnologiaAnual { get; set; } = new();
    public List<MovimientoImpresionPeriodoDto> ImpresionesDiarias { get; set; } = new();
    public List<MovimientoImpresionPeriodoDto> ImpresionesSemanales { get; set; } = new();
    public List<MovimientoImpresionPeriodoDto> ImpresionesMensuales { get; set; } = new();
    public List<MovimientoImpresionPeriodoDto> ImpresionesAnuales { get; set; } = new();
}

public class ChartItemDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class MovimientoInsumoPeriodoDto
{
    public string Label { get; set; } = string.Empty;
    public int Entradas { get; set; }
    public int Salidas { get; set; }
}

public class MovimientoTecnologiaPeriodoDto
{
    public string Label { get; set; } = string.Empty;
    public int Entradas { get; set; }
    public int Asignaciones { get; set; }
    public int Reparaciones { get; set; }
    public int Bajas { get; set; }
}

public class MovimientoImpresionPeriodoDto
{
    public string Label { get; set; } = string.Empty;
    public int Solicitudes { get; set; }
    public int TotalImpresiones { get; set; }
}
