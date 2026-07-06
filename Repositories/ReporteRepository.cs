using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Repositories;

public class ReporteRepository
{
    private readonly ApplicationDbContext _context;

    public ReporteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReporteEjecutivoViewModel> ObtenerEjecutivoAsync()
    {
        var tecnologias = await _context.Tecnologias.AsNoTracking().ToListAsync();
        var asignacionesActivas = await _context.Asignaciones.AsNoTracking()
            .Where(a => a.FechaDevolucion == null && (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa"))
            .ToListAsync();
        var reparacionesActivas = await _context.Reparaciones.AsNoTracking()
            .Where(r => r.EstadoReparacion != Estado.Reparada && r.EstadoReparacion != Estado.Rechazada)
            .ToListAsync();
        var bajasAprobadas = await _context.Bajas.AsNoTracking().Where(b => b.Estado == Estado.Aprobada).ToListAsync();
        var desdeMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var impresiones = await ObtenerImpresionesNormalizadasAsync();

        var asignadas = asignacionesActivas.Count;
        var enReparacion = reparacionesActivas.Count;
        var dadasBaja = bajasAprobadas.Count;
        var disponibles = Math.Max(0, tecnologias.Count - asignadas - enReparacion - dadasBaja);

        return new ReporteEjecutivoViewModel
        {
            EquiposDisponibles = disponibles,
            EquiposAsignados = asignadas,
            EquiposEnReparacion = enReparacion,
            EquiposDadosDeBaja = dadasBaja,
            InsumosRegistrados = await _context.Insumos.AsNoTracking().CountAsync(),
            EntradasMes = await _context.EntradasInsumo.AsNoTracking().CountAsync(e => e.FechaEntrega >= desdeMes),
            SalidasMes = await _context.SalidasInsumo.AsNoTracking().CountAsync(s => s.FechaSalida >= desdeMes),
            ImpresionesPendientes = impresiones.Count(i => i == Estado.Pendiente),
            ImpresionesEnProceso = impresiones.Count(i => i == Estado.EnProceso),
            ImpresionesEntregadas = impresiones.Count(i => i == Estado.Entregada),
            ImpresionesRechazadas = impresiones.Count(i => i == Estado.Rechazada),
            TecnologiasPorEstado =
            [
                new("Disponible", disponibles),
                new("Asignado", asignadas),
                new("En reparacion", enReparacion),
                new("Dado de baja", dadasBaja)
            ],
            ImpresionesPorEstado =
            [
                new("Pendiente", impresiones.Count(i => i == Estado.Pendiente)),
                new("En proceso", impresiones.Count(i => i == Estado.EnProceso)),
                new("Entregada", impresiones.Count(i => i == Estado.Entregada)),
                new("Rechazada", impresiones.Count(i => i == Estado.Rechazada))
            ]
        };
    }

    public async Task<ReporteInventarioTecnologicoViewModel> ObtenerInventarioTecnologicoAsync(ReporteInventarioTecnologicoFiltro filtro)
    {
        var equipos = await ObtenerTecnologiasBaseAsync();
        var asignaciones = await ObtenerAsignacionesDetalleAsync();

        var filas = equipos.Select(e =>
        {
            var asignacion = asignaciones.FirstOrDefault(a => a.IdTecnologia == e.IdTecnologia && a.EstaActiva);
            var estado = ObtenerEstadoEquipo(e, asignaciones, null, null);
            return new ReporteInventarioTecnologicoFila
            {
                Sku = e.Sku,
                Tipo = e.Tipo,
                Marca = e.Marca,
                Modelo = e.Modelo,
                Estado = estado,
                Dependencia = asignacion?.Dependencia ?? "-",
                FuncionarioAsignado = asignacion?.Funcionario ?? "-",
                FechaIngreso = e.FechaIngreso
            };
        }).ToList();

        filas = filas.Where(f =>
            Coincide(f.Tipo, filtro.TipoTecnologia) &&
            Coincide(f.Marca, filtro.Marca) &&
            Coincide(f.Modelo, filtro.Modelo) &&
            Coincide(f.Estado, filtro.Estado) &&
            Coincide(f.Dependencia, filtro.Dependencia))
            .OrderBy(f => f.Tipo)
            .ThenBy(f => f.Marca)
            .ThenBy(f => f.Modelo)
            .ThenBy(f => f.Sku)
            .ToList();

        return new ReporteInventarioTecnologicoViewModel
        {
            Filtro = filtro,
            Filas = filas,
            Tipos = Select(filas.Select(f => f.Tipo)),
            Marcas = Select(filas.Select(f => f.Marca)),
            Modelos = Select(filas.Select(f => f.Modelo)),
            Estados = Select(["Disponible", "Asignado", "En Reparacion", "Dado de Baja"]),
            Dependencias = Select(filas.Select(f => f.Dependencia).Where(d => d != "-"))
        };
    }

    public async Task<ReporteMovimientosInsumosViewModel> ObtenerMovimientosInsumosAsync(ReporteMovimientosInsumosFiltro filtro)
    {
        var insumos = await _context.Insumos.AsNoTracking().ToDictionaryAsync(i => i.IdInsumo);
        var tipos = await _context.TiposInsumo.AsNoTracking().ToDictionaryAsync(t => t.IdTipoInsumo, t => t.NombreTipoInsumo);
        var proveedores = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.IdProveedor, p => p.NombreProveedor);
        var dependencias = await _context.Dependencias.AsNoTracking().ToDictionaryAsync(d => d.IdDependencia, d => d.NombreDependencia);
        var personal = await _context.Personal.AsNoTracking().ToDictionaryAsync(p => p.RutPersonal, p => $"{p.Nombre} {p.Apellido}");

        var entradas = await _context.EntradasInsumo.AsNoTracking().ToListAsync();
        var salidas = await _context.SalidasInsumo.AsNoTracking().ToListAsync();

        var filas = entradas.Select(e =>
        {
            var insumo = insumos.GetValueOrDefault(e.IdInsumo);
            return new ReporteMovimientoInsumoFila
            {
                Fecha = e.FechaEntrega,
                Movimiento = "Entrada",
                Insumo = insumo?.NombreInsumo ?? $"Insumo #{e.IdInsumo}",
                TipoInsumo = insumo != null ? tipos.GetValueOrDefault(insumo.IdTipoInsumo, string.Empty) : string.Empty,
                Cantidad = e.Cantidad,
                Proveedor = proveedores.GetValueOrDefault(e.IdProveedor, "-"),
                Funcionario = "-",
                Dependencia = "-",
                StockActual = insumo?.StockActual ?? 0
            };
        }).Concat(salidas.Select(s =>
        {
            var insumo = insumos.GetValueOrDefault(s.IdInsumo);
            return new ReporteMovimientoInsumoFila
            {
                Fecha = s.FechaSalida,
                Movimiento = "Salida",
                Insumo = insumo?.NombreInsumo ?? $"Insumo #{s.IdInsumo}",
                TipoInsumo = insumo != null ? tipos.GetValueOrDefault(insumo.IdTipoInsumo, string.Empty) : string.Empty,
                Cantidad = s.Cantidad,
                Proveedor = "-",
                Funcionario = personal.GetValueOrDefault(s.RutPersonal, s.RutPersonal),
                Dependencia = dependencias.GetValueOrDefault(s.IdDependencia, "-"),
                StockActual = insumo?.StockActual ?? 0
            };
        })).OrderByDescending(f => f.Fecha).ToList();

        filas = filas.Where(f =>
            (!filtro.FechaDesde.HasValue || f.Fecha.Date >= filtro.FechaDesde.Value.Date) &&
            (!filtro.FechaHasta.HasValue || f.Fecha.Date <= filtro.FechaHasta.Value.Date) &&
            Coincide(f.TipoInsumo, filtro.TipoInsumo) &&
            Coincide(f.Proveedor, filtro.Proveedor) &&
            Coincide(f.Dependencia, filtro.Dependencia)).ToList();

        return new ReporteMovimientosInsumosViewModel
        {
            Filtro = filtro,
            Filas = filas,
            TotalEntradas = filas.Where(f => f.Movimiento == "Entrada").Sum(f => f.Cantidad),
            TotalSalidas = filas.Where(f => f.Movimiento == "Salida").Sum(f => f.Cantidad),
            StockActual = filas.GroupBy(f => f.Insumo).Sum(g => g.Last().StockActual),
            TiposInsumo = Select(tipos.Values),
            Proveedores = Select(proveedores.Values),
            Dependencias = Select(dependencias.Values)
        };
    }

    public async Task<ReporteAsignacionesViewModel> ObtenerAsignacionesAsync(ReporteAsignacionesFiltro filtro)
    {
        var filas = await ObtenerAsignacionesDetalleAsync();
        filas = filas.Where(f =>
            Coincide(f.Funcionario, filtro.Funcionario) &&
            Coincide(f.Dependencia, filtro.Dependencia) &&
            Coincide(f.TipoTecnologia, filtro.TipoTecnologia) &&
            Coincide(f.Estado, filtro.Estado))
            .OrderByDescending(f => f.FechaAsignacion)
            .ThenBy(f => f.Funcionario)
            .ThenBy(f => f.Sku)
            .ToList();

        return new ReporteAsignacionesViewModel
        {
            Filtro = filtro,
            Filas = filas,
            Funcionarios = Select(filas.Select(f => f.Funcionario)),
            Dependencias = Select(filas.Select(f => f.Dependencia)),
            TiposTecnologia = Select(filas.Select(f => f.TipoTecnologia)),
            Estados = Select(["Activa", "Vigente", "Finalizada"])
        };
    }

    public async Task<ReporteReparacionesViewModel> ObtenerReparacionesAsync(ReporteReparacionesFiltro filtro)
    {
        var equipos = await ObtenerTecnologiasBaseAsync();
        var reparaciones = await _context.Reparaciones.AsNoTracking().ToListAsync();
        var filas = reparaciones.Select(r =>
        {
            var equipo = equipos.FirstOrDefault(e => e.IdTecnologia == r.IdTecnologia);
            return new ReporteReparacionFila
            {
                Sku = equipo?.Sku ?? $"Equipo #{r.IdTecnologia}",
                Equipo = equipo == null ? "-" : $"{equipo.Tipo} {equipo.Marca} {equipo.Modelo}",
                Destino = r.Destino ?? "-",
                FechaEnvio = r.FechaEnvio,
                FechaRetorno = r.FechaRetorno,
                Estado = r.EstadoReparacion,
                Observaciones = r.Detalle ?? "-"
            };
        }).Where(f =>
            Coincide(f.Estado, filtro.Estado) &&
            (!filtro.Fecha.HasValue || f.FechaEnvio.Date == filtro.Fecha.Value.Date))
            .OrderByDescending(f => f.FechaEnvio)
            .ThenBy(f => f.Sku)
            .ToList();

        var finalizadas = filas.Where(f => f.FechaRetorno.HasValue).ToList();
        return new ReporteReparacionesViewModel
        {
            Filtro = filtro,
            Filas = filas,
            Pendientes = filas.Count(f => !f.FechaRetorno.HasValue && f.Estado != Estado.Rechazada),
            Finalizadas = finalizadas.Count,
            TiempoPromedioReparacion = finalizadas.Count == 0 ? 0 : finalizadas.Average(f => (f.FechaRetorno!.Value.Date - f.FechaEnvio.Date).TotalDays),
            Estados = Select(["Pendiente", "Solicitada", "En Reparacion", "Reparada", "Rechazada"])
        };
    }

    public async Task<ReporteBajasViewModel> ObtenerBajasAsync(ReporteBajasFiltro filtro)
    {
        var equipos = await ObtenerTecnologiasBaseAsync();
        var bajas = await _context.Bajas.AsNoTracking().ToListAsync();
        var filas = bajas.Select(b =>
        {
            var equipo = equipos.FirstOrDefault(e => e.IdTecnologia == b.IdTecnologia);
            var motivo = NombreMotivo(b.IdMotivo);
            return new ReporteBajaFila
            {
                Sku = equipo?.Sku ?? $"Equipo #{b.IdTecnologia}",
                Equipo = equipo == null ? "-" : $"{equipo.Tipo} {equipo.Marca} {equipo.Modelo}",
                TipoTecnologia = equipo?.Tipo ?? "-",
                Motivo = motivo,
                FechaBaja = b.FechaBaja,
                UsuarioRegistra = b.UsuarioRegistraBaja ?? "-",
                UsuarioAutoriza = b.UsuarioAutorizaBaja ?? "-"
            };
        }).Where(f =>
            (!filtro.Fecha.HasValue || f.FechaBaja.Date == filtro.Fecha.Value.Date) &&
            Coincide(f.Motivo, filtro.Motivo) &&
            Coincide(f.TipoTecnologia, filtro.TipoTecnologia))
            .OrderByDescending(f => f.FechaBaja)
            .ThenBy(f => f.Sku)
            .ToList();

        return new ReporteBajasViewModel
        {
            Filtro = filtro,
            Filas = filas,
            CantidadBajas = filas.Count,
            BajasPorTipo = filas.GroupBy(f => f.TipoTecnologia).Select(g => new ReporteConteo(g.Key, g.Count())).ToList(),
            BajasPorMotivo = filas.GroupBy(f => f.Motivo).Select(g => new ReporteConteo(g.Key, g.Count())).ToList(),
            Motivos = Select(["Obsolescencia", "Dano irreparable", "Perdida o extravio", "Robo informado"]),
            TiposTecnologia = Select(equipos.Select(e => e.Tipo))
        };
    }

    public async Task<ReporteImpresionesViewModel> ObtenerImpresionesAsync(ReporteImpresionesFiltro filtro)
    {
        var solicitudes = await _context.SolicitudesImpresion.AsNoTracking().Include(s => s.EstadoImpresion).Include(s => s.Personal).ToListAsync();
        var salidas = await _context.SalidasInsumo.AsNoTracking().ToListAsync();
        var dependencias = await _context.Dependencias.AsNoTracking().ToDictionaryAsync(d => d.IdDependencia, d => d.NombreDependencia);
        var dependenciaPorRut = salidas.GroupBy(s => s.RutPersonal).ToDictionary(g => g.Key, g => dependencias.GetValueOrDefault(g.OrderByDescending(x => x.FechaSalida).First().IdDependencia, "-"));

        var filas = solicitudes.Select(s =>
        {
            var solicitante = s.Personal == null ? s.RutPersonal : $"{s.Personal.Nombre} {s.Personal.Apellido}";
            var estado = NormalizarEstadoImpresion(s.EstadoImpresion?.Estado);
            return new ReporteImpresionFila
            {
                Solicitante = solicitante,
                RutPersonal = s.RutPersonal,
                Dependencia = dependenciaPorRut.GetValueOrDefault(s.RutPersonal, "-"),
                CantidadPaginas = s.CantidadPaginas,
                CantidadCopias = s.CantidadCopias,
                Color = s.Color,
                DobleCara = s.DobleCara,
                Estado = estado,
                FechaSolicitud = s.FechaSolicitud,
                FechaEntrega = s.FechaEntrega
            };
        }).Where(f =>
            (!filtro.Fecha.HasValue || f.FechaSolicitud.Date == filtro.Fecha.Value.Date) &&
            Coincide(f.Solicitante, filtro.Funcionario) &&
            Coincide(f.Estado, filtro.Estado) &&
            Coincide(f.Dependencia, filtro.Dependencia))
            .OrderByDescending(f => f.FechaSolicitud)
            .ThenBy(f => f.Solicitante)
            .ToList();

        return new ReporteImpresionesViewModel
        {
            Filtro = filtro,
            Filas = filas,
            TotalSolicitudes = filas.Count,
            TotalPaginas = filas.Sum(f => f.CantidadPaginas),
            TotalCopias = filas.Sum(f => f.CantidadCopias),
            TotalImpresiones = filas.Sum(f => f.TotalImpresiones),
            TotalColor = filas.Where(f => f.Color.Contains("Color", StringComparison.OrdinalIgnoreCase)).Sum(f => f.TotalImpresiones),
            TotalBlancoNegro = filas.Where(f => !f.Color.Contains("Color", StringComparison.OrdinalIgnoreCase)).Sum(f => f.TotalImpresiones),
            Funcionarios = Select(filas.Select(f => f.Solicitante)),
            Estados = Select(["Pendiente", "En Proceso", "Entregada", "Rechazada"]),
            Dependencias = Select(filas.Select(f => f.Dependencia).Where(d => d != "-"))
        };
    }

    public async Task<ReportePersonalViewModel> ObtenerPersonalAsync(ReportePersonalFiltro filtro)
    {
        var usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.RutPersonal);
        var personal = await _context.Personal.AsNoTracking().ToListAsync();
        var filas = personal.Select(p =>
        {
            usuarios.TryGetValue(p.RutPersonal, out var usuario);
            return new ReportePersonalFila
            {
                Funcionario = $"{p.Nombre} {p.Apellido}",
                Cargo = p.Cargo ?? "-",
                Correo = p.Correo,
                Usuario = usuario == null ? "Sin acceso" : "Con acceso",
                Estado = p.Activo ? "Activo" : "Inactivo",
                UltimoAcceso = usuario?.UltimoAcceso
            };
        }).Where(f => Coincide(f.Cargo, filtro.Cargo) && Coincide(f.Estado, filtro.Estado))
            .OrderBy(f => f.Funcionario)
            .ToList();

        return new ReportePersonalViewModel
        {
            Filtro = filtro,
            Filas = filas,
            Cargos = Select(filas.Select(f => f.Cargo).Where(c => c != "-")),
            Estados = Select(["Activo", "Inactivo"])
        };
    }

    private async Task<List<TecnologiaBase>> ObtenerTecnologiasBaseAsync()
    {
        var tecnologias = await _context.Tecnologias.AsNoTracking().ToListAsync();
        var modelos = await _context.Modelos.AsNoTracking().ToDictionaryAsync(m => m.IdModelo);
        var marcas = await _context.Marcas.AsNoTracking().ToDictionaryAsync(m => m.IdMarca);
        var tipos = await _context.TiposTecnologia.AsNoTracking().ToDictionaryAsync(t => t.IdTipoTecnologia);
        var entradas = await _context.EntradasTecnologia.AsNoTracking().ToDictionaryAsync(e => e.IdEntradaTecnologia);

        return tecnologias.Select(t =>
        {
            modelos.TryGetValue(t.IdModelo, out var modelo);
            var marca = modelo != null ? marcas.GetValueOrDefault(modelo.IdMarca)?.NombreMarca : null;
            return new TecnologiaBase
            {
                IdTecnologia = t.IdTecnologia,
                Sku = t.SkuCodigoInventario,
                Tipo = tipos.GetValueOrDefault(t.IdTipoTecnologia)?.NombreTipoTecnologia ?? "-",
                Marca = marca ?? "-",
                Modelo = modelo?.NombreModelo ?? "-",
                FechaIngreso = t.IdEntradaTecnologia.HasValue && entradas.TryGetValue(t.IdEntradaTecnologia.Value, out var entrada) ? entrada.FechaEntrada : null,
                Activo = t.Estado
            };
        }).ToList();
    }

    private async Task<List<ReporteAsignacionFila>> ObtenerAsignacionesDetalleAsync()
    {
        var asignaciones = await _context.Asignaciones.AsNoTracking().ToListAsync();
        var equipos = await ObtenerTecnologiasBaseAsync();
        var personal = await _context.Personal.AsNoTracking().ToDictionaryAsync(p => p.RutPersonal, p => $"{p.Nombre} {p.Apellido}");
        var dependencias = await _context.Dependencias.AsNoTracking().ToDictionaryAsync(d => d.IdDependencia, d => d.NombreDependencia);

        return asignaciones.Select(a =>
        {
            var equipo = equipos.FirstOrDefault(e => e.IdTecnologia == a.IdTecnologia);
            return new ReporteAsignacionFila
            {
                IdTecnologia = a.IdTecnologia,
                Sku = equipo?.Sku ?? $"Equipo #{a.IdTecnologia}",
                Equipo = equipo == null ? "-" : $"{equipo.Tipo} {equipo.Marca} {equipo.Modelo}",
                TipoTecnologia = equipo?.Tipo ?? "-",
                Funcionario = string.IsNullOrWhiteSpace(a.RutPersonal)
                    ? "-"
                    : personal.GetValueOrDefault(a.RutPersonal, a.RutPersonal),
                Dependencia = a.IdDependencia.HasValue
                    ? dependencias.GetValueOrDefault(a.IdDependencia.Value, "-")
                    : "-",
                FechaAsignacion = a.FechaAsignacion,
                FechaDevolucion = a.FechaDevolucion,
                Estado = a.FechaDevolucion.HasValue ? "Finalizada" : a.EstadoAsignacion,
                EstaActiva = !a.FechaDevolucion.HasValue && (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa")
            };
        }).ToList();
    }

    private static string ObtenerEstadoEquipo(TecnologiaBase equipo, List<ReporteAsignacionFila> asignaciones, List<Reparacion>? reparaciones, List<Baja>? bajas)
    {
        if (!equipo.Activo)
            return "Dado de Baja";
        if (bajas?.Any(b => b.IdTecnologia == equipo.IdTecnologia && b.Estado == Estado.Aprobada) == true)
            return "Dado de Baja";
        if (reparaciones?.Any(r => r.IdTecnologia == equipo.IdTecnologia && r.EstadoReparacion != Estado.Reparada && r.EstadoReparacion != Estado.Rechazada) == true)
            return "En Reparacion";
        if (asignaciones.Any(a => a.IdTecnologia == equipo.IdTecnologia && a.EstaActiva))
            return "Asignado";
        return "Disponible";
    }

    private async Task<List<string>> ObtenerImpresionesNormalizadasAsync()
    {
        var solicitudes = await _context.SolicitudesImpresion.AsNoTracking().Include(s => s.EstadoImpresion).ToListAsync();
        return solicitudes.Select(s => NormalizarEstadoImpresion(s.EstadoImpresion?.Estado)).ToList();
    }

    private static string NormalizarEstadoImpresion(string? estado)
        => estado?.Trim().ToUpperInvariant() switch
        {
            "PENDIENTE" => Estado.Pendiente,
            "APROBADA" => Estado.EnProceso,
            "EN PROCESO" => Estado.EnProceso,
            "ENTREGADA" => Estado.Entregada,
            "RECHAZADA" => Estado.Rechazada,
            _ => estado?.Trim() ?? string.Empty
        };

    private static bool Coincide(string? valor, string? filtro)
        => string.IsNullOrWhiteSpace(filtro) || string.Equals(valor, filtro, StringComparison.OrdinalIgnoreCase);

    private static List<SelectListItem> Select(IEnumerable<string> valores)
        => valores.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().OrderBy(v => v).Select(v => new SelectListItem { Value = v, Text = v }).ToList();

    private static string NombreMotivo(int idMotivo)
        => idMotivo switch
        {
            1 => "Obsolescencia",
            2 => "Dano irreparable",
            3 => "Perdida o extravio",
            4 => "Robo informado",
            _ => $"Motivo #{idMotivo}"
        };

    private sealed class TecnologiaBase
    {
        public int IdTecnologia { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public DateTime? FechaIngreso { get; set; }
        public bool Activo { get; set; }
    }
}
