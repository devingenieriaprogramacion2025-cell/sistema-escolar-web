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
        var mesFiltro = mes is >= 1 and <= 12 ? mes.Value : (int?)null;
        var anioFiltro = anio is >= 2000 and <= 2100 ? anio.Value : hoy.Year;
        var desde = mesFiltro.HasValue
            ? new DateTime(anioFiltro, mesFiltro.Value, 1)
            : new DateTime(anioFiltro, 1, 1);
        var hasta = mesFiltro.HasValue
            ? desde.AddMonths(1)
            : desde.AddYears(1);
        var esProfesor = rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal);

        var model = new DashboardViewModel
        {
            Filtros = await CrearFiltrosAsync(mesFiltro, anioFiltro, idTipoInsumo, idDependencia)
        };

        model.GestionTecnologica.Tecnologias = await ObtenerTecnologiasAsync(desde, hasta, idDependencia);
        model.ResumenGeneral = await ObtenerResumenGeneralAsync(model.GestionTecnologica.Tecnologias);
        model.Impresiones = await ObtenerImpresionesAsync(desde, hasta, esProfesor ? rutPersonal : null);
        model.InventarioInsumos = await ObtenerInventarioInsumosAsync(desde, hasta, idTipoInsumo, idDependencia);
        model.GestionTecnologica.Asignaciones = await ObtenerAsignacionesAsync(desde, hasta, esProfesor ? rutPersonal : null, idDependencia);
        model.GestionTecnologica.Reparaciones = await ObtenerReparacionesAsync(desde, hasta, idDependencia);
        model.GestionTecnologica.Bajas = await ObtenerBajasAsync(desde, hasta, idDependencia);
        model.Alertas = await ObtenerAlertasAsync(desde, hasta, esProfesor ? rutPersonal : null, idTipoInsumo, idDependencia);

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

    private async Task<DashboardFiltrosViewModel> CrearFiltrosAsync(int? mes, int anio, int? idTipoInsumo, int? idDependencia)
    {
        var cultura = CultureInfo.GetCultureInfo("es-CL");
        var anioActual = DateTime.Today.Year;

        return new DashboardFiltrosViewModel
        {
            Mes = mes,
            Anio = anio,
            IdTipoInsumo = idTipoInsumo,
            IdDependencia = idDependencia,
            Meses = new[]
                {
                    new SelectListItem
                    {
                        Value = string.Empty,
                        Text = "Todos",
                        Selected = !mes.HasValue
                    }
                }
                .Concat(Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(CultureInfo.InvariantCulture),
                    Text = cultura.DateTimeFormat.GetMonthName(m),
                    Selected = m == mes
                }))
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

    private Task<DashboardResumenGeneralViewModel> ObtenerResumenGeneralAsync(DashboardTecnologiaIndicadoresViewModel tecnologia)
    {
        return Task.FromResult(new DashboardResumenGeneralViewModel
        {
            TotalTecnologias = tecnologia.TotalRegistradas,
            TecnologiasDisponibles = tecnologia.Disponibles,
            TecnologiasAsignadas = tecnologia.Asignadas,
            TecnologiasEnReparacion = tecnologia.EnReparacion,
            TecnologiasDadasDeBaja = tecnologia.DadasDeBaja,
            TotalInsumos = 0,
            ImpresionesPendientes = 0,
            AlertasBajoStock = 0
        });
    }

    private async Task<DashboardTecnologiaIndicadoresViewModel> ObtenerTecnologiasAsync(DateTime desde, DateTime hasta, int? idDependencia)
    {
        var tecnologiasPeriodo =
            from tecnologia in _context.Tecnologias.AsNoTracking()
            join entrada in _context.EntradasTecnologia.AsNoTracking()
                on tecnologia.IdEntradaTecnologia equals entrada.IdEntradaTecnologia
            where entrada.FechaEntrada >= desde && entrada.FechaEntrada < hasta
            select tecnologia;

        if (idDependencia.HasValue)
        {
            tecnologiasPeriodo = tecnologiasPeriodo.Where(t =>
                _context.Asignaciones.Any(a =>
                    a.IdTecnologia == t.IdTecnologia &&
                    a.IdDependencia == idDependencia.Value &&
                    a.FechaAsignacion >= desde &&
                    a.FechaAsignacion < hasta));
        }

        var tecnologiaIds = await tecnologiasPeriodo
            .Select(t => t.IdTecnologia)
            .Distinct()
            .ToListAsync();

        var total = tecnologiaIds.Count;
        var asignadas = await _context.Asignaciones.AsNoTracking().CountAsync(a =>
            tecnologiaIds.Contains(a.IdTecnologia) &&
            (!idDependencia.HasValue || a.IdDependencia == idDependencia.Value) &&
            a.FechaAsignacion >= desde &&
            a.FechaAsignacion < hasta &&
            a.FechaDevolucion == null &&
            (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa"));
        var enReparacion = await _context.Reparaciones.AsNoTracking().CountAsync(r =>
            tecnologiaIds.Contains(r.IdTecnologia) &&
            r.FechaEnvio >= desde &&
            r.FechaEnvio < hasta &&
            r.EstadoReparacion != Estado.Reparada &&
            r.EstadoReparacion != Estado.Rechazada);
        var bajas = await _context.Bajas.AsNoTracking().CountAsync(b =>
            tecnologiaIds.Contains(b.IdTecnologia) &&
            b.FechaBaja >= desde &&
            b.FechaBaja < hasta &&
            b.Estado == Estado.Aprobada);

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
            .Where(s => s.FechaSolicitud >= desde && s.FechaSolicitud < hasta)
            .Select(s => new
            {
                s.FechaSolicitud,
                s.CantidadPaginas,
                s.CantidadCopias,
                Estado = s.EstadoImpresion == null ? string.Empty : s.EstadoImpresion.Estado,
                Solicitante = s.Personal == null ? s.RutPersonal : s.Personal.Nombre + " " + s.Personal.Apellido
            })
            .ToListAsync();

        var estados = solicitudes.GroupBy(s => NormalizarEstadoImpresion(s.Estado)).ToDictionary(g => g.Key, g => g.Count());
        var resueltas = solicitudes.Count(s =>
            NormalizarEstadoImpresion(s.Estado) == Estado.Entregada ||
            NormalizarEstadoImpresion(s.Estado) == Estado.Rechazada);

        return new DashboardImpresionesViewModel
        {
            Pendientes = ObtenerConteo(estados, Estado.Pendiente),
            EnProceso = ObtenerConteo(estados, Estado.EnProceso),
            Entregadas = ObtenerConteo(estados, Estado.Entregada),
            Rechazadas = ObtenerConteo(estados, Estado.Rechazada),
            TotalMensualSolicitudes = solicitudes.Count,
            TotalMensualPaginas = solicitudes.Sum(s => s.CantidadPaginas * s.CantidadCopias),
            TotalMensualCopias = solicitudes.Sum(s => s.CantidadCopias),
            PorcentajeResueltas = solicitudes.Count == 0 ? 0 : Math.Round((decimal)resueltas * 100 / solicitudes.Count, 1),
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

    private async Task<DashboardInventarioInsumosViewModel> ObtenerInventarioInsumosAsync(
        DateTime desde,
        DateTime hasta,
        int? idTipoInsumo,
        int? idDependencia)
    {
        var insumosConMovimiento = _context.EntradasInsumo.AsNoTracking()
            .Where(e => e.FechaEntrega >= desde && e.FechaEntrega < hasta)
            .Select(e => e.IdInsumo);

        var salidasPeriodo = _context.SalidasInsumo.AsNoTracking()
            .Where(s => s.FechaSalida >= desde && s.FechaSalida < hasta);

        if (idDependencia.HasValue)
        {
            salidasPeriodo = salidasPeriodo.Where(s => s.IdDependencia == idDependencia.Value);
            insumosConMovimiento = salidasPeriodo.Select(s => s.IdInsumo);
        }
        else
        {
            insumosConMovimiento = insumosConMovimiento.Union(salidasPeriodo.Select(s => s.IdInsumo));
        }

        var query =
            from insumo in _context.Insumos.AsNoTracking()
            join tipo in _context.TiposInsumo.AsNoTracking() on insumo.IdTipoInsumo equals tipo.IdTipoInsumo
            where insumo.Estado && insumosConMovimiento.Contains(insumo.IdInsumo)
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
        var query = _context.Asignaciones.AsNoTracking()
            .Where(a =>
                (a.FechaAsignacion >= desde && a.FechaAsignacion < hasta) ||
                (a.FechaDevolucion.HasValue && a.FechaDevolucion.Value >= desde && a.FechaDevolucion.Value < hasta))
            .AsQueryable();

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
            Activas = asignaciones.Count(a =>
                a.FechaAsignacion >= desde &&
                a.FechaAsignacion < hasta &&
                a.FechaDevolucion == null &&
                (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa")),
            Devueltas = asignaciones.Count(a =>
                a.FechaDevolucion.HasValue &&
                a.FechaDevolucion.Value >= desde &&
                a.FechaDevolucion.Value < hasta),
            DelMes = asignaciones.Count(a => a.FechaAsignacion >= desde && a.FechaAsignacion < hasta),
            Ultimas = ultimas
        };
    }

    private async Task<DashboardReparacionesIndicadoresViewModel> ObtenerReparacionesAsync(DateTime desde, DateTime hasta, int? idDependencia)
    {
        var query = _context.Reparaciones.AsNoTracking()
            .Where(r =>
                (r.FechaEnvio >= desde && r.FechaEnvio < hasta) ||
                (r.FechaRetorno.HasValue && r.FechaRetorno.Value >= desde && r.FechaRetorno.Value < hasta));

        if (idDependencia.HasValue)
        {
            query = query.Where(r =>
                _context.Asignaciones.Any(a =>
                    a.IdTecnologia == r.IdTecnologia &&
                    a.IdDependencia == idDependencia.Value));
        }

        var reparacionesPeriodo = await query.ToListAsync();
        var finalizadasMes = reparacionesPeriodo
            .Where(r => r.FechaRetorno.HasValue && r.FechaRetorno.Value >= desde && r.FechaRetorno.Value < hasta)
            .ToList();

        var antiguas = await (
            from reparacion in query
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
            Pendientes = reparacionesPeriodo.Count(r =>
                r.FechaEnvio >= desde &&
                r.FechaEnvio < hasta &&
                (r.EstadoReparacion == Estado.Pendiente || r.EstadoReparacion == "Solicitada")),
            EnProceso = reparacionesPeriodo.Count(r =>
                r.FechaEnvio >= desde &&
                r.FechaEnvio < hasta &&
                (r.EstadoReparacion == Estado.EnReparacion || r.EstadoReparacion == Estado.EnProceso)),
            Finalizadas = finalizadasMes.Count(r => r.EstadoReparacion == Estado.Reparada),
            PromedioDias = finalizadasMes.Count == 0
                ? 0
                : Math.Round((decimal)finalizadasMes.Average(r => (r.FechaRetorno!.Value.Date - r.FechaEnvio.Date).TotalDays), 1),
            MasAntiguas = antiguas
        };
    }

    private async Task<DashboardBajasIndicadoresViewModel> ObtenerBajasAsync(DateTime desde, DateTime hasta, int? idDependencia)
    {
        var query = _context.Bajas.AsNoTracking()
            .Where(b => b.FechaBaja >= desde && b.FechaBaja < hasta);

        if (idDependencia.HasValue)
        {
            query = query.Where(b =>
                _context.Asignaciones.Any(a =>
                    a.IdTecnologia == b.IdTecnologia &&
                    a.IdDependencia == idDependencia.Value));
        }

        var bajas = await query.ToListAsync();
        var ultimas = await (
            from baja in query.OrderByDescending(b => b.FechaBaja).Take(5)
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

    private async Task<DashboardAlertasViewModel> ObtenerAlertasAsync(
        DateTime desde,
        DateTime hasta,
        string? rutPersonal,
        int? idTipoInsumo,
        int? idDependencia)
    {
        var alertas = new List<DashboardAlertaViewModel>();

        var insumosConMovimiento = _context.EntradasInsumo.AsNoTracking()
            .Where(e => e.FechaEntrega >= desde && e.FechaEntrega < hasta)
            .Select(e => e.IdInsumo);

        var salidasPeriodo = _context.SalidasInsumo.AsNoTracking()
            .Where(s => s.FechaSalida >= desde && s.FechaSalida < hasta);

        if (idDependencia.HasValue)
        {
            salidasPeriodo = salidasPeriodo.Where(s => s.IdDependencia == idDependencia.Value);
            insumosConMovimiento = salidasPeriodo.Select(s => s.IdInsumo);
        }
        else
        {
            insumosConMovimiento = insumosConMovimiento.Union(salidasPeriodo.Select(s => s.IdInsumo));
        }

        var bajoStock = await (
            from insumo in _context.Insumos.AsNoTracking()
            join tipo in _context.TiposInsumo.AsNoTracking() on insumo.IdTipoInsumo equals tipo.IdTipoInsumo
            where insumo.Estado &&
                  insumo.StockActual < insumo.StockMinimo &&
                  insumosConMovimiento.Contains(insumo.IdInsumo) &&
                  (!idTipoInsumo.HasValue || insumo.IdTipoInsumo == idTipoInsumo.Value)
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

        var reparacionesQuery = _context.Reparaciones.AsNoTracking()
            .Where(r =>
                r.FechaEnvio >= desde &&
                r.FechaEnvio < hasta &&
                r.EstadoReparacion != Estado.Reparada &&
                r.EstadoReparacion != Estado.Rechazada &&
                r.FechaEnvio <= DateTime.Today.AddDays(-7));

        if (idDependencia.HasValue)
        {
            reparacionesQuery = reparacionesQuery.Where(r =>
                _context.Asignaciones.Any(a =>
                    a.IdTecnologia == r.IdTecnologia &&
                    a.IdDependencia == idDependencia.Value));
        }

        var reparacionesAntiguas = await (
            from reparacion in reparacionesQuery
            join tecnologia in _context.Tecnologias.AsNoTracking() on reparacion.IdTecnologia equals tecnologia.IdTecnologia
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
            .Where(s =>
                s.FechaSolicitud >= desde &&
                s.FechaSolicitud < hasta &&
                s.EstadoImpresion != null &&
                s.EstadoImpresion.Estado == Estado.Pendiente);
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

        var bajasPendientesQuery = _context.Bajas.AsNoTracking()
            .Where(b =>
                b.FechaBaja >= desde &&
                b.FechaBaja < hasta &&
                b.Estado == Estado.Pendiente);

        if (idDependencia.HasValue)
        {
            bajasPendientesQuery = bajasPendientesQuery.Where(b =>
                _context.Asignaciones.Any(a =>
                    a.IdTecnologia == b.IdTecnologia &&
                    a.IdDependencia == idDependencia.Value));
        }

        var bajasPendientes = await bajasPendientesQuery.CountAsync();
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

        if (!idDependencia.HasValue)
        {
            var tecnologiasSinAsignar = await (
                from tecnologia in _context.Tecnologias.AsNoTracking()
                join entrada in _context.EntradasTecnologia.AsNoTracking() on tecnologia.IdEntradaTecnologia equals entrada.IdEntradaTecnologia
                where tecnologia.Estado &&
                      entrada.FechaEntrada >= desde &&
                      entrada.FechaEntrada < hasta &&
                      entrada.FechaEntrada <= DateTime.Today.AddDays(-90) &&
                      !_context.Asignaciones.Any(a => a.IdTecnologia == tecnologia.IdTecnologia && a.FechaDevolucion == null) &&
                      !_context.Reparaciones.Any(r => r.IdTecnologia == tecnologia.IdTecnologia && r.EstadoReparacion != Estado.Reparada && r.EstadoReparacion != Estado.Rechazada) &&
                      !_context.Bajas.Any(b => b.IdTecnologia == tecnologia.IdTecnologia && b.Estado == Estado.Aprobada)
                orderby entrada.FechaEntrada
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
        }

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
