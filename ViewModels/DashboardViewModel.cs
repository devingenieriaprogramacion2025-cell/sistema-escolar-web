using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class DashboardViewModel
{
    public DashboardFiltrosViewModel Filtros { get; set; } = new();
    public DashboardResumenGeneralViewModel ResumenGeneral { get; set; } = new();
    public DashboardImpresionesViewModel Impresiones { get; set; } = new();
    public DashboardInventarioInsumosViewModel InventarioInsumos { get; set; } = new();
    public DashboardGestionTecnologicaViewModel GestionTecnologica { get; set; } = new();
    public DashboardAlertasViewModel Alertas { get; set; } = new();
}

public class DashboardFiltrosViewModel
{
    public int? Mes { get; set; }
    public int Anio { get; set; }
    public int? IdTipoInsumo { get; set; }
    public int? IdDependencia { get; set; }
    public string Periodo => Mes.HasValue ? $"{Mes.Value:00}/{Anio}" : Anio.ToString();
    public List<SelectListItem> Meses { get; set; } = new();
    public List<SelectListItem> Anios { get; set; } = new();
    public List<SelectListItem> TiposInsumo { get; set; } = new();
    public List<SelectListItem> Dependencias { get; set; } = new();
}

public class DashboardResumenGeneralViewModel
{
    public int TotalTecnologias { get; set; }
    public int TecnologiasDisponibles { get; set; }
    public int TecnologiasAsignadas { get; set; }
    public int TecnologiasEnReparacion { get; set; }
    public int TecnologiasDadasDeBaja { get; set; }
    public int TotalInsumos { get; set; }
    public int ImpresionesPendientes { get; set; }
    public int AlertasBajoStock { get; set; }
}

public class DashboardImpresionesViewModel
{
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Entregadas { get; set; }
    public int Rechazadas { get; set; }
    public int TotalMensualSolicitudes { get; set; }
    public int TotalMensualPaginas { get; set; }
    public int TotalMensualCopias { get; set; }
    public decimal PorcentajeResueltas { get; set; }
    public List<DashboardImpresionRecienteViewModel> UltimasSolicitudes { get; set; } = new();
}

public class DashboardImpresionRecienteViewModel
{
    public string Solicitante { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public int TotalImpresiones { get; set; }
}

public class DashboardInventarioInsumosViewModel
{
    public int TotalInsumos { get; set; }
    public int TotalBajoStock { get; set; }
    public List<DashboardStockTipoViewModel> InsumosPorTipo { get; set; } = new();
    public List<DashboardInsumoStockViewModel> MayorStock { get; set; } = new();
    public List<DashboardInsumoStockViewModel> MenorStock { get; set; } = new();
    public List<DashboardInsumoCompraViewModel> RecomendacionCompra { get; set; } = new();
}

public class DashboardStockTipoViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public int Total { get; set; }
    public int BajoStock { get; set; }
}

public class DashboardInsumoStockViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
}

public class DashboardInsumoCompraViewModel : DashboardInsumoStockViewModel
{
    public int CantidadSugerida { get; set; }
}

public class DashboardGestionTecnologicaViewModel
{
    public DashboardTecnologiaIndicadoresViewModel Tecnologias { get; set; } = new();
    public DashboardAsignacionesIndicadoresViewModel Asignaciones { get; set; } = new();
    public DashboardReparacionesIndicadoresViewModel Reparaciones { get; set; } = new();
    public DashboardBajasIndicadoresViewModel Bajas { get; set; } = new();
}

public class DashboardTecnologiaIndicadoresViewModel
{
    public int TotalRegistradas { get; set; }
    public int Disponibles { get; set; }
    public int Asignadas { get; set; }
    public int EnReparacion { get; set; }
    public int DadasDeBaja { get; set; }
}

public class DashboardAsignacionesIndicadoresViewModel
{
    public int Activas { get; set; }
    public int Devueltas { get; set; }
    public int DelMes { get; set; }
    public List<DashboardMovimientoEquipoViewModel> Ultimas { get; set; } = new();
}

public class DashboardReparacionesIndicadoresViewModel
{
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Finalizadas { get; set; }
    public decimal PromedioDias { get; set; }
    public List<DashboardReparacionAbiertaViewModel> MasAntiguas { get; set; } = new();
}

public class DashboardBajasIndicadoresViewModel
{
    public int Solicitadas { get; set; }
    public int Aprobadas { get; set; }
    public int Rechazadas { get; set; }
    public int DelMes { get; set; }
    public List<DashboardMovimientoEquipoViewModel> Ultimas { get; set; } = new();
}

public class DashboardMovimientoEquipoViewModel
{
    public string CodigoEquipo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class DashboardReparacionAbiertaViewModel
{
    public string CodigoEquipo { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public int DiasAbierta { get; set; }
}

public class DashboardAlertasViewModel
{
    public List<DashboardAlertaViewModel> Items { get; set; } = new();
}

public class DashboardAlertaViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Severidad { get; set; } = "Media";
}
