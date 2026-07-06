using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class MovimientoInsumoService
{
    private readonly ApplicationDbContext _context;

    public MovimientoInsumoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EntradaInsumoDto>> ListarEntradasAsync()
    {
        var entradas = await _context.EntradasInsumo
            .AsNoTracking()
            .OrderByDescending(e => e.FechaEntrega)
            .ThenByDescending(e => e.IdEntradaInsumo)
            .ToListAsync();
        var insumos = await _context.Insumos.AsNoTracking().ToDictionaryAsync(i => i.IdInsumo, i => i.NombreInsumo);
        var proveedores = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.IdProveedor, p => p.NombreProveedor);

        return entradas.Select(e => new EntradaInsumoDto
        {
            IdEntradaInsumo = e.IdEntradaInsumo,
            Insumo = insumos.GetValueOrDefault(e.IdInsumo, $"Insumo #{e.IdInsumo}"),
            Proveedor = proveedores.GetValueOrDefault(e.IdProveedor, $"Proveedor #{e.IdProveedor}"),
            NumeroFactura = e.NumeroFactura,
            FechaEntrega = e.FechaEntrega,
            Cantidad = e.Cantidad
        }).ToList();
    }

    public async Task<List<SalidaInsumoDto>> ListarSalidasAsync()
    {
        var salidas = await _context.SalidasInsumo
            .AsNoTracking()
            .OrderByDescending(s => s.FechaSalida)
            .ThenByDescending(s => s.IdSalidaInsumo)
            .ToListAsync();
        var insumos = await _context.Insumos.AsNoTracking().ToDictionaryAsync(i => i.IdInsumo, i => i.NombreInsumo);
        var dependencias = await _context.Dependencias.AsNoTracking().ToDictionaryAsync(d => d.IdDependencia, d => d.NombreDependencia);
        var personal = await _context.Personal.AsNoTracking().ToDictionaryAsync(p => p.RutPersonal, p => $"{p.Nombre} {p.Apellido}");

        return salidas.Select(s => new SalidaInsumoDto
        {
            IdSalidaInsumo = s.IdSalidaInsumo,
            Insumo = insumos.GetValueOrDefault(s.IdInsumo, $"Insumo #{s.IdInsumo}"),
            Dependencia = dependencias.GetValueOrDefault(s.IdDependencia, $"Dependencia #{s.IdDependencia}"),
            Responsable = personal.GetValueOrDefault(s.RutPersonal, s.RutPersonal),
            Cantidad = s.Cantidad,
            FechaSalida = s.FechaSalida
        }).ToList();
    }

    public async Task<CrearEntradaInsumoViewModel> CrearFormularioEntradaAsync()
    {
        var model = new CrearEntradaInsumoViewModel();
        await CargarCombosAsync(model);
        return model;
    }

    public async Task<CrearEntradaInsumoViewModel?> ObtenerFormularioEntradaAsync(int id)
    {
        var entrada = await _context.EntradasInsumo.AsNoTracking().FirstOrDefaultAsync(e => e.IdEntradaInsumo == id);
        if (entrada == null)
            return null;

        var proveedor = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.IdProveedor == entrada.IdProveedor);
        var model = new CrearEntradaInsumoViewModel
        {
            IdEntradaInsumo = entrada.IdEntradaInsumo,
            IdInsumo = entrada.IdInsumo,
            IdProveedor = entrada.IdProveedor,
            NombreProveedor = proveedor?.NombreProveedor ?? string.Empty,
            NumeroFactura = entrada.NumeroFactura,
            FechaEntrega = entrada.FechaEntrega,
            Cantidad = entrada.Cantidad
        };
        await CargarCombosAsync(model);
        return model;
    }

    public async Task<List<SelectListItem>> ObtenerTiposInsumoAsync()
    {
        return await _context.TiposInsumo.AsNoTracking()
            .Where(t => t.NombreTipoInsumo == "Material de aseo" ||
                t.NombreTipoInsumo == "Libreria" ||
                t.NombreTipoInsumo == "Librería" ||
                t.NombreTipoInsumo == "LibrerÃ­a")
            .OrderBy(t => t.NombreTipoInsumo)
            .Select(t => new SelectListItem { Value = t.IdTipoInsumo.ToString(), Text = t.NombreTipoInsumo })
            .ToListAsync();
    }

    public Task<bool> EsTipoInsumoPermitidoAsync(int idTipoInsumo)
    {
        return _context.TiposInsumo.AnyAsync(t => t.IdTipoInsumo == idTipoInsumo &&
            (t.NombreTipoInsumo == "Material de aseo" ||
             t.NombreTipoInsumo == "Libreria" ||
             t.NombreTipoInsumo == "Librería" ||
             t.NombreTipoInsumo == "LibrerÃ­a"));
    }

    public async Task<CrearSalidaInsumoViewModel> CrearFormularioSalidaAsync()
    {
        var model = new CrearSalidaInsumoViewModel();
        await CargarCombosAsync(model);
        return model;
    }

    public async Task<CrearSalidaInsumoViewModel?> ObtenerFormularioSalidaAsync(int id)
    {
        var salida = await _context.SalidasInsumo.AsNoTracking().FirstOrDefaultAsync(s => s.IdSalidaInsumo == id);
        if (salida == null)
            return null;

        var model = new CrearSalidaInsumoViewModel
        {
            IdSalidaInsumo = salida.IdSalidaInsumo,
            IdInsumo = salida.IdInsumo,
            IdDependencia = salida.IdDependencia,
            RutPersonal = salida.RutPersonal,
            Cantidad = salida.Cantidad,
            FechaSalida = salida.FechaSalida
        };
        await CargarCombosAsync(model);
        return model;
    }

    public async Task CargarCombosAsync(CrearEntradaInsumoViewModel model)
    {
        model.Insumos = await ObtenerInsumosAsync();
        model.Proveedores = await ObtenerProveedoresAsync(model.IdProveedor);
    }

    public async Task CargarCombosAsync(CrearSalidaInsumoViewModel model)
    {
        model.Insumos = await ObtenerInsumosAsync();
        model.Dependencias = await _context.Dependencias.AsNoTracking()
            .OrderBy(d => d.NombreDependencia)
            .Select(d => new SelectListItem { Value = d.IdDependencia.ToString(), Text = d.NombreDependencia })
            .ToListAsync();
        model.Personal = await _context.Personal.AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Apellido)
            .Select(p => new SelectListItem { Value = p.RutPersonal, Text = p.Nombre + " " + p.Apellido + " - " + p.Cargo })
            .ToListAsync();
    }

    public async Task RegistrarEntradaAsync(CrearEntradaInsumoViewModel model)
    {
        if (model.Cantidad < 1 || model.Cantidad > 100000)
            throw new InvalidOperationException("La cantidad debe estar entre 1 y 100000.");

        var insumoExiste = await _context.Insumos.AnyAsync(i => i.IdInsumo == model.IdInsumo && i.Estado);
        if (!insumoExiste)
            throw new InvalidOperationException("El insumo debe estar activo.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.EntradasInsumo.Add(new EntradaInsumo
        {
            IdInsumo = model.IdInsumo!.Value,
            IdProveedor = model.IdProveedor!.Value,
            NumeroFactura = model.NumeroFactura.Trim(),
            FechaEntrega = model.FechaEntrega.Date,
            Cantidad = model.Cantidad
        });
        await _context.SaveChangesAsync();

        await _context.Insumos
            .Where(i => i.IdInsumo == model.IdInsumo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual + model.Cantidad));

        await transaction.CommitAsync();
    }

    public async Task ActualizarEntradaAsync(CrearEntradaInsumoViewModel model)
    {
        if (!model.IdEntradaInsumo.HasValue)
            throw new InvalidOperationException("La entrada no existe.");
        if (model.Cantidad < 1 || model.Cantidad > 100000)
            throw new InvalidOperationException("La cantidad debe estar entre 1 y 100000.");

        var entrada = await _context.EntradasInsumo.FirstOrDefaultAsync(e => e.IdEntradaInsumo == model.IdEntradaInsumo.Value);
        if (entrada == null)
            throw new InvalidOperationException("La entrada no existe.");

        var insumoExiste = await _context.Insumos.AnyAsync(i => i.IdInsumo == model.IdInsumo && i.Estado);
        if (!insumoExiste)
            throw new InvalidOperationException("El insumo debe estar activo.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Insumos
            .Where(i => i.IdInsumo == entrada.IdInsumo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual - entrada.Cantidad));
        await _context.Insumos
            .Where(i => i.IdInsumo == model.IdInsumo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual + model.Cantidad));

        entrada.IdInsumo = model.IdInsumo!.Value;
        entrada.IdProveedor = model.IdProveedor!.Value;
        entrada.NumeroFactura = model.NumeroFactura.Trim();
        entrada.FechaEntrega = model.FechaEntrega.Date;
        entrada.Cantidad = model.Cantidad;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task EliminarEntradaAsync(int id)
    {
        var entrada = await _context.EntradasInsumo.FirstOrDefaultAsync(e => e.IdEntradaInsumo == id);
        if (entrada == null)
            throw new InvalidOperationException("La entrada no existe.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Insumos
            .Where(i => i.IdInsumo == entrada.IdInsumo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual - entrada.Cantidad));
        _context.EntradasInsumo.Remove(entrada);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RegistrarSalidaAsync(CrearSalidaInsumoViewModel model)
    {
        if (model.Cantidad < 1 || model.Cantidad > 100000)
            throw new InvalidOperationException("La cantidad debe estar entre 1 y 100000.");

        var dependenciaExiste = await _context.Dependencias.AnyAsync(d => d.IdDependencia == model.IdDependencia);
        var personalExiste = await _context.Personal.AnyAsync(p => p.RutPersonal == model.RutPersonal && p.Activo);
        if (!dependenciaExiste || !personalExiste)
            throw new InvalidOperationException("La dependencia o persona responsable ya no existe o no esta activa.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var stockActualizado = await _context.Insumos
            .Where(i => i.IdInsumo == model.IdInsumo && i.Estado && i.StockActual >= model.Cantidad)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual - model.Cantidad));

        if (stockActualizado == 0)
            throw new InvalidOperationException("El insumo no existe, esta inactivo o no tiene stock suficiente.");

        _context.SalidasInsumo.Add(new SalidaInsumo
        {
            IdInsumo = model.IdInsumo!.Value,
            IdDependencia = model.IdDependencia!.Value,
            RutPersonal = model.RutPersonal,
            Cantidad = model.Cantidad,
            FechaSalida = model.FechaSalida.Date
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ActualizarSalidaAsync(CrearSalidaInsumoViewModel model)
    {
        if (!model.IdSalidaInsumo.HasValue)
            throw new InvalidOperationException("La salida no existe.");

        var salida = await _context.SalidasInsumo.FirstOrDefaultAsync(s => s.IdSalidaInsumo == model.IdSalidaInsumo.Value);
        if (salida == null)
            throw new InvalidOperationException("La salida no existe.");

        var dependenciaExiste = await _context.Dependencias.AnyAsync(d => d.IdDependencia == model.IdDependencia);
        var personalExiste = await _context.Personal.AnyAsync(p => p.RutPersonal == model.RutPersonal && p.Activo);
        if (!dependenciaExiste || !personalExiste)
            throw new InvalidOperationException("La dependencia o persona responsable ya no existe o no esta activa.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Insumos
            .Where(i => i.IdInsumo == salida.IdInsumo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual + salida.Cantidad));

        var stockActualizado = await _context.Insumos
            .Where(i => i.IdInsumo == model.IdInsumo && i.Estado && i.StockActual >= model.Cantidad)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual - model.Cantidad));
        if (stockActualizado == 0)
            throw new InvalidOperationException("El insumo no existe, esta inactivo o no tiene stock suficiente.");

        salida.IdInsumo = model.IdInsumo!.Value;
        salida.IdDependencia = model.IdDependencia!.Value;
        salida.RutPersonal = model.RutPersonal;
        salida.Cantidad = model.Cantidad;
        salida.FechaSalida = model.FechaSalida.Date;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task EliminarSalidaAsync(int id)
    {
        var salida = await _context.SalidasInsumo.FirstOrDefaultAsync(s => s.IdSalidaInsumo == id);
        if (salida == null)
            throw new InvalidOperationException("La salida no existe.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Insumos
            .Where(i => i.IdInsumo == salida.IdInsumo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockActual, i => i.StockActual + salida.Cantidad));
        _context.SalidasInsumo.Remove(salida);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task<List<SelectListItem>> ObtenerInsumosAsync()
    {
        return await _context.Insumos.AsNoTracking()
            .Where(i => i.Estado)
            .OrderBy(i => i.NombreInsumo)
            .Select(i => new SelectListItem
            {
                Value = i.IdInsumo.ToString(),
                Text = i.NombreInsumo
            })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> ObtenerProveedoresAsync(int? seleccionado = null)
    {
        return await _context.Proveedores.AsNoTracking()
            .OrderBy(p => p.NombreProveedor)
            .Select(p => new SelectListItem
            {
                Value = p.IdProveedor.ToString(),
                Text = p.NombreProveedor + " - " + p.RutProveedor,
                Selected = seleccionado == p.IdProveedor
            })
            .ToListAsync();
    }
}
