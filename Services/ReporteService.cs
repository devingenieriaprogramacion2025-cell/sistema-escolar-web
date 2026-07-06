using SistemaEscolarWeb.Repositories;
using SistemaEscolarWeb.Reports;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class ReporteService
{
    private readonly ReporteRepository _repository;

    public ReporteService(ReporteRepository repository)
    {
        _repository = repository;
    }

    public List<ReporteHomeItem> ObtenerMenuReportes()
        =>
        [
            new() { Titulo = "Reporte Ejecutivo", Descripcion = "Resumen institucional con indicadores y graficos.", Icono = "bi-graph-up-arrow", Accion = "Ejecutivo" },
            new() { Titulo = "Inventario Tecnologico", Descripcion = "Detalle de equipos, estados, responsables y dependencias.", Icono = "bi-pc-display", Accion = "InventarioTecnologico" },
            new() { Titulo = "Movimientos de Insumos", Descripcion = "Entradas y salidas consolidadas con stock actual.", Icono = "bi-box-seam", Accion = "MovimientosInsumos" },
            new() { Titulo = "Asignaciones", Descripcion = "Consulta de equipos asignados y devueltos.", Icono = "bi-clipboard-check", Accion = "Asignaciones" },
            new() { Titulo = "Reparaciones", Descripcion = "Seguimiento de envios, retornos y tiempos de reparacion.", Icono = "bi-tools", Accion = "Reparaciones" },
            new() { Titulo = "Bajas", Descripcion = "Analisis de bajas por motivo y tipo de tecnologia.", Icono = "bi-trash3", Accion = "Bajas" },
            new() { Titulo = "Impresiones", Descripcion = "Solicitudes, copias, estados y tipos de impresion.", Icono = "bi-printer", Accion = "Impresiones" },
            new() { Titulo = "Personal", Descripcion = "Estado del personal y accesos de usuario.", Icono = "bi-people", Accion = "Personal" }
        ];

    public Task<ReporteEjecutivoViewModel> ObtenerEjecutivoAsync() => _repository.ObtenerEjecutivoAsync();
    public Task<ReporteInventarioTecnologicoViewModel> ObtenerInventarioTecnologicoAsync(ReporteInventarioTecnologicoFiltro filtro) => _repository.ObtenerInventarioTecnologicoAsync(filtro);
    public Task<ReporteMovimientosInsumosViewModel> ObtenerMovimientosInsumosAsync(ReporteMovimientosInsumosFiltro filtro) => _repository.ObtenerMovimientosInsumosAsync(filtro);
    public Task<ReporteAsignacionesViewModel> ObtenerAsignacionesAsync(ReporteAsignacionesFiltro filtro) => _repository.ObtenerAsignacionesAsync(filtro);
    public Task<ReporteReparacionesViewModel> ObtenerReparacionesAsync(ReporteReparacionesFiltro filtro) => _repository.ObtenerReparacionesAsync(filtro);
    public Task<ReporteBajasViewModel> ObtenerBajasAsync(ReporteBajasFiltro filtro) => _repository.ObtenerBajasAsync(filtro);
    public Task<ReporteImpresionesViewModel> ObtenerImpresionesAsync(ReporteImpresionesFiltro filtro) => _repository.ObtenerImpresionesAsync(filtro);
    public Task<ReportePersonalViewModel> ObtenerPersonalAsync(ReportePersonalFiltro filtro) => _repository.ObtenerPersonalAsync(filtro);

    public async Task<ReporteExportData?> ObtenerExportacionAsync(string reporte, IReadOnlyDictionary<string, string?> parametros)
    {
        return NormalizarReporte(reporte) switch
        {
            "ejecutivo" => await ExportarEjecutivoAsync(),
            "inventariotecnologico" => await ExportarInventarioTecnologicoAsync(parametros),
            "movimientosinsumos" => await ExportarMovimientosInsumosAsync(parametros),
            "asignaciones" => await ExportarAsignacionesAsync(parametros),
            "reparaciones" => await ExportarReparacionesAsync(parametros),
            "bajas" => await ExportarBajasAsync(parametros),
            "impresiones" => await ExportarImpresionesAsync(parametros),
            "personal" => await ExportarPersonalAsync(parametros),
            _ => null
        };
    }

    private async Task<ReporteExportData> ExportarEjecutivoAsync()
    {
        var model = await ObtenerEjecutivoAsync();
        return new ReporteExportData
        {
            Titulo = "Reporte Ejecutivo",
            Descripcion = "Resumen institucional con indicadores principales.",
            Resumen =
            [
                new("Equipos disponibles", model.EquiposDisponibles.ToString()),
                new("Equipos asignados", model.EquiposAsignados.ToString()),
                new("Equipos en reparacion", model.EquiposEnReparacion.ToString()),
                new("Equipos dados de baja", model.EquiposDadosDeBaja.ToString()),
                new("Insumos registrados", model.InsumosRegistrados.ToString()),
                new("Entradas del mes", model.EntradasMes.ToString()),
                new("Salidas del mes", model.SalidasMes.ToString())
            ],
            Columnas = ["Indicador", "Valor"],
            Filas =
            [
                .. model.TecnologiasPorEstado.Select(i => new List<string> { $"Tecnologia - {i.Label}", i.Value.ToString() }),
                .. model.ImpresionesPorEstado.Select(i => new List<string> { $"Impresiones - {i.Label}", i.Value.ToString() })
            ]
        };
    }

    private async Task<ReporteExportData> ExportarInventarioTecnologicoAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReporteInventarioTecnologicoFiltro
        {
            TipoTecnologia = Valor(parametros, "TipoTecnologia"),
            Marca = Valor(parametros, "Marca"),
            Modelo = Valor(parametros, "Modelo"),
            Estado = Valor(parametros, "Estado"),
            Dependencia = Valor(parametros, "Dependencia")
        };
        var model = await ObtenerInventarioTecnologicoAsync(filtro);
        var filas = model.Filas.OrderBy(f => f.Tipo).ThenBy(f => f.Marca).ThenBy(f => f.Modelo).ThenBy(f => f.Sku).ToList();

        return new ReporteExportData
        {
            Titulo = "Inventario Tecnologico",
            Descripcion = "Equipos por estado, dependencia y responsable.",
            Filtros = Filtros(("Tipo tecnologia", filtro.TipoTecnologia), ("Marca", filtro.Marca), ("Modelo", filtro.Modelo), ("Estado", filtro.Estado), ("Dependencia", filtro.Dependencia)),
            Resumen =
            [
                new("Total equipos", filas.Count.ToString()),
                new("Disponibles", filas.Count(f => f.Estado == "Disponible").ToString()),
                new("Asignados", filas.Count(f => f.Estado == "Asignado").ToString()),
                new("En reparacion", filas.Count(f => f.Estado == "En Reparacion").ToString()),
                new("Dados de baja", filas.Count(f => f.Estado == "Dado de Baja").ToString())
            ],
            Columnas = ["SKU", "Tipo", "Marca", "Modelo", "Estado", "Dependencia", "Funcionario asignado", "Fecha ingreso"],
            Filas = filas.Select(f => new List<string> { f.Sku, f.Tipo, f.Marca, f.Modelo, f.Estado, f.Dependencia, f.FuncionarioAsignado, Fecha(f.FechaIngreso) }).ToList()
        };
    }

    private async Task<ReporteExportData> ExportarMovimientosInsumosAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReporteMovimientosInsumosFiltro
        {
            FechaDesde = FechaParametro(parametros, "FechaDesde"),
            FechaHasta = FechaParametro(parametros, "FechaHasta"),
            TipoInsumo = Valor(parametros, "TipoInsumo"),
            Proveedor = Valor(parametros, "Proveedor"),
            Dependencia = Valor(parametros, "Dependencia")
        };
        var model = await ObtenerMovimientosInsumosAsync(filtro);
        var filas = model.Filas.OrderByDescending(f => f.Fecha).ThenBy(f => f.Insumo).ToList();

        return new ReporteExportData
        {
            Titulo = "Movimientos de Insumos",
            Descripcion = "Entradas y salidas consolidadas con stock actual.",
            Filtros = Filtros(("Fecha desde", Fecha(filtro.FechaDesde)), ("Fecha hasta", Fecha(filtro.FechaHasta)), ("Tipo insumo", filtro.TipoInsumo), ("Proveedor", filtro.Proveedor), ("Dependencia", filtro.Dependencia)),
            Resumen =
            [
                new("Total entradas", model.TotalEntradas.ToString()),
                new("Total salidas", model.TotalSalidas.ToString()),
                new("Stock actual", model.StockActual.ToString())
            ],
            Columnas = ["Fecha", "Movimiento", "Insumo", "Tipo insumo", "Cantidad", "Proveedor", "Funcionario", "Dependencia", "Stock actual"],
            Filas = filas.Select(f => new List<string> { Fecha(f.Fecha), f.Movimiento, f.Insumo, f.TipoInsumo, f.Cantidad.ToString(), f.Proveedor, f.Funcionario, f.Dependencia, f.StockActual.ToString() }).ToList()
        };
    }

    private async Task<ReporteExportData> ExportarAsignacionesAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReporteAsignacionesFiltro
        {
            Funcionario = Valor(parametros, "Funcionario"),
            Dependencia = Valor(parametros, "Dependencia"),
            TipoTecnologia = Valor(parametros, "TipoTecnologia"),
            Estado = Valor(parametros, "Estado")
        };
        var model = await ObtenerAsignacionesAsync(filtro);
        var filas = model.Filas.OrderByDescending(f => f.FechaAsignacion).ThenBy(f => f.Funcionario).ThenBy(f => f.Sku).ToList();

        return new ReporteExportData
        {
            Titulo = "Asignaciones",
            Descripcion = "Consulta de equipos asignados y devueltos.",
            Filtros = Filtros(("Funcionario", filtro.Funcionario), ("Dependencia", filtro.Dependencia), ("Tipo tecnologia", filtro.TipoTecnologia), ("Estado", filtro.Estado)),
            Resumen =
            [
                new("Total asignaciones", filas.Count.ToString()),
                new("Activas", filas.Count(f => f.EstaActiva).ToString()),
                new("Finalizadas", filas.Count(f => f.Estado == "Finalizada").ToString())
            ],
            Columnas = ["SKU", "Equipo", "Tipo tecnologia", "Funcionario", "Dependencia", "Fecha asignacion", "Fecha devolucion", "Estado"],
            Filas = filas.Select(f => new List<string> { f.Sku, f.Equipo, f.TipoTecnologia, f.Funcionario, f.Dependencia, Fecha(f.FechaAsignacion), Fecha(f.FechaDevolucion), f.Estado }).ToList()
        };
    }

    private async Task<ReporteExportData> ExportarReparacionesAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReporteReparacionesFiltro
        {
            Estado = Valor(parametros, "Estado"),
            Fecha = FechaParametro(parametros, "Fecha")
        };
        var model = await ObtenerReparacionesAsync(filtro);
        var filas = model.Filas.OrderByDescending(f => f.FechaEnvio).ThenBy(f => f.Sku).ToList();

        return new ReporteExportData
        {
            Titulo = "Reparaciones",
            Descripcion = "Seguimiento de envios, retornos y tiempos de reparacion.",
            Filtros = Filtros(("Estado", filtro.Estado), ("Fecha", Fecha(filtro.Fecha))),
            Resumen =
            [
                new("Pendientes", model.Pendientes.ToString()),
                new("Finalizadas", model.Finalizadas.ToString()),
                new("Promedio dias", model.TiempoPromedioReparacion.ToString("0.0"))
            ],
            Columnas = ["SKU", "Equipo", "Destino reparacion", "Fecha envio", "Fecha retorno", "Estado", "Observaciones"],
            Filas = filas.Select(f => new List<string> { f.Sku, f.Equipo, f.Destino, Fecha(f.FechaEnvio), Fecha(f.FechaRetorno), f.Estado, f.Observaciones }).ToList()
        };
    }

    private async Task<ReporteExportData> ExportarBajasAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReporteBajasFiltro
        {
            Fecha = FechaParametro(parametros, "Fecha"),
            Motivo = Valor(parametros, "Motivo"),
            TipoTecnologia = Valor(parametros, "TipoTecnologia")
        };
        var model = await ObtenerBajasAsync(filtro);
        var filas = model.Filas.OrderByDescending(f => f.FechaBaja).ThenBy(f => f.Sku).ToList();

        return new ReporteExportData
        {
            Titulo = "Bajas",
            Descripcion = "Analisis de bajas por fecha, motivo y tipo de tecnologia.",
            Filtros = Filtros(("Fecha", Fecha(filtro.Fecha)), ("Motivo", filtro.Motivo), ("Tipo tecnologia", filtro.TipoTecnologia)),
            Resumen =
            [
                new("Cantidad bajas", model.CantidadBajas.ToString()),
                .. model.BajasPorTipo.OrderBy(i => i.Label).Select(i => new ReporteExportItem($"Tipo: {i.Label}", i.Value.ToString())),
                .. model.BajasPorMotivo.OrderBy(i => i.Label).Select(i => new ReporteExportItem($"Motivo: {i.Label}", i.Value.ToString()))
            ],
            Columnas = ["SKU", "Equipo", "Tipo tecnologia", "Motivo", "Fecha baja", "Usuario registra", "Usuario autoriza"],
            Filas = filas.Select(f => new List<string> { f.Sku, f.Equipo, f.TipoTecnologia, f.Motivo, Fecha(f.FechaBaja), f.UsuarioRegistra, f.UsuarioAutoriza }).ToList()
        };
    }

    private async Task<ReporteExportData> ExportarImpresionesAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReporteImpresionesFiltro
        {
            Fecha = FechaParametro(parametros, "Fecha"),
            Funcionario = Valor(parametros, "Funcionario"),
            Estado = Valor(parametros, "Estado"),
            Dependencia = Valor(parametros, "Dependencia")
        };
        var model = await ObtenerImpresionesAsync(filtro);
        var filas = model.Filas.OrderByDescending(f => f.FechaSolicitud).ThenBy(f => f.Solicitante).ToList();

        return new ReporteExportData
        {
            Titulo = "Impresiones",
            Descripcion = "Solicitudes, copias, estados y tipos de impresion.",
            Filtros = Filtros(("Fecha", Fecha(filtro.Fecha)), ("Funcionario", filtro.Funcionario), ("Estado", filtro.Estado), ("Dependencia", filtro.Dependencia)),
            Resumen =
            [
                new("Solicitudes", model.TotalSolicitudes.ToString()),
                new("Total paginas", model.TotalPaginas.ToString()),
                new("Total copias", model.TotalCopias.ToString()),
                new("Total impresiones", model.TotalImpresiones.ToString()),
                new("Color", model.TotalColor.ToString()),
                new("Blanco y negro", model.TotalBlancoNegro.ToString())
            ],
            Columnas = ["Solicitante", "RUT", "Dependencia", "Paginas", "Copias", "Total", "Color", "Doble cara", "Estado", "Fecha solicitud", "Fecha entrega"],
            Filas = filas.Select(f => new List<string> { f.Solicitante, f.RutPersonal, f.Dependencia, f.CantidadPaginas.ToString(), f.CantidadCopias.ToString(), f.TotalImpresiones.ToString(), f.Color, f.DobleCara ? "Si" : "No", f.Estado, Fecha(f.FechaSolicitud), Fecha(f.FechaEntrega) }).ToList()
        };
    }

    private async Task<ReporteExportData> ExportarPersonalAsync(IReadOnlyDictionary<string, string?> parametros)
    {
        var filtro = new ReportePersonalFiltro
        {
            Cargo = Valor(parametros, "Cargo"),
            Estado = Valor(parametros, "Estado")
        };
        var model = await ObtenerPersonalAsync(filtro);
        var filas = model.Filas.OrderBy(f => f.Funcionario).ToList();

        return new ReporteExportData
        {
            Titulo = "Personal",
            Descripcion = "Estado del personal y accesos de usuario.",
            Filtros = Filtros(("Cargo", filtro.Cargo), ("Estado", filtro.Estado)),
            Resumen =
            [
                new("Total funcionarios", filas.Count.ToString()),
                new("Activos", filas.Count(f => f.Estado == "Activo").ToString()),
                new("Inactivos", filas.Count(f => f.Estado == "Inactivo").ToString()),
                new("Con acceso", filas.Count(f => f.Usuario == "Con acceso").ToString())
            ],
            Columnas = ["Funcionario", "Cargo", "Correo", "Usuario", "Estado", "Ultimo acceso"],
            Filas = filas.Select(f => new List<string> { f.Funcionario, f.Cargo, f.Correo, f.Usuario, f.Estado, FechaHora(f.UltimoAcceso) }).ToList()
        };
    }

    private static string NormalizarReporte(string reporte)
        => reporte.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    private static string? Valor(IReadOnlyDictionary<string, string?> parametros, string key)
        => parametros.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static DateTime? FechaParametro(IReadOnlyDictionary<string, string?> parametros, string key)
        => DateTime.TryParse(Valor(parametros, key), out var fecha) ? fecha : null;

    private static string Fecha(DateTime? fecha)
        => fecha.HasValue ? fecha.Value.ToString("dd-MM-yyyy") : "-";

    private static string FechaHora(DateTime? fecha)
        => fecha.HasValue ? fecha.Value.ToString("dd-MM-yyyy HH:mm") : "-";

    private static List<ReporteExportItem> Filtros(params (string Nombre, string? Valor)[] filtros)
        => filtros
            .Where(f => !string.IsNullOrWhiteSpace(f.Valor) && f.Valor != "-")
            .Select(f => new ReporteExportItem(f.Nombre, f.Valor!))
            .ToList();
}
