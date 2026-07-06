namespace SistemaEscolarWeb.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.DTOs;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.ViewModels;

public class ImpresionService
{
    private const long MaxArchivoBytes = 20 * 1024 * 1024;
    private const string CarpetaArchivos = "uploads/impresiones";

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".rtf",
        ".odt",
        ".ods",
        ".odp",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public ImpresionService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<List<ImpresionDto>> ListarAsync(string? rol, string? rutPersonal, string? busqueda = null)
    {
        var query = _context.SolicitudesImpresion
            .Include(s => s.EstadoImpresion)
            .Include(s => s.Personal)
            .AsQueryable();

        if (rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal))
            query = query.Where(s => s.RutPersonal == rutPersonal);

        var solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
        var resultado = solicitudes.Select(MapearDto).ToList();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            resultado = resultado.Where(i =>
                i.NombrePersonal.ToLower().Contains(termino) ||
                i.Estado.ToLower().Contains(termino) ||
                (i.Archivo ?? "").ToLower().Contains(termino)).ToList();
        }

        return resultado;
    }

    public async Task<CrearImpresionViewModel> CrearFormularioAsync(string? rol, string? rutPersonal)
    {
        var model = new CrearImpresionViewModel();
        if (rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal))
            model.RutPersonal = rutPersonal;
        await CargarCombosAsync(model, rol, rutPersonal);
        return model;
    }

    public async Task CargarCombosAsync(CrearImpresionViewModel model, string? rol, string? rutPersonal)
    {
        var personalQuery = _context.Personal.Where(p => p.Activo);
        if (rol == "Profesor" && !string.IsNullOrWhiteSpace(rutPersonal))
            personalQuery = personalQuery.Where(p => p.RutPersonal == rutPersonal);

        model.PersonalDisponible = await personalQuery
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Apellido)
            .Select(p => new SelectListItem
            {
                Value = p.RutPersonal,
                Text = p.Nombre + " " + p.Apellido + " - " + p.Cargo
            })
            .ToListAsync();

        model.Colores = new List<SelectListItem>
        {
            new() { Value = "Blanco y negro", Text = "Blanco y negro" },
            new() { Value = "Color", Text = "Color" }
        };
    }

    public async Task CrearAsync(CrearImpresionViewModel model)
    {
        if (model.Archivo == null || model.Archivo.Length == 0)
            throw new InvalidOperationException("Debe seleccionar un archivo para imprimir.");
        if (model.CantidadPaginas <= 0)
            throw new InvalidOperationException("La cantidad de paginas debe ser mayor a cero.");
        if (model.CantidadCopias <= 0)
            throw new InvalidOperationException("La cantidad de copias debe ser mayor a cero.");

        var existePersona = await _context.Personal.AnyAsync(p => p.RutPersonal == model.RutPersonal && p.Activo);
        if (!existePersona)
            throw new InvalidOperationException("La persona seleccionada no existe o esta inactiva.");

        var pendiente = await _context.EstadosImpresion.FirstOrDefaultAsync(e => e.Estado == Estado.Pendiente)
            ?? await _context.EstadosImpresion.FirstAsync();

        var rutaArchivo = await GuardarArchivoAsync(model.Archivo);

        var solicitud = new SolicitudImpresion
        {
            RutPersonal = model.RutPersonal,
            IdEstadoImpresion = pendiente.IdEstadoImpresion,
            FechaSolicitud = DateTime.Now,
            Archivo = rutaArchivo,
            CantidadPaginas = model.CantidadPaginas,
            CantidadCopias = model.CantidadCopias,
            Color = model.Color,
            DobleCara = model.DobleCara,
            Detalle = model.Observacion?.Trim()
        };

        _context.SolicitudesImpresion.Add(solicitud);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            EliminarArchivoFisico(rutaArchivo);
            throw;
        }
    }

    public async Task CambiarEstadoAsync(int id, string estado)
    {
        var solicitud = await _context.SolicitudesImpresion.FirstOrDefaultAsync(s => s.IdSolicitudImpresion == id);
        if (solicitud == null)
            throw new InvalidOperationException("La solicitud seleccionada no existe.");

        var estadoEntidad = await _context.EstadosImpresion.FirstOrDefaultAsync(e => e.Estado == estado);
        if (estadoEntidad == null)
            throw new InvalidOperationException("El estado seleccionado no existe.");

        solicitud.IdEstadoImpresion = estadoEntidad.IdEstadoImpresion;
        solicitud.FechaEntrega = estado == Estado.Entregada ? DateTime.Now : solicitud.FechaEntrega;
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("No se permite eliminar solicitudes de impresion. El historial institucional debe conservarse.");
    }

    public async Task<ArchivoImpresionDescarga?> ObtenerArchivoAsync(int id, string? rol, string? rutPersonal)
    {
        var solicitud = await _context.SolicitudesImpresion.FirstOrDefaultAsync(s => s.IdSolicitudImpresion == id);
        if (solicitud == null || !EsArchivoCargado(solicitud.Archivo))
            return null;

        if (rol == "Profesor" && solicitud.RutPersonal != rutPersonal)
            throw new UnauthorizedAccessException("No tiene permisos para descargar este archivo.");

        var rutaFisica = ObtenerRutaFisica(solicitud.Archivo!);
        if (!System.IO.File.Exists(rutaFisica))
            return null;

        var nombreDescarga = ObtenerNombreDescarga(solicitud.Archivo!);
        if (!_contentTypeProvider.TryGetContentType(nombreDescarga, out var contentType))
            contentType = "application/octet-stream";

        return new ArchivoImpresionDescarga(rutaFisica, nombreDescarga, contentType);
    }

    private static ImpresionDto MapearDto(SolicitudImpresion solicitud)
    {
        return new ImpresionDto
        {
            IdSolicitudImpresion = solicitud.IdSolicitudImpresion,
            RutPersonal = solicitud.RutPersonal,
            NombrePersonal = solicitud.Personal == null ? solicitud.RutPersonal : $"{solicitud.Personal.Nombre} {solicitud.Personal.Apellido}",
            Estado = NormalizarEstado(solicitud.EstadoImpresion?.Estado) ?? $"Estado #{solicitud.IdEstadoImpresion}",
            FechaSolicitud = solicitud.FechaSolicitud,
            FechaEntrega = solicitud.FechaEntrega,
            Archivo = solicitud.Archivo,
            CantidadPaginas = solicitud.CantidadPaginas,
            CantidadCopias = solicitud.CantidadCopias,
            Color = solicitud.Color,
            DobleCara = solicitud.DobleCara,
            Detalle = solicitud.Detalle
        };
    }

    private static string? NormalizarEstado(string? estado)
    {
        return InputValidationHelper.NormalizeKey(estado) switch
        {
            "PENDIENTE" => Estado.Pendiente,
            "APROBADA" => Estado.EnProceso,
            "EN PROCESO" => Estado.EnProceso,
            "ENTREGADA" => Estado.Entregada,
            "RECHAZADA" => Estado.Rechazada,
            _ => estado?.Trim()
        };
    }

    private async Task<string?> GuardarArchivoAsync(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return null;

        if (archivo.Length > MaxArchivoBytes)
            throw new InvalidOperationException("El archivo no puede superar los 20 MB.");

        var nombreOriginal = Path.GetFileName(archivo.FileName);
        var extension = Path.GetExtension(nombreOriginal);
        if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
            throw new InvalidOperationException("El formato del archivo no esta permitido.");

        var carpetaFisica = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", "impresiones");
        Directory.CreateDirectory(carpetaFisica);

        var nombreBase = SanitizarNombre(Path.GetFileNameWithoutExtension(nombreOriginal));
        var nombreGuardado = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{nombreBase}{extension.ToLowerInvariant()}";
        var rutaFisica = Path.Combine(carpetaFisica, nombreGuardado);

        await using var stream = System.IO.File.Create(rutaFisica);
        await archivo.CopyToAsync(stream);

        return $"{CarpetaArchivos}/{nombreGuardado}";
    }

    private string ObtenerRutaFisica(string rutaRelativa)
    {
        var nombreArchivo = Path.GetFileName(rutaRelativa);
        return Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", "impresiones", nombreArchivo);
    }

    private void EliminarArchivoFisico(string? rutaRelativa)
    {
        if (!EsArchivoCargado(rutaRelativa))
            return;

        var rutaFisica = ObtenerRutaFisica(rutaRelativa!);
        if (System.IO.File.Exists(rutaFisica))
            System.IO.File.Delete(rutaFisica);
    }

    private static bool EsArchivoCargado(string? rutaRelativa)
    {
        return !string.IsNullOrWhiteSpace(rutaRelativa)
            && rutaRelativa.StartsWith($"{CarpetaArchivos}/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ObtenerNombreDescarga(string rutaRelativa)
    {
        var nombreArchivo = Path.GetFileName(rutaRelativa);
        var partes = nombreArchivo.Split('-', 3);
        return partes.Length == 3 ? partes[2] : nombreArchivo;
    }

    private static string SanitizarNombre(string nombre)
    {
        var caracteres = nombre.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var seguro = new string(caracteres).Trim('-');
        if (string.IsNullOrWhiteSpace(seguro))
            seguro = "archivo";

        return seguro.Length > 80 ? seguro[..80] : seguro;
    }

    public sealed record ArchivoImpresionDescarga(string RutaFisica, string NombreDescarga, string ContentType);
}
