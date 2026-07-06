using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaEscolarWeb.ViewModels;

public class CrearEntradaInsumoViewModel
{
    public int? IdEntradaInsumo { get; set; }

    [Required(ErrorMessage = "Seleccione un insumo.")]
    public int? IdInsumo { get; set; }

    [Required(ErrorMessage = "Seleccione un proveedor.")]
    public int? IdProveedor { get; set; }

    public string NombreProveedor { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el numero de factura.")]
    [StringLength(80)]
    public string NumeroFactura { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime FechaEntrega { get; set; } = DateTime.Today;

    [Range(1, 100000, ErrorMessage = "La cantidad debe estar entre 1 y 100000.")]
    public int Cantidad { get; set; } = 1;

    public IEnumerable<SelectListItem> Insumos { get; set; } = [];
    public IEnumerable<SelectListItem> Proveedores { get; set; } = [];
}

public class CrearSalidaInsumoViewModel
{
    public int? IdSalidaInsumo { get; set; }

    [Required(ErrorMessage = "Seleccione un insumo.")]
    public int? IdInsumo { get; set; }

    [Required(ErrorMessage = "Seleccione una dependencia.")]
    public int? IdDependencia { get; set; }

    [Required(ErrorMessage = "Seleccione la persona responsable.")]
    public string RutPersonal { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "La cantidad debe estar entre 1 y 100000.")]
    public int Cantidad { get; set; } = 1;

    [Required]
    [DataType(DataType.Date)]
    public DateTime FechaSalida { get; set; } = DateTime.Today;

    public IEnumerable<SelectListItem> Insumos { get; set; } = [];
    public IEnumerable<SelectListItem> Dependencias { get; set; } = [];
    public IEnumerable<SelectListItem> Personal { get; set; } = [];
}

public class GestionEntradasInsumoViewModel
{
    public CrearEntradaInsumoViewModel Formulario { get; set; } = new();
    public IEnumerable<SistemaEscolarWeb.DTOs.EntradaInsumoDto> Entradas { get; set; } = [];
    public IEnumerable<SelectListItem> TiposInsumo { get; set; } = [];
}

public class GestionSalidasInsumoViewModel
{
    public CrearSalidaInsumoViewModel Formulario { get; set; } = new();
    public IEnumerable<SistemaEscolarWeb.DTOs.SalidaInsumoDto> Salidas { get; set; } = [];
}
