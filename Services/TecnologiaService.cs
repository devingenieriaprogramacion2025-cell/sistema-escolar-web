using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Repositories;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class TecnologiaService
{
    private readonly TecnologiaRepository _repository;
    private readonly ApplicationDbContext _context;

    public TecnologiaService(TecnologiaRepository repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<EquipoDto>> ListarAsync(string? busqueda = null)
    {
        var equipos = await _repository.ListarAsync();

        var resultado = new List<EquipoDto>();
        foreach (var equipo in equipos)
        {
            resultado.Add(await MapearDtoAsync(equipo));
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            resultado = resultado.Where(t =>
                t.CodigoInventario.ToLower().Contains(termino) ||
                t.Marca.ToLower().Contains(termino) ||
                t.Modelo.ToLower().Contains(termino) ||
                t.Proveedor.ToLower().Contains(termino) ||
                t.TipoTecnologia.ToLower().Contains(termino) ||
                (t.NumeroFactura ?? "").ToLower().Contains(termino) ||
                (t.Descripcion ?? "").ToLower().Contains(termino)).ToList();
        }

        return resultado;
    }

    public async Task<EquipoDto?> ObtenerDetalleAsync(int id)
    {
        var equipo = await _repository.ObtenerAsync(id);
        return equipo == null ? null : await MapearDtoAsync(equipo);
    }

    public async Task<CrearEquipoViewModel?> ObtenerFormularioAsync(int id)
    {
        var equipo = await _repository.ObtenerAsync(id);
        if (equipo == null) return null;

        return new CrearEquipoViewModel
        {
            IdTecnologia = equipo.IdTecnologia,
            IdModelo = equipo.IdModelo,
            IdTipoTecnologia = equipo.IdTipoTecnologia,
            IdEntradaTecnologia = equipo.IdEntradaTecnologia,
            Estado = equipo.Estado,
            SkuCodigoInventario = equipo.SkuCodigoInventario,
            Marca = await ObtenerNombreMarcaAsync(equipo.IdModelo),
            Modelo = await ObtenerNombreModeloAsync(equipo.IdModelo),
            TipoTecnologia = await ObtenerNombreTipoAsync(equipo.IdTipoTecnologia),
            Descripcion = await ObtenerDescripcionTipoAsync(equipo.IdTipoTecnologia),
            Proveedor = await ObtenerProveedorEntradaAsync(equipo.IdEntradaTecnologia),
            FechaEntrada = await ObtenerFechaEntradaAsync(equipo.IdEntradaTecnologia),
            Cantidad = await ObtenerCantidadEntradaAsync(equipo.IdEntradaTecnologia),
            NumeroFactura = await ObtenerFacturaEntradaAsync(equipo.IdEntradaTecnologia)
        };
    }

    public async Task<TecnologiaFormViewModel?> ObtenerFormularioTecnologiaAsync(int id)
    {
        var equipo = await _repository.ObtenerAsync(id);
        if (equipo == null) return null;

        return new TecnologiaFormViewModel
        {
            IdTecnologia = equipo.IdTecnologia,
            SkuCodigoInventario = equipo.SkuCodigoInventario,
            Marca = await ObtenerNombreMarcaAsync(equipo.IdModelo),
            Modelo = await ObtenerNombreModeloAsync(equipo.IdModelo),
            IdEntradaTecnologia = equipo.IdEntradaTecnologia,
            TipoTecnologia = await ObtenerNombreTipoAsync(equipo.IdTipoTecnologia),
            Descripcion = await ObtenerDescripcionTipoAsync(equipo.IdTipoTecnologia),
            Estado = equipo.Estado
        };
    }

    public async Task<List<EntradaTecnologiaDto>> ListarEntradasTecnologiaAsync()
    {
        var entradas = await _context.EntradasTecnologia.AsNoTracking()
            .OrderByDescending(e => e.FechaEntrada)
            .ThenByDescending(e => e.IdEntradaTecnologia)
            .ToListAsync();
        var proveedores = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.IdProveedor, p => p.NombreProveedor);
        var tecnologias = await _context.Tecnologias.AsNoTracking()
            .Where(t => t.IdEntradaTecnologia.HasValue)
            .ToListAsync();
        var modelos = await _context.Modelos.AsNoTracking().ToDictionaryAsync(m => m.IdModelo);
        var marcas = await _context.Marcas.AsNoTracking().ToDictionaryAsync(m => m.IdMarca);
        var tipos = await _context.TiposTecnologia.AsNoTracking().ToDictionaryAsync(t => t.IdTipoTecnologia);

        return entradas.Select(e => new EntradaTecnologiaDto
        {
            IdEntradaTecnologia = e.IdEntradaTecnologia,
            Proveedor = proveedores.GetValueOrDefault(e.IdProveedor, $"Proveedor #{e.IdProveedor}"),
            Marca = ObtenerMarcaDesdeTecnologias(tecnologias, modelos, marcas, e.IdEntradaTecnologia),
            Modelo = ObtenerModeloDesdeTecnologias(tecnologias, modelos, e.IdEntradaTecnologia),
            TipoTecnologia = ObtenerTipoDesdeTecnologias(tecnologias, tipos, e.IdEntradaTecnologia),
            SkuGenerados = ObtenerSkuDesdeTecnologias(tecnologias, e.IdEntradaTecnologia),
            FechaEntrada = e.FechaEntrada,
            Cantidad = e.Cantidad,
            NumeroFactura = e.NumeroFactura
        }).ToList();
    }

    public async Task<List<SelectListItem>> ObtenerEntradasSelectAsync()
    {
        var entradas = await ListarEntradasTecnologiaAsync();
        return entradas.Select(e => new SelectListItem
        {
            Value = e.IdEntradaTecnologia.ToString(),
            Text = $"{e.FechaEntrada:dd/MM/yyyy} - {e.Proveedor} - Factura {e.NumeroFactura}"
        }).ToList();
    }

    public async Task<List<SelectListItem>> ObtenerTecnologiasSelectAsync()
    {
        var equipos = await ListarAsync();
        return equipos
            .Where(e => e.PuedeEditar)
            .Select(e => new SelectListItem
            {
                Value = e.IdTecnologia.ToString(),
                Text = $"{e.CodigoInventario} - {e.Marca} {e.Modelo}"
            })
            .ToList();
    }

    public async Task<EntradaTecnologiaFormViewModel?> ObtenerFormularioEntradaTecnologiaAsync(int id)
    {
        var entrada = await _context.EntradasTecnologia.AsNoTracking().FirstOrDefaultAsync(e => e.IdEntradaTecnologia == id);
        if (entrada == null) return null;
        var proveedor = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.IdProveedor == entrada.IdProveedor);

        return new EntradaTecnologiaFormViewModel
        {
            IdEntradaTecnologia = entrada.IdEntradaTecnologia,
            IdProveedor = entrada.IdProveedor,
            Proveedor = proveedor?.NombreProveedor ?? string.Empty,
            Marca = await ObtenerMarcaEntradaAsync(entrada.IdEntradaTecnologia),
            Modelo = await ObtenerModeloEntradaAsync(entrada.IdEntradaTecnologia),
            TipoTecnologia = await ObtenerTipoEntradaAsync(entrada.IdEntradaTecnologia),
            Descripcion = await ObtenerDescripcionEntradaAsync(entrada.IdEntradaTecnologia),
            PrefijoSku = await ObtenerPrefijoEntradaAsync(entrada.IdEntradaTecnologia),
            FechaEntrada = entrada.FechaEntrada,
            Cantidad = entrada.Cantidad,
            NumeroFactura = entrada.NumeroFactura,
            Proveedores = await ObtenerProveedoresSelectAsync(entrada.IdProveedor)
        };
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null)
        => await _repository.ExisteCodigoAsync(codigo, idExcluir);

    public async Task CrearAsync(CrearEquipoViewModel model)
    {
        var idModelo = await ObtenerOCrearModeloAsync(model.Marca, model.Modelo);
        var idTipo = await ObtenerOCrearTipoAsync(model.TipoTecnologia, model.Descripcion);
        var idEntrada = await CrearEntradaAsync(model);

        var tecnologia = new Tecnologia
        {
            IdModelo = idModelo,
            IdTipoTecnologia = idTipo,
            IdEntradaTecnologia = idEntrada,
            Estado = model.Estado,
            SkuCodigoInventario = model.SkuCodigoInventario.Trim()
        };

        await _repository.AgregarAsync(tecnologia);
    }

    public async Task CrearTecnologiaAsync(TecnologiaFormViewModel model)
    {
        var tecnologia = new Tecnologia
        {
            IdModelo = await ObtenerOCrearModeloAsync(model.Marca, model.Modelo),
            IdTipoTecnologia = await ObtenerOCrearTipoAsync(model.TipoTecnologia, model.Descripcion),
            IdEntradaTecnologia = model.IdEntradaTecnologia,
            Estado = model.Estado,
            SkuCodigoInventario = model.SkuCodigoInventario.Trim()
        };

        await _repository.AgregarAsync(tecnologia);
    }

    public async Task ActualizarTecnologiaAsync(TecnologiaFormViewModel model)
    {
        if (!model.IdTecnologia.HasValue)
            throw new InvalidOperationException("No se encontro el equipo a actualizar.");

        var tecnologia = await _repository.ObtenerAsync(model.IdTecnologia.Value);
        if (tecnologia == null)
            throw new InvalidOperationException("El equipo seleccionado no existe.");

        tecnologia.IdModelo = await ObtenerOCrearModeloAsync(model.Marca, model.Modelo);
        tecnologia.IdTipoTecnologia = await ObtenerOCrearTipoAsync(model.TipoTecnologia, model.Descripcion);
        tecnologia.IdEntradaTecnologia = model.IdEntradaTecnologia;
        tecnologia.Estado = model.Estado;
        tecnologia.SkuCodigoInventario = model.SkuCodigoInventario.Trim();
        await _repository.GuardarAsync();
    }

    public async Task CrearEntradaTecnologiaAsync(EntradaTecnologiaFormViewModel model)
    {
        if (model.Cantidad <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(model.PrefijoSku))
            throw new InvalidOperationException("Debe ingresar el prefijo SKU.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var entrada = new EntradaTecnologia
        {
            IdProveedor = model.IdProveedor!.Value,
            FechaEntrada = model.FechaEntrada.Date,
            Cantidad = model.Cantidad,
            NumeroFactura = model.NumeroFactura.Trim()
        };
        _context.EntradasTecnologia.Add(entrada);
        await _context.SaveChangesAsync();

        await CrearUnidadesTecnologiaAsync(model, entrada.IdEntradaTecnologia);
        await transaction.CommitAsync();
    }

    public async Task ActualizarEntradaTecnologiaAsync(EntradaTecnologiaFormViewModel model)
    {
        if (!model.IdEntradaTecnologia.HasValue)
            throw new InvalidOperationException("La entrada tecnologica no existe.");

        var entrada = await _context.EntradasTecnologia.FirstOrDefaultAsync(e => e.IdEntradaTecnologia == model.IdEntradaTecnologia.Value);
        if (entrada == null)
            throw new InvalidOperationException("La entrada tecnologica no existe.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        entrada.IdProveedor = model.IdProveedor!.Value;
        entrada.FechaEntrada = model.FechaEntrada.Date;
        entrada.NumeroFactura = model.NumeroFactura.Trim();

        var unidades = await _context.Tecnologias.Where(t => t.IdEntradaTecnologia == entrada.IdEntradaTecnologia).ToListAsync();
        if (unidades.Count != model.Cantidad)
            throw new InvalidOperationException("No se puede cambiar la cantidad de una entrada que ya genero unidades. Cree una nueva entrada para agregar mas unidades.");

        var idModelo = await ObtenerOCrearModeloAsync(model.Marca, model.Modelo);
        var idTipo = await ObtenerOCrearTipoAsync(model.TipoTecnologia, model.Descripcion);
        foreach (var unidad in unidades)
        {
            unidad.IdModelo = idModelo;
            unidad.IdTipoTecnologia = idTipo;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ActualizarAsync(CrearEquipoViewModel model)
    {
        if (!model.IdTecnologia.HasValue)
            throw new InvalidOperationException("No se encontró el equipo a actualizar.");

        var tecnologia = await _repository.ObtenerAsync(model.IdTecnologia.Value);
        if (tecnologia == null)
            throw new InvalidOperationException("El equipo seleccionado no existe.");

        var estadoOperativo = await ObtenerEstadoOperativoAsync(tecnologia);
        if (estadoOperativo == Estado.EquipoDadoDeBaja)
            throw new InvalidOperationException("No se puede editar un equipo dado de baja.");

        tecnologia.IdModelo = await ObtenerOCrearModeloAsync(model.Marca, model.Modelo);
        tecnologia.IdTipoTecnologia = await ObtenerOCrearTipoAsync(model.TipoTecnologia, model.Descripcion);
        tecnologia.IdEntradaTecnologia = await ObtenerOActualizarEntradaAsync(tecnologia.IdEntradaTecnologia, model);
        tecnologia.Estado = model.Estado;
        tecnologia.SkuCodigoInventario = model.SkuCodigoInventario.Trim();

        await _repository.GuardarAsync();
    }

    private async Task<EquipoDto> MapearDtoAsync(Tecnologia equipo)
    {
        return new EquipoDto
        {
            IdTecnologia = equipo.IdTecnologia,
            CodigoInventario = equipo.SkuCodigoInventario,
            IdModelo = equipo.IdModelo,
            IdTipoTecnologia = equipo.IdTipoTecnologia,
            IdEntradaTecnologia = equipo.IdEntradaTecnologia,
            Marca = await ObtenerNombreMarcaAsync(equipo.IdModelo),
            Modelo = await ObtenerNombreModeloAsync(equipo.IdModelo),
            TipoTecnologia = await ObtenerNombreTipoAsync(equipo.IdTipoTecnologia),
            Descripcion = await ObtenerDescripcionTipoAsync(equipo.IdTipoTecnologia),
            Proveedor = await ObtenerProveedorEntradaAsync(equipo.IdEntradaTecnologia),
            FechaEntrada = await ObtenerFechaEntradaAsync(equipo.IdEntradaTecnologia),
            Cantidad = await ObtenerCantidadEntradaAsync(equipo.IdEntradaTecnologia),
            NumeroFactura = await ObtenerFacturaEntradaAsync(equipo.IdEntradaTecnologia),
            Activo = equipo.Estado,
            EstadoOperativo = await ObtenerEstadoOperativoAsync(equipo)
        };
    }

    private async Task<int> ObtenerOCrearMarcaAsync(string nombre)
    {
        nombre = nombre.Trim();
        var marca = await _context.Marcas.FirstOrDefaultAsync(m => m.NombreMarca == nombre);
        if (marca != null) return marca.IdMarca;

        marca = new Marca { NombreMarca = nombre };
        _context.Marcas.Add(marca);
        await _context.SaveChangesAsync();
        return marca.IdMarca;
    }

    private async Task<int> ObtenerOCrearModeloAsync(string marcaNombre, string modeloNombre)
    {
        var idMarca = await ObtenerOCrearMarcaAsync(marcaNombre);
        modeloNombre = modeloNombre.Trim();
        var modelo = await _context.Modelos.FirstOrDefaultAsync(m => m.IdMarca == idMarca && m.NombreModelo == modeloNombre);
        if (modelo != null) return modelo.IdModelo;

        modelo = new Modelo { IdMarca = idMarca, NombreModelo = modeloNombre };
        _context.Modelos.Add(modelo);
        await _context.SaveChangesAsync();
        return modelo.IdModelo;
    }

    private async Task<int> ObtenerOCrearTipoAsync(string nombre, string? descripcion)
    {
        nombre = nombre.Trim();
        descripcion = descripcion?.Trim() ?? string.Empty;
        var tipo = await _context.TiposTecnologia.FirstOrDefaultAsync(t => t.NombreTipoTecnologia == nombre);
        if (tipo != null)
        {
            tipo.Descripcion = descripcion;
            await _context.SaveChangesAsync();
            return tipo.IdTipoTecnologia;
        }

        tipo = new TipoTecnologia { NombreTipoTecnologia = nombre, Descripcion = descripcion };
        _context.TiposTecnologia.Add(tipo);
        await _context.SaveChangesAsync();
        return tipo.IdTipoTecnologia;
    }

    private async Task CrearUnidadesTecnologiaAsync(EntradaTecnologiaFormViewModel model, int idEntradaTecnologia)
    {
        var idModelo = await ObtenerOCrearModeloAsync(model.Marca, model.Modelo);
        var idTipo = await ObtenerOCrearTipoAsync(model.TipoTecnologia, model.Descripcion);
        var prefijo = model.PrefijoSku.Trim().ToUpperInvariant();
        var inicio = await ObtenerSiguienteNumeroSkuAsync(prefijo);

        for (var i = 0; i < model.Cantidad; i++)
        {
            var sku = $"{prefijo}-{inicio + i:000}";
            if (await _context.Tecnologias.AnyAsync(t => t.SkuCodigoInventario == sku))
                throw new InvalidOperationException($"El SKU generado {sku} ya existe.");

            _context.Tecnologias.Add(new Tecnologia
            {
                IdModelo = idModelo,
                IdTipoTecnologia = idTipo,
                IdEntradaTecnologia = idEntradaTecnologia,
                Estado = true,
                SkuCodigoInventario = sku
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<int> ObtenerSiguienteNumeroSkuAsync(string prefijo)
    {
        var skuExistentes = await _context.Tecnologias.AsNoTracking()
            .Where(t => t.SkuCodigoInventario.StartsWith(prefijo + "-"))
            .Select(t => t.SkuCodigoInventario)
            .ToListAsync();

        var maximo = 0;
        foreach (var sku in skuExistentes)
        {
            var sufijo = sku[(prefijo.Length + 1)..];
            if (int.TryParse(sufijo, out var numero) && numero > maximo)
                maximo = numero;
        }

        return maximo + 1;
    }

    private static string ObtenerMarcaDesdeTecnologias(
        List<Tecnologia> tecnologias,
        Dictionary<int, Modelo> modelos,
        Dictionary<int, Marca> marcas,
        int idEntradaTecnologia)
    {
        var tecnologia = tecnologias.FirstOrDefault(t => t.IdEntradaTecnologia == idEntradaTecnologia);
        if (tecnologia == null || !modelos.TryGetValue(tecnologia.IdModelo, out var modelo))
            return string.Empty;

        return marcas.GetValueOrDefault(modelo.IdMarca)?.NombreMarca ?? string.Empty;
    }

    private static string ObtenerModeloDesdeTecnologias(
        List<Tecnologia> tecnologias,
        Dictionary<int, Modelo> modelos,
        int idEntradaTecnologia)
    {
        var tecnologia = tecnologias.FirstOrDefault(t => t.IdEntradaTecnologia == idEntradaTecnologia);
        return tecnologia != null && modelos.TryGetValue(tecnologia.IdModelo, out var modelo)
            ? modelo.NombreModelo
            : string.Empty;
    }

    private static string ObtenerTipoDesdeTecnologias(
        List<Tecnologia> tecnologias,
        Dictionary<int, TipoTecnologia> tipos,
        int idEntradaTecnologia)
    {
        var tecnologia = tecnologias.FirstOrDefault(t => t.IdEntradaTecnologia == idEntradaTecnologia);
        return tecnologia != null && tipos.TryGetValue(tecnologia.IdTipoTecnologia, out var tipo)
            ? tipo.NombreTipoTecnologia
            : string.Empty;
    }

    private static string ObtenerSkuDesdeTecnologias(List<Tecnologia> tecnologias, int idEntradaTecnologia)
    {
        var skus = tecnologias
            .Where(t => t.IdEntradaTecnologia == idEntradaTecnologia)
            .OrderBy(t => t.SkuCodigoInventario)
            .Select(t => t.SkuCodigoInventario)
            .ToList();

        if (skus.Count == 0) return string.Empty;
        if (skus.Count == 1) return skus[0];
        return $"{skus.First()} a {skus.Last()}";
    }

    private async Task<Tecnologia?> ObtenerPrimeraTecnologiaEntradaAsync(int idEntradaTecnologia)
    {
        return await _context.Tecnologias.AsNoTracking()
            .Where(t => t.IdEntradaTecnologia == idEntradaTecnologia)
            .OrderBy(t => t.SkuCodigoInventario)
            .FirstOrDefaultAsync();
    }

    private async Task<string> ObtenerMarcaEntradaAsync(int idEntradaTecnologia)
    {
        var tecnologia = await ObtenerPrimeraTecnologiaEntradaAsync(idEntradaTecnologia);
        return tecnologia == null ? string.Empty : await ObtenerNombreMarcaAsync(tecnologia.IdModelo);
    }

    private async Task<string> ObtenerModeloEntradaAsync(int idEntradaTecnologia)
    {
        var tecnologia = await ObtenerPrimeraTecnologiaEntradaAsync(idEntradaTecnologia);
        return tecnologia == null ? string.Empty : await ObtenerNombreModeloAsync(tecnologia.IdModelo);
    }

    private async Task<string> ObtenerTipoEntradaAsync(int idEntradaTecnologia)
    {
        var tecnologia = await ObtenerPrimeraTecnologiaEntradaAsync(idEntradaTecnologia);
        return tecnologia == null ? string.Empty : await ObtenerNombreTipoAsync(tecnologia.IdTipoTecnologia);
    }

    private async Task<string> ObtenerDescripcionEntradaAsync(int idEntradaTecnologia)
    {
        var tecnologia = await ObtenerPrimeraTecnologiaEntradaAsync(idEntradaTecnologia);
        return tecnologia == null ? string.Empty : await ObtenerDescripcionTipoAsync(tecnologia.IdTipoTecnologia);
    }

    private async Task<string> ObtenerPrefijoEntradaAsync(int idEntradaTecnologia)
    {
        var tecnologia = await ObtenerPrimeraTecnologiaEntradaAsync(idEntradaTecnologia);
        if (tecnologia == null) return string.Empty;
        var index = tecnologia.SkuCodigoInventario.LastIndexOf('-');
        return index > 0 ? tecnologia.SkuCodigoInventario[..index] : tecnologia.SkuCodigoInventario;
    }

    private async Task<int> CrearEntradaAsync(CrearEquipoViewModel model)
    {
        var entrada = new EntradaTecnologia
        {
            IdProveedor = await ObtenerProveedorPorNombreAsync(model.Proveedor),
            FechaEntrada = model.FechaEntrada.Date,
            Cantidad = model.Cantidad,
            NumeroFactura = model.NumeroFactura.Trim()
        };
        _context.EntradasTecnologia.Add(entrada);
        await _context.SaveChangesAsync();
        return entrada.IdEntradaTecnologia;
    }

    private async Task<int> ObtenerOActualizarEntradaAsync(int? idEntrada, CrearEquipoViewModel model)
    {
        if (idEntrada.HasValue)
        {
            var entrada = await _context.EntradasTecnologia.FirstOrDefaultAsync(e => e.IdEntradaTecnologia == idEntrada.Value);
            if (entrada != null)
            {
                entrada.IdProveedor = await ObtenerProveedorPorNombreAsync(model.Proveedor);
                entrada.FechaEntrada = model.FechaEntrada.Date;
                entrada.Cantidad = model.Cantidad;
                entrada.NumeroFactura = model.NumeroFactura.Trim();
                await _context.SaveChangesAsync();
                return entrada.IdEntradaTecnologia;
            }
        }

        return await CrearEntradaAsync(model);
    }

    private async Task<string> ObtenerNombreMarcaAsync(int idModelo)
    {
        var modelo = await _context.Modelos.FirstOrDefaultAsync(m => m.IdModelo == idModelo);
        if (modelo == null) return $"Marca #{idModelo}";
        var marca = await _context.Marcas.FirstOrDefaultAsync(m => m.IdMarca == modelo.IdMarca);
        return marca?.NombreMarca ?? $"Marca #{modelo.IdMarca}";
    }

    private async Task<string> ObtenerNombreModeloAsync(int idModelo)
    {
        var modelo = await _context.Modelos.FirstOrDefaultAsync(m => m.IdModelo == idModelo);
        return modelo?.NombreModelo ?? $"Modelo #{idModelo}";
    }

    private async Task<string> ObtenerNombreTipoAsync(int idTipo)
    {
        var tipo = await _context.TiposTecnologia.FirstOrDefaultAsync(t => t.IdTipoTecnologia == idTipo);
        return tipo?.NombreTipoTecnologia ?? $"Tipo #{idTipo}";
    }

    private async Task<string> ObtenerDescripcionTipoAsync(int idTipo)
    {
        var tipo = await _context.TiposTecnologia.FirstOrDefaultAsync(t => t.IdTipoTecnologia == idTipo);
        return tipo?.Descripcion ?? string.Empty;
    }

    private async Task<DateTime> ObtenerFechaEntradaAsync(int? idEntrada)
    {
        var entrada = idEntrada.HasValue
            ? await _context.EntradasTecnologia.FirstOrDefaultAsync(e => e.IdEntradaTecnologia == idEntrada.Value)
            : null;
        return entrada?.FechaEntrada ?? DateTime.Today;
    }

    private async Task<string> ObtenerProveedorEntradaAsync(int? idEntrada)
    {
        var entrada = idEntrada.HasValue
            ? await _context.EntradasTecnologia.FirstOrDefaultAsync(e => e.IdEntradaTecnologia == idEntrada.Value)
            : null;
        if (entrada == null) return string.Empty;

        var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.IdProveedor == entrada.IdProveedor);
        return proveedor?.NombreProveedor ?? string.Empty;
    }

    public async Task<List<SelectListItem>> ObtenerProveedoresSelectAsync(int? seleccionado = null)
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

    private async Task<int> ObtenerProveedorPorNombreAsync(string nombreProveedor)
    {
        var nombre = nombreProveedor.Trim();
        var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.NombreProveedor == nombre);
        if (proveedor == null)
            throw new InvalidOperationException("El proveedor seleccionado no existe. Registrelo en el mantenedor de proveedores.");

        return proveedor.IdProveedor;
    }

    private async Task<int> ObtenerCantidadEntradaAsync(int? idEntrada)
    {
        var entrada = idEntrada.HasValue
            ? await _context.EntradasTecnologia.FirstOrDefaultAsync(e => e.IdEntradaTecnologia == idEntrada.Value)
            : null;
        return entrada?.Cantidad ?? 1;
    }

    private async Task<string> ObtenerFacturaEntradaAsync(int? idEntrada)
    {
        var entrada = idEntrada.HasValue
            ? await _context.EntradasTecnologia.FirstOrDefaultAsync(e => e.IdEntradaTecnologia == idEntrada.Value)
            : null;
        return entrada?.NumeroFactura ?? string.Empty;
    }

    private async Task<string> ObtenerEstadoOperativoAsync(Tecnologia equipo)
    {
        if (!equipo.Estado)
            return Estado.EquipoDadoDeBaja;

        var bajaAprobada = await _context.Bajas.AnyAsync(b =>
            b.IdTecnologia == equipo.IdTecnologia && b.Estado == Estado.Aprobada);
        if (bajaAprobada)
            return Estado.EquipoDadoDeBaja;

        var reparacionActiva = await _context.Reparaciones.AnyAsync(r =>
            r.IdTecnologia == equipo.IdTecnologia &&
            r.EstadoReparacion != Estado.Reparada &&
            r.EstadoReparacion != Estado.Rechazada);
        if (reparacionActiva)
            return Estado.EquipoEnReparacion;

        var asignacionActiva = await _context.Asignaciones.AnyAsync(a =>
            a.IdTecnologia == equipo.IdTecnologia &&
            a.FechaDevolucion == null &&
            (a.EstadoAsignacion == "Vigente" || a.EstadoAsignacion == "Activa"));
        if (asignacionActiva)
            return Estado.EquipoAsignado;

        return Estado.EquipoDisponible;
    }
}
