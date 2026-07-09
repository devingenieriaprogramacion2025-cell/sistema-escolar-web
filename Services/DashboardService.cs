using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> ObtenerDashboardAsync(
        string rol,
        string? rutPersonal,
        int? mes,
        int? anio,
        int? idTipoInsumo,
        int? idDependencia)
    {
        var hoy = DateTime.Today;
        var mesFiltro = mes is >= 1 and <= 12 ? mes.Value : hoy.Month;
        var anioFiltro = anio is >= 2000 and <= 2100 ? anio.Value : hoy.Year;
        var desde = new DateTime(anioFiltro, mesFiltro, 1);
        var hasta = desde.AddMonths(1);
        var esProfesor = rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal);

        var model = new DashboardViewModel
        {
            Filtros = await CrearFiltrosAsync(mesFiltro, anioFiltro, idTipoInsumo, idDependencia)
        };

        model.GestionTecnologica.Tecnologias = await ObtenerTecnologiasAsync();
        model.ResumenGeneral = await ObtenerResumenGeneralAsync(model.GestionTecnologica.Tecnologias);
        model.Impresiones = await ObtenerImpresionesAsync(desde, hasta, esProfesor ? rutPersonal : null);
        model.InventarioInsumos = await ObtenerInventarioInsumosAsync(idTipoInsumo);
        model.GestionTecnologica.Asignaciones = await ObtenerAsignacionesAsync(desde, hasta, esProfesor ? rutPersonal : null, idDependencia);
        model.GestionTecnologica.Reparaciones = await ObtenerReparacionesAsync(desde, hasta);
        model.GestionTecnologica.Bajas = await ObtenerBajasAsync(desde, hasta);
        model.Alertas = await ObtenerAlertasAsync(esProfesor ? rutPersonal : null);

        model.ResumenGeneral.ImpresionesPendientes = model.Impresiones.Pendientes;
        model.ResumenGeneral.TotalInsumos = model.InventarioInsumos.TotalInsumos;
        model.ResumenGeneral.AlertasBajoStock = model.InventarioInsumos.TotalBajoStock;

        return model;
    }

    public async Task<List<ChartItemDto>> ObtenerImpresionesPorEstadoAsync(string rol, string? rutPersonal)
    {
        var query = _context.SolicitudesImpresion
            .AsNoTracking()
            .Include(s => s.EstadoImpresion)
            .AsQueryable();

        if (rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal))
            query = query.Where(s => s.RutPersonal == rutPersonal);

        var estados = await query
            .Select(s => s.EstadoImpresion == null ? string.Empty : s.EstadoImpresion.Estado)
            .ToListAsync();

        var conteos = estados
            .GroupBy(NormalizarEstadoImpresion)
            .ToDictionary(g => g.Key, g => g.Count());

        return new List<ChartItemDto>
        {
            new() { Label = Estado.Pendiente, Value = ObtenerConteo(conteos, Estado.Pendiente) },
            new() { Label = Estado.EnProceso, Value = ObtenerConteo(conteos, Estado.EnProceso) },
            new() { Label = Estado.Entregada, Value = ObtenerConteo(conteos, Estado.Entregada) },
            new() { Label = Estado.Rechazada, Value = ObtenerConteo(conteos, Estado.Rechazada) }
        };
    }

    private async Task<DashboardFiltrosViewModel> CrearFiltrosAsync(int mes, int anio, int? idTipoInsumo, int? idDependencia)
    {
        var cultura = CultureInfo.GetCultureInfo("es-CL");
        var anioActual = DateTime.Today.Year;

        return new DashboardFiltrosViewModel
        {
            Mes = mes,
            Anio = anio,
            IdTipoInsumo = idTipoInsumo,
            IdDependencia = idDependencia,
            Meses = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(CultureInfo.InvariantCulture),
                    Text = cultura.DateTimeFormat.GetMonthName(m),
                    Selected = m == mes
                })
                .ToList(),
            Anios = Enumerable.Range(anioActual - 4, 6)
                .Select(a => new SelectListItem
                {
                    Value = a.ToString(CultureInfo.InvariantCulture),
                    Text = a.ToString(CultureInfo.InvariantCulture),
                    Selected = a == anio
                })
                .ToList(),
            TiposInsumo = await _context.TiposInsumo.AsNoTracking()
                .OrderBy(t => t.NombreTipoInsumo)
                .Select(t => new SelectListItem
                {
                    Value = t.IdTipoInsumo.ToString(),
                    Text = t.NombreTipoInsumo,
                    Selected = idTipoInsumo == t.IdTipoInsumo
                })
                .ToListAsync(),
            Dependencias = await _context.Dependencias.AsNoTracking()
                .OrderBy(d => d.NombreDependencia)
                .Select(d => new SelectListItem
                {
                    Value = d.IdDependencia.ToString(),
                    Text = d.NombreDependencia,
                    Selected = idDependencia == d.IdDependencia
                })
                .ToListAsync()
        };
    }

    private async Task<DashboardResumenGeneralViewModel> ObtenerResumenGeneralAsync(DashboardTecnologiaIndicadoresViewModel tecnologia)
    {
        return new DashboardResumenGeneralViewModel
        {
            TotalTecnologias = tecnologia.TotalRegistradas,
            TecnologiasDisponibles = tecnologia.Disponibles,
            TecnologiasAsignadas = tecnologia.Asignadas,
            TecnologiasEnReparacion = tecnologia.EnReparacion,
            TecnologiasDadasDeBaja = tecnologia.DadasDeBaja,
            TotalInsumos = await _context.Insumos.AsNoTracking().CountAsync(i => i.Estado),
            ImpresionesPendientes = 0,
            AlertasBajoStock = await _context.Insumos.AsNoTracking().CountAsync(i => i.Estado && i.StockActual < i.StockMinimo)
        };
    }

    private async Task<DashboardTecnologiaIndicadoresViewModel> ObtenerTecnologiasAsync()
    {
        var total = await _context.Tecnologias.AsNoTracking().CountAsync();
        var asignadas = await _context.Asignaciones.AsNoTracking().CountAsync(a =>
            a.FechaDevolucion == null && (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa"));
        var enReparacion = await _context.Reparaciones.AsNoTracking().CountAsync(r =>
            r.EstadoReparacion != Estado.Reparada && r.EstadoReparacion != Estado.Rechazada);
        var bajas = await _context.Bajas.AsNoTracking().CountAsync(b => b.Estado == Estado.Aprobada);

        return new DashboardTecnologiaIndicadoresViewModel
        {
            TotalRegistradas = total,
            Asignadas = asignadas,
            EnReparacion = enReparacion,
            DadasDeBaja = bajas,
            Disponibles = Math.Max(0, total - asignadas - enReparacion - bajas)
        };
    }

    private async Task<DashboardImpresionesViewModel> ObtenerImpresionesAsync(DateTime desde, DateTime hasta, string? rutPersonal)
    {
        var query = _context.SolicitudesImpresion.AsNoTracking()
            .Include(s => s.EstadoImpresion)
            .Include(s => s.Personal)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(rutPersonal))
            query = query.Where(s => s.RutPersonal == rutPersonal);

        var solicitudes = await query
            .Select(s => new
            {
                s.FechaSolicitud,
                s.CantidadPaginas,
                s.CantidadCopias,
                Estado = s.EstadoImpresion == null ? string.Empty : s.EstadoImpresion.Estado,
                Solicitante = s.Personal == null ? s.RutPersonal : s.Personal.Nombre + " " + s.Personal.Apellido
            })
            .ToListAsync();

        var mensuales = solicitudes.Where(s => s.FechaSolicitud >= desde && s.FechaSolicitud < hasta).ToList();
        var estados = solicitudes.GroupBy(s => NormalizarEstadoImpresion(s.Estado)).ToDictionary(g => g.Key, g => g.Count());
        var resueltas = mensuales.Count(s =>
            NormalizarEstadoImpresion(s.Estado) == Estado.Entregada ||
            NormalizarEstadoImpresion(s.Estado) == Estado.Rechazada);

        return new DashboardImpresionesViewModel
        {
            Pendientes = ObtenerConteo(estados, Estado.Pendiente),
            EnProceso = ObtenerConteo(estados, Estado.EnProceso),
            Entregadas = ObtenerConteo(estados, Estado.Entregada),
            Rechazadas = ObtenerConteo(estados, Estado.Rechazada),
            TotalMensualSolicitudes = mensuales.Count,
            TotalMensualPaginas = mensuales.Sum(s => s.CantidadPaginas * s.CantidadCopias),
            TotalMensualCopias = mensuales.Sum(s => s.CantidadCopias),
            PorcentajeResueltas = mensuales.Count == 0 ? 0 : Math.Round((decimal)resueltas * 100 / mensuales.Count, 1),
            UltimasSolicitudes = solicitudes
                .OrderByDescending(s => s.FechaSolicitud)
                .Take(5)
                .Select(s => new DashboardImpresionRecienteViewModel
                {
                    Solicitante = s.Solicitante,
                    Estado = NormalizarEstadoImpresion(s.Estado),
                    FechaSolicitud = s.FechaSolicitud,
                    TotalImpresiones = s.CantidadPaginas * s.CantidadCopias
                })
                .ToList()
        };
    }

    private async Task<DashboardInventarioInsumosViewModel> ObtenerInventarioInsumosAsync(int? idTipoInsumo)
    {
        var query =
            from insumo in _context.Insumos.AsNoTracking()
            join tipo in _context.TiposInsumo.AsNoTracking() on insumo.IdTipoInsumo equals tipo.IdTipoInsumo
            where insumo.Estado
            select new
            {
                insumo.IdTipoInsumo,
                insumo.NombreInsumo,
                Tipo = tipo.NombreTipoInsumo,
                insumo.UnidadMedida,
                insumo.StockActual,
                insumo.StockMinimo
            };

        if (idTipoInsumo.HasValue)
            query = query.Where(i => i.IdTipoInsumo == idTipoInsumo.Value);

        var insumos = await query.ToListAsync();

        return new DashboardInventarioInsumosViewModel
        {
            TotalInsumos = insumos.Count,
            TotalBajoStock = insumos.Count(i => i.StockActual < i.StockMinimo),
            InsumosPorTipo = insumos
                .GroupBy(i => i.Tipo)
                .OrderBy(g => g.Key)
                .Select(g => new DashboardStockTipoViewModel
                {
                    Tipo = g.Key,
                    Total = g.Count(),
                    BajoStock = g.Count(i => i.StockActual < i.StockMinimo)
                })
                .ToList(),
            MayorStock = insumos
                .OrderByDescending(i => i.StockActual)
                .Take(5)
                .Select(i => CrearInsumoStock(i.NombreInsumo, i.Tipo, i.UnidadMedida, i.StockActual, i.StockMinimo))
                .ToList(),
            MenorStock = insumos
                .OrderBy(i => i.StockActual)
                .ThenByDescending(i => i.StockMinimo - i.StockActual)
                .Take(5)
                .Select(i => CrearInsumoStock(i.NombreInsumo, i.Tipo, i.UnidadMedida, i.StockActual, i.StockMinimo))
                .ToList(),
            RecomendacionCompra = insumos
                .Where(i => i.StockActual < i.StockMinimo)
                .OrderByDescending(i => i.StockMinimo - i.StockActual)
                .ThenBy(i => i.NombreInsumo)
                .Take(10)
                .Select(i => new DashboardInsumoCompraViewModel
                {
                    Nombre = i.NombreInsumo,
                    Tipo = i.Tipo,
                    UnidadMedida = i.UnidadMedida,
                    StockActual = i.StockActual,
                    StockMinimo = i.StockMinimo,
                    CantidadSugerida = i.StockMinimo - i.StockActual
                })
                .ToList()
        };
    }

    private async Task<DashboardAsignacionesIndicadoresViewModel> ObtenerAsignacionesAsync(DateTime desde, DateTime hasta, string? rutPersonal, int? idDependencia)
    {
        var query = _context.Asignaciones.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(rutPersonal))
            query = query.Where(a => a.RutPersonal == rutPersonal);
        if (idDependencia.HasValue)
            query = query.Where(a => a.IdDependencia == idDependencia.Value);

        var asignaciones = await query.ToListAsync();
        var ultimas = await (
            from asignacion in query.OrderByDescending(a => a.FechaAsignacion).Take(5)
            join tecnologia in _context.Tecnologias.AsNoTracking() on asignacion.IdTecnologia equals tecnologia.IdTecnologia
            select new DashboardMovimientoEquipoViewModel
            {
                CodigoEquipo = tecnologia.SkuCodigoInventario,
                Detalle = asignacion.TipoAsignacion,
                Estado = asignacion.FechaDevolucion == null ? asignacion.EstadoAsignacion : "Finalizada",
                Fecha = asignacion.FechaAsignacion
            })
            .ToListAsync();

        return new DashboardAsignacionesIndicadoresViewModel
        {
            Activas = asignaciones.Count(a => a.FechaDevolucion == null && (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa")),
            Devueltas = asignaciones.Count(a => a.FechaDevolucion != null),
            DelMes = asignaciones.Count(a => a.FechaAsignacion >= desde && a.FechaAsignacion < hasta),
            Ultimas = ultimas
        };
    }

    private async Task<DashboardReparacionesIndicadoresViewModel> ObtenerReparacionesAsync(DateTime desde, DateTime hasta)
    {
        var reparaciones = await _context.Reparaciones.AsNoTracking().ToListAsync();
        var finalizadasMes = reparaciones
            .Where(r => r.FechaRetorno.HasValue && r.FechaRetorno.Value >= desde && r.FechaRetorno.Value < hasta)
            .ToList();

        var antiguas = await (
            from reparacion in _context.Reparaciones.AsNoTracking()
            join tecnologia in _context.Tecnologias.AsNoTracking() on reparacion.IdTecnologia equals tecnologia.IdTecnologia
            where reparacion.EstadoReparacion != Estado.Reparada && reparacion.EstadoReparacion != Estado.Rechazada
            orderby reparacion.FechaEnvio
            select new DashboardReparacionAbiertaViewModel
            {
                CodigoEquipo = tecnologia.SkuCodigoInventario,
                Destino = reparacion.Destino ?? string.Empty,
                Estado = reparacion.EstadoReparacion,
                FechaEnvio = reparacion.FechaEnvio,
                DiasAbierta = EF.Functions.DateDiffDay(reparacion.FechaEnvio, DateTime.Today)
            })
            .Take(5)
            .ToListAsync();

        return new DashboardReparacionesIndicadoresViewModel
        {
            Pendientes = reparaciones.Count(r => r.EstadoReparacion == Estado.Pendiente || r.EstadoReparacion == "Solicitada"),
            EnProceso = reparaciones.Count(r => r.EstadoReparacion == Estado.EnReparacion || r.EstadoReparacion == Estado.EnProceso),
            Finalizadas = reparaciones.Count(r => r.EstadoReparacion == Estado.Reparada),
            PromedioDias = finalizadasMes.Count == 0
                ? 0
                : Math.Round((decimal)finalizadasMes.Average(r => (r.FechaRetorno!.Value.Date - r.FechaEnvio.Date).TotalDays), 1),
            MasAntiguas = antiguas
        };
    }

    private async Task<DashboardBajasIndicadoresViewModel> ObtenerBajasAsync(DateTime desde, DateTime hasta)
    {
        var bajas = await _context.Bajas.AsNoTracking().ToListAsync();
        var ultimas = await (
            from baja in _context.Bajas.AsNoTracking().OrderByDescending(b => b.FechaBaja).Take(5)
            join tecnologia in _context.Tecnologias.AsNoTracking() on baja.IdTecnologia equals tecnologia.IdTecnologia
            select new DashboardMovimientoEquipoViewModel
            {
                CodigoEquipo = tecnologia.SkuCodigoInventario,
                Detalle = baja.Detalle ?? string.Empty,
                Estado = baja.Estado,
                Fecha = baja.FechaBaja
            })
            .ToListAsync();

        return new DashboardBajasIndicadoresViewModel
        {
            Solicitadas = bajas.Count(b => b.Estado == Estado.Pendiente),
            Aprobadas = bajas.Count(b => b.Estado == Estado.Aprobada),
            Rechazadas = bajas.Count(b => b.Estado == Estado.Rechazada),
            DelMes = bajas.Count(b => b.FechaBaja >= desde && b.FechaBaja < hasta),
            Ultimas = ultimas
        };
    }

    private async Task<DashboardAlertasViewModel> ObtenerAlertasAsync(string? rutPersonal)
    {
        var alertas = new List<DashboardAlertaViewModel>();

        var bajoStock = await (
            from insumo in _context.Insumos.AsNoTracking()
            join tipo in _context.TiposInsumo.AsNoTracking() on insumo.IdTipoInsumo equals tipo.IdTipoInsumo
            where insumo.Estado && insumo.StockActual < insumo.StockMinimo
            orderby insumo.StockMinimo - insumo.StockActual descending
            select new { insumo.NombreInsumo, tipo.NombreTipoInsumo, insumo.StockActual, insumo.StockMinimo })
            .Take(5)
            .ToListAsync();

        alertas.AddRange(bajoStock.Select(i => new DashboardAlertaViewModel
        {
            Tipo = "Insumos",
            Titulo = i.NombreInsumo,
            Detalle = $"{i.NombreTipoInsumo}: stock {i.StockActual}, minimo {i.StockMinimo}.",
            Severidad = "Alta"
        }));

        var reparacionesAntiguas = await (
            from reparacion in _context.Reparaciones.AsNoTracking()
            join tecnologia in _context.Tecnologias.AsNoTracking() on reparacion.IdTecnologia equals tecnologia.IdTecnologia
            where reparacion.EstadoReparacion != Estado.Reparada &&
                  reparacion.EstadoReparacion != Estado.Rechazada &&
                  reparacion.FechaEnvio <= DateTime.Today.AddDays(-7)
            orderby reparacion.FechaEnvio
            select new { tecnologia.SkuCodigoInventario, reparacion.FechaEnvio, reparacion.EstadoReparacion })
            .Take(5)
            .ToListAsync();

        alertas.AddRange(reparacionesAntiguas.Select(r => new DashboardAlertaViewModel
        {
            Tipo = "Reparaciones",
            Titulo = r.SkuCodigoInventario,
            Detalle = $"{r.EstadoReparacion}, abierta hace {(DateTime.Today - r.FechaEnvio.Date).Days} dias.",
            Severidad = "Media"
        }));

        var impresionesPendientesQuery = _context.SolicitudesImpresion.AsNoTracking()
            .Include(s => s.EstadoImpresion)
            .Where(s => s.EstadoImpresion != null && s.EstadoImpresion.Estado == Estado.Pendiente);
        if (!string.IsNullOrWhiteSpace(rutPersonal))
            impresionesPendientesQuery = impresionesPendientesQuery.Where(s => s.RutPersonal == rutPersonal);

        var impresionesPendientes = await impresionesPendientesQuery.CountAsync();
        if (impresionesPendientes > 0)
        {
            alertas.Add(new DashboardAlertaViewModel
            {
                Tipo = "Impresiones",
                Titulo = "Solicitudes pendientes",
                Detalle = $"{impresionesPendientes} solicitudes requieren revision.",
                Severidad = "Media"
            });
        }

        var bajasPendientes = await _context.Bajas.AsNoTracking().CountAsync(b => b.Estado == Estado.Pendiente);
        if (bajasPendientes > 0)
        {
            alertas.Add(new DashboardAlertaViewModel
            {
                Tipo = "Bajas",
                Titulo = "Bajas pendientes",
                Detalle = $"{bajasPendientes} solicitudes esperan aprobacion.",
                Severidad = "Alta"
            });
        }

        var tecnologiasSinAsignar = await (
            from tecnologia in _context.Tecnologias.AsNoTracking()
            join entrada in _context.EntradasTecnologia.AsNoTracking() on tecnologia.IdEntradaTecnologia equals entrada.IdEntradaTecnologia into entradas
            from entrada in entradas.DefaultIfEmpty()
            where tecnologia.Estado &&
                  !_context.Asignaciones.Any(a => a.IdTecnologia == tecnologia.IdTecnologia && a.FechaDevolucion == null) &&
                  !_context.Reparaciones.Any(r => r.IdTecnologia == tecnologia.IdTecnologia && r.EstadoReparacion != Estado.Reparada && r.EstadoReparacion != Estado.Rechazada) &&
                  !_context.Bajas.Any(b => b.IdTecnologia == tecnologia.IdTecnologia && b.Estado == Estado.Aprobada) &&
                  entrada != null &&
                  entrada.FechaEntrada <= DateTime.Today.AddDays(-90)
            orderby entrada!.FechaEntrada
            select new { tecnologia.SkuCodigoInventario, entrada.FechaEntrada })
            .Take(5)
            .ToListAsync();

        alertas.AddRange(tecnologiasSinAsignar.Select(t => new DashboardAlertaViewModel
        {
            Tipo = "Tecnologia",
            Titulo = t.SkuCodigoInventario,
            Detalle = $"Disponible sin asignacion desde {t.FechaEntrada:dd/MM/yyyy}.",
            Severidad = "Baja"
        }));

        return new DashboardAlertasViewModel
        {
            Items = alertas
                .OrderBy(a => a.Severidad == "Alta" ? 0 : a.Severidad == "Media" ? 1 : 2)
                .ThenBy(a => a.Tipo)
                .Take(12)
                .ToList()
        };
    }

    private static DashboardInsumoStockViewModel CrearInsumoStock(string nombre, string tipo, string unidad, int stockActual, int stockMinimo)
    {
        return new DashboardInsumoStockViewModel
        {
            Nombre = nombre,
            Tipo = tipo,
            UnidadMedida = unidad,
            StockActual = stockActual,
            StockMinimo = stockMinimo
        };
    }

    private static int ObtenerConteo(IReadOnlyDictionary<string, int> conteos, string estado)
        => conteos.TryGetValue(estado, out var total) ? total : 0;

    private static string NormalizarEstadoImpresion(string? estado)
    {
        return InputValidationHelper.NormalizeKey(estado) switch
        {
            "PENDIENTE" => Estado.Pendiente,
            "APROBADA" => Estado.EnProceso,
            "EN PROCESO" => Estado.EnProceso,
            "ENTREGADA" => Estado.Entregada,
            "RECHAZADA" => Estado.Rechazada,
            _ => estado?.Trim() ?? string.Empty
        };
    }
}
