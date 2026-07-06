using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Repositories;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class AsignacionService
{
    private readonly AsignacionRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly TecnologiaService _tecnologiaService;

    public AsignacionService(AsignacionRepository repository, ApplicationDbContext context, TecnologiaService tecnologiaService)
    {
        _repository = repository;
        _context = context;
        _tecnologiaService = tecnologiaService;
    }

    public async Task<List<AsignacionDto>> ListarAsync(string? rol, string? rutPersonal, string? busqueda = null)
    {
        var asignaciones = rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal)
            ? await _repository.ListarPorPersonaAsync(rutPersonal)
            : await _repository.ListarAsync();

        var resultado = new List<AsignacionDto>();
        foreach (var asignacion in asignaciones)
        {
            resultado.Add(await MapearDtoAsync(asignacion));
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            resultado = resultado.Where(a =>
                a.CodigoEquipo.ToLower().Contains(termino) ||
                a.MarcaEquipo.ToLower().Contains(termino) ||
                a.ModeloEquipo.ToLower().Contains(termino) ||
                a.TipoTecnologia.ToLower().Contains(termino) ||
                a.NombrePersonal.ToLower().Contains(termino) ||
                a.Dependencia.ToLower().Contains(termino) ||
                a.AsignadoA.ToLower().Contains(termino) ||
                a.TipoDestinatario.ToLower().Contains(termino) ||
                a.EstadoAsignacion.ToLower().Contains(termino)).ToList();
        }

        return resultado;
    }

    public async Task<AsignacionDto?> ObtenerDetalleAsync(int id, string? rol, string? rutPersonal)
    {
        var asignacion = await _repository.ObtenerAsync(id);
        if (asignacion == null) return null;

        if (rol == "Profesor" && asignacion.RutPersonal != rutPersonal)
            return null;

        return await MapearDtoAsync(asignacion);
    }

    public async Task<CrearAsignacionViewModel> CrearFormularioAsync()
    {
        var model = new CrearAsignacionViewModel();
        await CargarCombosAsync(model);
        return model;
    }

    public async Task CargarCombosAsync(CrearAsignacionViewModel model)
    {
        var equipos = await _context.Tecnologias
            .Where(t => t.Estado)
            .OrderBy(t => t.SkuCodigoInventario)
            .ToListAsync();

        var disponibles = new List<SelectListItem>();
        foreach (var equipo in equipos)
        {
            var detalle = await _tecnologiaService.ObtenerDetalleAsync(equipo.IdTecnologia);
            if (detalle?.EstadoOperativo == Estado.EquipoDisponible)
            {
                disponibles.Add(new SelectListItem
                {
                    Value = equipo.IdTecnologia.ToString(),
                    Text = $"{detalle.TipoTecnologia} - {detalle.Marca} {detalle.Modelo} ({detalle.CodigoInventario})"
                });
            }
        }

        model.EquiposDisponibles = disponibles;

        model.PersonalDisponible = await _context.Personal
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Apellido)
            .Select(p => new SelectListItem
            {
                Value = p.RutPersonal,
                Text = p.Nombre + " " + p.Apellido + " - " + p.Cargo
            })
            .ToListAsync();

        model.Dependencias = await _context.Dependencias
            .OrderBy(d => d.NombreDependencia)
            .Select(d => new SelectListItem
            {
                Value = d.IdDependencia.ToString(),
                Text = d.NombreDependencia
            })
            .ToListAsync();
    }

    public async Task CrearAsync(CrearAsignacionViewModel model)
    {
        NormalizarDestinatario(model);

        var equipo = await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == model.IdTecnologia);
        if (equipo == null)
            throw new InvalidOperationException("El equipo seleccionado no existe.");

        var detalleEquipo = await _tecnologiaService.ObtenerDetalleAsync(model.IdTecnologia);
        if (detalleEquipo?.EstadoOperativo != Estado.EquipoDisponible)
            throw new InvalidOperationException("El equipo seleccionado no se encuentra disponible para asignación.");

        if (model.TipoDestinatario == "Persona")
        {
            var personaActiva = await _context.Personal.AnyAsync(p => p.RutPersonal == model.RutPersonal && p.Activo);
            if (!personaActiva)
                throw new InvalidOperationException("La persona seleccionada no se encuentra activa.");
        }

        if (model.TipoDestinatario == "Dependencia")
        {
            var dependenciaExiste = await _context.Dependencias.AnyAsync(d => d.IdDependencia == model.IdDependencia);
            if (!dependenciaExiste)
                throw new InvalidOperationException("La dependencia seleccionada no existe.");
        }

        var asignacion = new Asignacion
        {
            IdTecnologia = model.IdTecnologia,
            RutPersonal = model.RutPersonal,
            IdDependencia = model.IdDependencia,
            FechaAsignacion = DateTime.Now,
            FechaDevolucion = null,
            TipoAsignacion = string.IsNullOrWhiteSpace(model.Observacion)
                ? model.TipoAsignacion.Trim()
                : $"{model.TipoAsignacion.Trim()} | {model.Observacion.Trim()}",
            EstadoAsignacion = "Activa"
        };

        await _repository.AgregarAsync(asignacion);
    }

    public async Task RegistrarDevolucionAsync(int id)
    {
        var asignacion = await _repository.ObtenerAsync(id);
        if (asignacion == null)
            throw new InvalidOperationException("La asignación seleccionada no existe.");

        if (asignacion.FechaDevolucion != null || asignacion.EstadoAsignacion == "Finalizada")
            throw new InvalidOperationException("La asignación ya se encuentra finalizada.");

        asignacion.FechaDevolucion = DateTime.Now;
        asignacion.EstadoAsignacion = "Finalizada";
        await _repository.GuardarAsync();
    }

    private async Task<AsignacionDto> MapearDtoAsync(Asignacion asignacion)
    {
        var equipo = await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == asignacion.IdTecnologia);
        var detalleEquipo = equipo == null ? null : await _tecnologiaService.ObtenerDetalleAsync(equipo.IdTecnologia);
        var persona = string.IsNullOrWhiteSpace(asignacion.RutPersonal)
            ? null
            : await _context.Personal.FirstOrDefaultAsync(p => p.RutPersonal == asignacion.RutPersonal);
        var dependencia = asignacion.IdDependencia.HasValue
            ? await _context.Dependencias.FirstOrDefaultAsync(d => d.IdDependencia == asignacion.IdDependencia.Value)
            : null;
        var tipoDestinatario = !string.IsNullOrWhiteSpace(asignacion.RutPersonal) ? "Persona" : "Dependencia";
        var nombrePersonal = persona == null ? asignacion.RutPersonal ?? string.Empty : $"{persona.Nombre} {persona.Apellido}";
        var nombreDependencia = dependencia?.NombreDependencia ?? (asignacion.IdDependencia.HasValue ? $"Dependencia #{asignacion.IdDependencia}" : string.Empty);
        var partesTipoAsignacion = SepararTipoYObservacion(asignacion.TipoAsignacion);

        return new AsignacionDto
        {
            IdAsignaciones = asignacion.IdAsignaciones,
            IdTecnologia = asignacion.IdTecnologia,
            CodigoEquipo = equipo?.SkuCodigoInventario ?? $"Equipo #{asignacion.IdTecnologia}",
            MarcaEquipo = detalleEquipo?.Marca ?? string.Empty,
            ModeloEquipo = detalleEquipo?.Modelo ?? string.Empty,
            TipoTecnologia = detalleEquipo?.TipoTecnologia ?? string.Empty,
            RutPersonal = asignacion.RutPersonal,
            NombrePersonal = nombrePersonal,
            IdDependencia = asignacion.IdDependencia,
            Dependencia = nombreDependencia,
            TipoDestinatario = tipoDestinatario,
            AsignadoA = tipoDestinatario == "Persona" ? nombrePersonal : nombreDependencia,
            FechaAsignacion = asignacion.FechaAsignacion,
            FechaDevolucion = asignacion.FechaDevolucion,
            TipoAsignacion = partesTipoAsignacion.Tipo,
            Observacion = partesTipoAsignacion.Observacion,
            EstadoAsignacion = detalleEquipo?.EstadoOperativo == Estado.EquipoDadoDeBaja
                ? Estado.EquipoDadoDeBaja
                : asignacion.FechaDevolucion == null ? asignacion.EstadoAsignacion : "Finalizada"
        };
    }

    private static void NormalizarDestinatario(CrearAsignacionViewModel model)
    {
        if (model.TipoDestinatario == "Persona")
        {
            model.RutPersonal = model.RutPersonal?.Trim();
            model.IdDependencia = null;
            return;
        }

        if (model.TipoDestinatario == "Dependencia")
        {
            model.RutPersonal = null;
        }
    }

    private static (string Tipo, string? Observacion) SepararTipoYObservacion(string valor)
    {
        var partes = valor.Split(" | ", 2, StringSplitOptions.None);
        return partes.Length == 2 ? (partes[0], partes[1]) : (valor, null);
    }
}
