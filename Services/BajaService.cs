namespace SistemaEscolarWeb.Services;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

public class BajaService
{
    private readonly ApplicationDbContext _context;
    private readonly TecnologiaService _tecnologiaService;

    public BajaService(ApplicationDbContext context, TecnologiaService tecnologiaService)
    {
        _context = context;
        _tecnologiaService = tecnologiaService;
    }

    public async Task<List<BajaDto>> ListarAsync(string? busqueda = null)
    {
        var bajas = await _context.Bajas.OrderByDescending(b => b.FechaBaja).ToListAsync();
        var resultado = new List<BajaDto>();
        foreach (var baja in bajas)
            resultado.Add(await MapearDtoAsync(baja));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            resultado = resultado.Where(b =>
                b.CodigoEquipo.ToLower().Contains(termino) ||
                b.Estado.ToLower().Contains(termino) ||
                (b.Detalle ?? "").ToLower().Contains(termino)).ToList();
        }

        return resultado;
    }

    public async Task<CrearBajaViewModel> CrearFormularioAsync()
    {
        var model = new CrearBajaViewModel();
        await CargarCombosAsync(model);
        return model;
    }

    public async Task<CrearBajaViewModel> CrearFormularioAsync(int? idTecnologia)
    {
        var model = new CrearBajaViewModel { IdTecnologia = idTecnologia ?? 0 };
        await CargarCombosAsync(model);
        return model;
    }

    public async Task CargarCombosAsync(CrearBajaViewModel model)
    {
        model.Motivos = new List<SelectListItem>
        {
            new() { Value = "1", Text = "Obsolescencia" },
            new() { Value = "2", Text = "Dano irreparable" },
            new() { Value = "3", Text = "Perdida o extravio" },
            new() { Value = "4", Text = "Robo informado" }
        };

        model.EquiposDisponibles = new List<SelectListItem>();
        var equipos = await _context.Tecnologias.Where(t => t.Estado).OrderBy(t => t.SkuCodigoInventario).ToListAsync();
        foreach (var equipo in equipos)
        {
            var detalle = await _tecnologiaService.ObtenerDetalleAsync(equipo.IdTecnologia);
            if (detalle?.EstadoOperativo != Estado.EquipoDadoDeBaja)
            {
                model.EquiposDisponibles.Add(new SelectListItem
                {
                    Value = equipo.IdTecnologia.ToString(),
                    Text = $"{equipo.SkuCodigoInventario} - {detalle?.EstadoOperativo}",
                    Selected = model.IdTecnologia == equipo.IdTecnologia
                });
            }
        }
    }

    public async Task CrearAsync(CrearBajaViewModel model, string usuario)
    {
        var equipo = await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == model.IdTecnologia);
        if (equipo == null)
            throw new InvalidOperationException("El equipo seleccionado no existe.");

        var tieneBajaPendiente = await _context.Bajas.AnyAsync(b =>
            b.IdTecnologia == model.IdTecnologia && (b.Estado == Estado.Pendiente || b.Estado == Estado.Aprobada));
        if (tieneBajaPendiente)
            throw new InvalidOperationException("El equipo ya tiene una baja pendiente o aprobada.");

        _context.Bajas.Add(new Baja
        {
            IdTecnologia = model.IdTecnologia,
            IdMotivo = model.IdMotivo,
            FechaBaja = DateTime.Now,
            Detalle = model.Observacion?.Trim(),
            UsuarioRegistraBaja = usuario,
            Estado = Estado.Pendiente
        });

        await _context.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int id, string estado, string usuario)
    {
        var baja = await _context.Bajas.FirstOrDefaultAsync(b => b.IdDeBaja == id);
        if (baja == null)
            throw new InvalidOperationException("La baja seleccionada no existe.");

        if (baja.Estado != Estado.Pendiente)
            throw new InvalidOperationException($"La baja ya fue procesada con estado {baja.Estado}.");

        baja.Estado = estado;
        baja.UsuarioAutorizaBaja = usuario;

        if (estado == Estado.Aprobada)
        {
            var equipo = await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == baja.IdTecnologia);
            if (equipo != null)
                equipo.Estado = false;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<BajaDto> MapearDtoAsync(Baja baja)
    {
        var equipo = await _context.Tecnologias.FirstOrDefaultAsync(t => t.IdTecnologia == baja.IdTecnologia);
        return new BajaDto
        {
            IdDeBaja = baja.IdDeBaja,
            IdTecnologia = baja.IdTecnologia,
            CodigoEquipo = equipo?.SkuCodigoInventario ?? $"Equipo #{baja.IdTecnologia}",
            FechaBaja = baja.FechaBaja,
            Detalle = baja.Detalle,
            UsuarioRegistraBaja = baja.UsuarioRegistraBaja,
            UsuarioAutorizaBaja = baja.UsuarioAutorizaBaja,
            Estado = baja.Estado
        };
    }
}
