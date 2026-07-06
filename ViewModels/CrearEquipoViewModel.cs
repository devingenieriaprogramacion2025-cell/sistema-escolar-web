using System.ComponentModel.DataAnnotations;

namespace SistemaEscolarWeb.ViewModels;

public class CrearEquipoViewModel
{
    public int? IdTecnologia { get; set; }
    public int IdModelo { get; set; }
    public int IdTipoTecnologia { get; set; }
    public int? IdEntradaTecnologia { get; set; }

    [Required(ErrorMessage = "Debe ingresar el codigo de inventario.")]
    [Display(Name = "Codigo inventario")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use solo letras, numeros, punto, guion o guion bajo.")]
    public string SkuCodigoInventario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar la marca.")]
    [Display(Name = "Marca")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "La marca contiene caracteres no permitidos.")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el modelo.")]
    [Display(Name = "Modelo")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,#&'()+/-]+$", ErrorMessage = "El modelo contiene caracteres no permitidos.")]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar la fecha de entrada.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha entrada")]
    public DateTime FechaEntrada { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Debe seleccionar el proveedor.")]
    [Display(Name = "Proveedor")]
    public int? IdProveedor { get; set; }

    [Display(Name = "Proveedor")]
    public string Proveedor { get; set; } = string.Empty;

    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Proveedores { get; set; } = [];

    [Required(ErrorMessage = "Debe ingresar la cantidad.")]
    [Range(1, 10000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "Debe ingresar el numero de factura.")]
    [Display(Name = "Numero factura")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-z0-9._/-]+$", ErrorMessage = "El numero de factura contiene caracteres no permitidos.")]
    public string NumeroFactura { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el tipo de tecnologia.")]
    [Display(Name = "Tipo tecnologia")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "El tipo de tecnologia contiene caracteres no permitidos.")]
    public string TipoTecnologia { get; set; } = string.Empty;

    [Display(Name = "Descripcion")]
    [StringLength(500)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,;:#()?!%&'""/\r\n_-]*$", ErrorMessage = "La descripcion contiene caracteres no permitidos.")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}

public class GestionTecnologiaViewModel : IListadoPaginado
{
    public TecnologiaFormViewModel Formulario { get; set; } = new();
    public IEnumerable<SistemaEscolarWeb.DTOs.EquipoDto> Equipos { get; set; } = [];
    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> EntradasTecnologia { get; set; } = [];
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalRegistros { get; set; }
    public int RegistrosPorPagina { get; set; } = 15;
    public string? Ordenar { get; set; }
    public string Direccion { get; set; } = "asc";
    public string? Busqueda { get; set; }
    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}

public class TecnologiaFormViewModel
{
    public int? IdTecnologia { get; set; }

    [Required(ErrorMessage = "Debe ingresar el codigo de inventario.")]
    [Display(Name = "Codigo inventario")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use solo letras, numeros, punto, guion o guion bajo.")]
    public string SkuCodigoInventario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar la marca.")]
    [Display(Name = "Marca")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "La marca contiene caracteres no permitidos.")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el modelo.")]
    [Display(Name = "Modelo")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,#&'()+/-]+$", ErrorMessage = "El modelo contiene caracteres no permitidos.")]
    public string Modelo { get; set; } = string.Empty;

    [Display(Name = "Entrada tecnologia")]
    public int? IdEntradaTecnologia { get; set; }

    [Required(ErrorMessage = "Debe ingresar el tipo de tecnologia.")]
    [Display(Name = "Tipo tecnologia")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "El tipo de tecnologia contiene caracteres no permitidos.")]
    public string TipoTecnologia { get; set; } = string.Empty;

    [Display(Name = "Descripcion")]
    [StringLength(500)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,;:#()?!%&'""/\r\n_-]*$", ErrorMessage = "La descripcion contiene caracteres no permitidos.")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}

public class EntradaTecnologiaFormViewModel
{
    public int? IdEntradaTecnologia { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el proveedor.")]
    [Display(Name = "Proveedor")]
    public int? IdProveedor { get; set; }

    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Proveedores { get; set; } = [];

    [Display(Name = "Proveedor")]
    [StringLength(160)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "El proveedor contiene caracteres no permitidos.")]
    public string Proveedor { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar la marca.")]
    [Display(Name = "Marca")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "La marca contiene caracteres no permitidos.")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el modelo.")]
    [Display(Name = "Modelo")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,#&'()+/-]+$", ErrorMessage = "El modelo contiene caracteres no permitidos.")]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el tipo de tecnologia.")]
    [Display(Name = "Tipo tecnologia")]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,&'/-]+$", ErrorMessage = "El tipo de tecnologia contiene caracteres no permitidos.")]
    public string TipoTecnologia { get; set; } = string.Empty;

    [Display(Name = "Descripcion")]
    [StringLength(500)]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9 .,;:#()?!%&'""/\r\n_-]*$", ErrorMessage = "La descripcion contiene caracteres no permitidos.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Debe ingresar el prefijo SKU.")]
    [Display(Name = "Prefijo SKU")]
    [StringLength(40)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use solo letras, numeros, punto, guion o guion bajo.")]
    public string PrefijoSku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar la fecha de entrada.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha entrada")]
    public DateTime FechaEntrada { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Debe ingresar la cantidad.")]
    [Range(1, 10000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "Debe ingresar el numero de factura.")]
    [Display(Name = "Numero factura")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-z0-9._/-]+$", ErrorMessage = "El numero de factura contiene caracteres no permitidos.")]
    public string NumeroFactura { get; set; } = string.Empty;
}

public class GestionEntradasTecnologiaViewModel : IListadoPaginado
{
    public EntradaTecnologiaFormViewModel Formulario { get; set; } = new();
    public IEnumerable<SistemaEscolarWeb.DTOs.EntradaTecnologiaDto> Entradas { get; set; } = [];
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalRegistros { get; set; }
    public int RegistrosPorPagina { get; set; } = 15;
    public string? Ordenar { get; set; }
    public string Direccion { get; set; } = "asc";
    public string? Busqueda { get; set; }
    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}
