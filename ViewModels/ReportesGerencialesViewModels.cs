using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public record ReporteConteo(string Label, int Value);

public class ReporteHomeItem
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
}

public class ReporteEjecutivoViewModel
{
    public int EquiposDisponibles { get; set; }
    public int EquiposAsignados { get; set; }
    public int EquiposEnReparacion { get; set; }
    public int EquiposDadosDeBaja { get; set; }
    public int InsumosRegistrados { get; set; }
    public int EntradasMes { get; set; }
    public int SalidasMes { get; set; }
    public int ImpresionesPendientes { get; set; }
    public int ImpresionesEnProceso { get; set; }
    public int ImpresionesEntregadas { get; set; }
    public int ImpresionesRechazadas { get; set; }
    public List<ReporteConteo> TecnologiasPorEstado { get; set; } = [];
    public List<ReporteConteo> ImpresionesPorEstado { get; set; } = [];
}

public class ReporteInventarioTecnologicoFiltro
{
    public string? TipoTecnologia { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Estado { get; set; }
    public string? Dependencia { get; set; }
}

public class ReporteInventarioTecnologicoFila
{
    public string Sku { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Dependencia { get; set; } = string.Empty;
    public string FuncionarioAsignado { get; set; } = string.Empty;
    public DateTime? FechaIngreso { get; set; }
}

public class ReporteInventarioTecnologicoViewModel
{
    public ReporteInventarioTecnologicoFiltro Filtro { get; set; } = new();
    public List<ReporteInventarioTecnologicoFila> Filas { get; set; } = [];
    public List<SelectListItem> Tipos { get; set; } = [];
    public List<SelectListItem> Marcas { get; set; } = [];
    public List<SelectListItem> Modelos { get; set; } = [];
    public List<SelectListItem> Estados { get; set; } = [];
    public List<SelectListItem> Dependencias { get; set; } = [];
}

public class ReporteMovimientosInsumosFiltro
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? TipoInsumo { get; set; }
    public string? Proveedor { get; set; }
    public string? Dependencia { get; set; }
}

public class ReporteMovimientoInsumoFila
{
    public DateTime Fecha { get; set; }
    public string Movimiento { get; set; } = string.Empty;
    public string Insumo { get; set; } = string.Empty;
    public string TipoInsumo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Funcionario { get; set; } = string.Empty;
    public string Dependencia { get; set; } = string.Empty;
    public int StockActual { get; set; }
}

public class ReporteMovimientosInsumosViewModel
{
    public ReporteMovimientosInsumosFiltro Filtro { get; set; } = new();
    public List<ReporteMovimientoInsumoFila> Filas { get; set; } = [];
    public int TotalEntradas { get; set; }
    public int TotalSalidas { get; set; }
    public int StockActual { get; set; }
    public List<SelectListItem> TiposInsumo { get; set; } = [];
    public List<SelectListItem> Proveedores { get; set; } = [];
    public List<SelectListItem> Dependencias { get; set; } = [];
}

public class ReporteAsignacionesFiltro
{
    public string? Funcionario { get; set; }
    public string? Dependencia { get; set; }
    public string? TipoTecnologia { get; set; }
    public string? Estado { get; set; }
}

public class ReporteAsignacionFila
{
    public int IdTecnologia { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string TipoTecnologia { get; set; } = string.Empty;
    public string Funcionario { get; set; } = string.Empty;
    public string Dependencia { get; set; } = string.Empty;
    public DateTime FechaAsignacion { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EstaActiva { get; set; }
}

public class ReporteAsignacionesViewModel
{
    public ReporteAsignacionesFiltro Filtro { get; set; } = new();
    public List<ReporteAsignacionFila> Filas { get; set; } = [];
    public List<SelectListItem> Funcionarios { get; set; } = [];
    public List<SelectListItem> Dependencias { get; set; } = [];
    public List<SelectListItem> TiposTecnologia { get; set; } = [];
    public List<SelectListItem> Estados { get; set; } = [];
}

public class ReporteReparacionesFiltro
{
    public string? Estado { get; set; }
    public DateTime? Fecha { get; set; }
}

public class ReporteReparacionFila
{
    public string Sku { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public DateTime? FechaRetorno { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public class ReporteReparacionesViewModel
{
    public ReporteReparacionesFiltro Filtro { get; set; } = new();
    public List<ReporteReparacionFila> Filas { get; set; } = [];
    public int Pendientes { get; set; }
    public int Finalizadas { get; set; }
    public double TiempoPromedioReparacion { get; set; }
    public List<SelectListItem> Estados { get; set; } = [];
}

public class ReporteBajasFiltro
{
    public DateTime? Fecha { get; set; }
    public string? Motivo { get; set; }
    public string? TipoTecnologia { get; set; }
}

public class ReporteBajaFila
{
    public string Sku { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string TipoTecnologia { get; set; } = string.Empty;
    public DateTime FechaBaja { get; set; }
    public string UsuarioRegistra { get; set; } = string.Empty;
    public string UsuarioAutoriza { get; set; } = string.Empty;
}

public class ReporteBajasViewModel
{
    public ReporteBajasFiltro Filtro { get; set; } = new();
    public List<ReporteBajaFila> Filas { get; set; } = [];
    public int CantidadBajas { get; set; }
    public List<ReporteConteo> BajasPorTipo { get; set; } = [];
    public List<ReporteConteo> BajasPorMotivo { get; set; } = [];
    public List<SelectListItem> Motivos { get; set; } = [];
    public List<SelectListItem> TiposTecnologia { get; set; } = [];
}

public class ReporteImpresionesFiltro
{
    public DateTime? Fecha { get; set; }
    public string? Funcionario { get; set; }
    public string? Estado { get; set; }
    public string? Dependencia { get; set; }
}

public class ReporteImpresionFila
{
    public string Solicitante { get; set; } = string.Empty;
    public string RutPersonal { get; set; } = string.Empty;
    public string Dependencia { get; set; } = string.Empty;
    public int CantidadPaginas { get; set; }
    public int CantidadCopias { get; set; }
    public int TotalImpresiones => CantidadPaginas * CantidadCopias;
    public string Color { get; set; } = string.Empty;
    public bool DobleCara { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaEntrega { get; set; }
}

public class ReporteImpresionesViewModel
{
    public ReporteImpresionesFiltro Filtro { get; set; } = new();
    public List<ReporteImpresionFila> Filas { get; set; } = [];
    public int TotalSolicitudes { get; set; }
    public int TotalPaginas { get; set; }
    public int TotalCopias { get; set; }
    public int TotalImpresiones { get; set; }
    public int TotalColor { get; set; }
    public int TotalBlancoNegro { get; set; }
    public List<SelectListItem> Funcionarios { get; set; } = [];
    public List<SelectListItem> Estados { get; set; } = [];
    public List<SelectListItem> Dependencias { get; set; } = [];
}

public class ReportePersonalFiltro
{
    public string? Cargo { get; set; }
    public string? Estado { get; set; }
}

public class ReportePersonalFila
{
    public string Funcionario { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? UltimoAcceso { get; set; }
}

public class ReportePersonalViewModel
{
    public ReportePersonalFiltro Filtro { get; set; } = new();
    public List<ReportePersonalFila> Filas { get; set; } = [];
    public List<SelectListItem> Cargos { get; set; } = [];
    public List<SelectListItem> Estados { get; set; } = [];
}
