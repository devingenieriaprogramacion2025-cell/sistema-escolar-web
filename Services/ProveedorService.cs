using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Repositories;
using SistemaEscolarWeb.ViewModels;

namespace SistemaEscolarWeb.Services;

public class ProveedorService
{
    private readonly ProveedorRepository _repository;

    public ProveedorService(ProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Proveedor>> ListarAsync()
        => await _repository.ListarAsync();

    public async Task<Proveedor?> ObtenerAsync(int id)
        => await _repository.ObtenerAsync(id);

    public async Task<List<SelectListItem>> SelectListAsync(int? seleccionado = null)
    {
        var proveedores = await ListarAsync();
        return proveedores
            .OrderBy(p => p.NombreProveedor)
            .Select(p => new SelectListItem
            {
                Value = p.IdProveedor.ToString(),
                Text = $"{p.NombreProveedor} - {p.RutProveedor}",
                Selected = seleccionado == p.IdProveedor
            })
            .ToList();
    }

    public async Task<bool> ExisteRutAsync(string rutProveedor, int? idExcluir = null)
        => await _repository.ExisteRutAsync(ChileanFormatHelper.NormalizeRut(rutProveedor), idExcluir);

    public async Task<Proveedor> CrearAsync(CrearProveedorViewModel model)
    {
        var rutProveedor = ChileanFormatHelper.FormatRutWithDots(model.RutProveedor);
        if (await ExisteRutAsync(rutProveedor))
            throw new InvalidOperationException("Ya existe un proveedor con el RUT ingresado.");

        var proveedor = new Proveedor
        {
            NombreProveedor = model.NombreProveedor.Trim(),
            RutProveedor = rutProveedor,
            Correo = string.IsNullOrWhiteSpace(model.Correo) ? null : model.Correo.Trim(),
            Telefono = ChileanFormatHelper.NormalizePhone(model.Telefono)
        };

        await _repository.AgregarAsync(proveedor);
        return proveedor;
    }

    public async Task ActualizarAsync(CrearProveedorViewModel model)
    {
        if (!model.IdProveedor.HasValue)
            throw new InvalidOperationException("No se encontro el proveedor a actualizar.");

        var proveedor = await _repository.ObtenerAsync(model.IdProveedor.Value);
        if (proveedor == null)
            throw new InvalidOperationException("El proveedor seleccionado no existe.");

        var rutProveedor = ChileanFormatHelper.FormatRutWithDots(model.RutProveedor);
        if (await ExisteRutAsync(rutProveedor, proveedor.IdProveedor))
            throw new InvalidOperationException("Ya existe otro proveedor con el RUT ingresado.");

        proveedor.NombreProveedor = model.NombreProveedor.Trim();
        proveedor.RutProveedor = rutProveedor;
        proveedor.Correo = string.IsNullOrWhiteSpace(model.Correo) ? null : model.Correo.Trim();
        proveedor.Telefono = ChileanFormatHelper.NormalizePhone(model.Telefono);
        await _repository.GuardarAsync();
    }

    public async Task EliminarAsync(int id)
    {
        if (await _repository.TieneEntradasAsync(id))
            throw new InvalidOperationException("No se puede eliminar un proveedor asociado a entradas de insumos o tecnologia.");

        throw new InvalidOperationException("La eliminacion fisica de proveedores no esta habilitada en este mantenedor.");
    }

    public static CrearProveedorViewModel MapearFormulario(Proveedor proveedor)
        => new()
        {
            IdProveedor = proveedor.IdProveedor,
            NombreProveedor = proveedor.NombreProveedor,
            RutProveedor = ChileanFormatHelper.FormatRutWithDots(proveedor.RutProveedor),
            Correo = proveedor.Correo,
            Telefono = proveedor.Telefono
        };
}
