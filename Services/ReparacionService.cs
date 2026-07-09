namespace SistemaEscolarWeb.Services;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

public class ReparacionService
{
    private readonly ApplicationDbContext _context;
    private readonly TecnologiaService _tecnologiaService;

    public ReparacionService(ApplicationDbContext context, TecnologiaService tecnologiaService)
    {
        _context = context;
        _tecnologiaService = tecnologiaService;
    }

    public async Task<List<ReparacionDto>> ListarAsync(string? busqueda = null)
    {
        var reparaciones = await _context.Reparaciones.OrderByDescending(r => r.FechaEnvio).ToListAsync();
        var resultado = new List<ReparacionDto>();
        foreach (var reparacion in reparaciones)
            resultado.Add(await MapearDtoAsync(reparacion));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            resultado = resultado.Where(r =>
                r.CodigoEquipo.ToLower().Contains(termino) ||
                (r.Destino ?? "").ToLower().Contains(termino) ||
                (r.Detalle ?? "").ToLower().Contains(termino) ||
                r.EstadoReparacion.ToLower().Contains(termino)).ToList();
        }

        return resultado;
    }

    public async Task<CrearReparacionViewModel> CrearFormularioAsync()
    {
        var model = new CrearReparacionViewModel();
        await CargarCombosAsync(model);
        return model;
    }

    public async Task CargarCombosAsync(CrearReparacionViewModel model)
    {
        var equipos = await _context.Tecnologias.Where(t => t.Estado).OrderBy(t => t.SkuCodigoInventario).ToListAsync();
        model.EquiposDisponibles = new List<SelectListItem>();

        foreach (var equipo in equipos)
        {
            var detalle = await _tecnologiaService.ObtenerDetalleAsync(equipo.IdTecnologia);
            if (detalle?.EstadoOperativo == Estado.EquipoDisponible)
            {
                model.EquiposDisponibles.Add(new SelectListItem
                {
                    Value = equipo.IdTecnologia.ToString(),
                    Text = $"{detalle.CodigoInventario} - {detalle.Marca} {detalle.Modelo}"
                });
            }
        }
    }

    public async Task CrearAsync(CrearReparacionViewModel model, string usuario)
    {
        var detalle = await _tecnologiaService.ObtenerDetalleAsync(model.IdTecnologia);
        if (detalle == null)
            throw new InvalidOperationException("El equipo seleccionado no existe.");
        if (detalle.EstadoOperativo != Estado.EquipoDisponible)
            throw new InvalidOperationException("El equipo debe estar disponible para enviarlo a reparacion.");

        _context.Reparaciones.Add(new Reparacion
        {
            IdTecnologia = model.IdTecnologia,
            Destino = model.Destino.Trim(),
            FechaEnvio = DateTime.Now,
            Detalle = model.Observacion?.Trim(),
            EstadoReparacion = Estado.Pendiente,
            UsuarioSolicita = usuario
        });

        await _context.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int id, string estado, string? comentario, string usuario)
    {
        if (!InputValidationHelper.IsSafeText(comentario, 500, required: true))
            throw new InvalidOperationException("Debe ingresar un comentario valido de hasta 500 caracteres.");

        var reparacion = await _context.Reparaciones.FirstOrDefaultAsync(r => r.IdReparacion == id);
        if (reparacion == null)
            throw new InvalidOperationException("La reparacion seleccionada no existe.");

        reparacion.EstadoReparacion = estado;
        if (estado == Estado.Aprobada || estado == Estado.EnReparacion)
            reparacion.UsuarioAprueba = usuario;
        if (estado == Estado.Reparada)
            reparacion.FechaRetorno = DateTime.Now;
        reparacion.Detalle = AnexarComentarioGestion(reparacion.Detalle, estado, comentario!, usuario);

        await _context.SaveChangesAsync();
    }

    private static string AnexarComentarioGestion(string? detalleActual, string estado, string comentario, string usuario)
    {
        var lineaComentario = $"[{DateTime.Now:dd/MM/yyyy HH:mm}] {usuario} - {estado}: {comentario.Trim()}";
        return string.IsNullOrWhiteSpace(detalleActual)
            ? lineaComentario
            : $"{detalleActual.Trim()}{Environment.NewLine}{lineaComentario}";
    }

    private async Task<ReparacionDto> MapearDtoAsync(Reparacion reparacion)
    {
        var equipo = await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == reparacion.IdTecnologia);
        return new ReparacionDto
        {
            IdReparacion = reparacion.IdReparacion,
            IdTecnologia = reparacion.IdTecnologia,
            CodigoEquipo = equipo?.SkuCodigoInventario ?? $"Equipo #{reparacion.IdTecnologia}",
            Destino = reparacion.Destino,
            FechaEnvio = reparacion.FechaEnvio,
            FechaRetorno = reparacion.FechaRetorno,
            Detalle = reparacion.Detalle,
            EstadoReparacion = reparacion.EstadoReparacion,
            UsuarioSolicita = reparacion.UsuarioSolicita,
            UsuarioAprueba = reparacion.UsuarioAprueba
        };
    }
}
