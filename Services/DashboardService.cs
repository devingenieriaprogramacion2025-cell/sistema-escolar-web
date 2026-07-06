using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;

namespace SistemaEscolarWeb.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResumenDto> ObtenerResumenAsync(string rol, string? rutPersonal)
    {
        var asignadas = await _context.Asignaciones.CountAsync(a =>
            a.FechaDevolucion == null && (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa"));
        var enReparacion = await _context.Reparaciones.CountAsync(r =>
            r.EstadoReparacion != Estado.Reparada && r.EstadoReparacion != Estado.Rechazada);
        var bajasAprobadas = await _context.Bajas.CountAsync(b => b.Estado == Estado.Aprobada);
        var totalEquipos = await _context.Tecnologias.CountAsync();
        var disponibles = Math.Max(0, totalEquipos - asignadas - enReparacion - bajasAprobadas);
        var impresionesPorEstado = await ObtenerImpresionesPorEstadoAsync(rol, rutPersonal);

        var resumen = new DashboardResumenDto
        {
            UsuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo),
            TotalPersonal = await _context.Personal.CountAsync(p => p.Activo),
            TotalEquipos = totalEquipos,
            EquiposAsignados = asignadas,
            EquiposDisponibles = disponibles,
            EquiposEnReparacion = enReparacion,
            EquiposDadosDeBaja = bajasAprobadas,
            ReparacionesPendientes = await _context.Reparaciones.CountAsync(r => r.EstadoReparacion == "Pendiente" || r.EstadoReparacion == "Solicitada"),
            BajasPendientes = await _context.Bajas.CountAsync(b => b.Estado == "Pendiente"),
            ImpresionesPendientes = impresionesPorEstado.First(i => i.Label == Estado.Pendiente).Value,
            ImpresionesEnProceso = impresionesPorEstado.First(i => i.Label == Estado.EnProceso).Value,
            ImpresionesEntregadas = impresionesPorEstado.First(i => i.Label == Estado.Entregada).Value,
            ImpresionesRechazadas = impresionesPorEstado.First(i => i.Label == Estado.Rechazada).Value
        };

        if (rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal))
        {
            resumen.EquiposAsignados = await _context.Asignaciones.CountAsync(a =>
                a.RutPersonal == rutPersonal &&
                a.FechaDevolucion == null &&
                (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa"));
        }

        resumen.EquiposPorEstado = new List<ChartItemDto>
        {
            new() { Label = "Disponibles", Value = resumen.EquiposDisponibles },
            new() { Label = "Asignados", Value = resumen.EquiposAsignados },
            new() { Label = "En reparacion", Value = resumen.EquiposEnReparacion },
            new() { Label = "Dados de baja", Value = resumen.EquiposDadosDeBaja }
        };

        resumen.ImpresionesPorEstado = impresionesPorEstado;

        resumen.PersonalPorRol = await _context.Roles
            .GroupJoin(_context.Personal,
                r => r.IdRol,
                p => p.IdRol,
                (r, personal) => new ChartItemDto { Label = r.NombreRol, Value = personal.Count(p => p.Activo) })
            .ToListAsync();

        var desde = DateTime.Today.AddMonths(-5);
        var asignaciones = await _context.Asignaciones.Where(a => a.FechaAsignacion >= desde).ToListAsync();
        var impresiones = await _context.SolicitudesImpresion.Where(i => i.FechaSolicitud >= desde).ToListAsync();
        resumen.MovimientosMensuales = Enumerable.Range(0, 6)
            .Select(i => DateTime.Today.AddMonths(-5 + i))
            .Select(mes => new ChartItemDto
            {
                Label = mes.ToString("MMM yyyy"),
                Value = asignaciones.Count(a => a.FechaAsignacion.Year == mes.Year && a.FechaAsignacion.Month == mes.Month)
                    + impresiones.Count(i => i.FechaSolicitud.Year == mes.Year && i.FechaSolicitud.Month == mes.Month)
            })
            .ToList();

        return resumen;
    }

    public async Task<List<ChartItemDto>> ObtenerImpresionesPorEstadoAsync(string rol, string? rutPersonal)
    {
        var impresionesQuery = _context.SolicitudesImpresion
            .Include(s => s.EstadoImpresion)
            .AsQueryable();

        if (rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal))
            impresionesQuery = impresionesQuery.Where(s => s.RutPersonal == rutPersonal);

        var solicitudes = await impresionesQuery
            .Select(s => new
            {
                Estado = s.EstadoImpresion == null ? string.Empty : s.EstadoImpresion.Estado
            })
            .ToListAsync();

        var conteos = solicitudes
            .GroupBy(i => NormalizarEstadoImpresion(i.Estado))
            .ToDictionary(g => g.Key, g => g.Count());

        return new List<ChartItemDto>
        {
            new() { Label = Estado.Pendiente, Value = ObtenerConteo(conteos, Estado.Pendiente) },
            new() { Label = Estado.EnProceso, Value = ObtenerConteo(conteos, Estado.EnProceso) },
            new() { Label = Estado.Entregada, Value = ObtenerConteo(conteos, Estado.Entregada) },
            new() { Label = Estado.Rechazada, Value = ObtenerConteo(conteos, Estado.Rechazada) }
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
