using SistemaEscolarWeb.Models;
using System.ComponentModel.DataAnnotations;

namespace SistemaEscolarWeb.ViewModels;

public class PermisosRolIndexViewModel : IListadoPaginado
{
    public IEnumerable<Rol> Roles { get; set; } = [];
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

public class AdministrarPermisosRolViewModel
{
    public int IdRol { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public List<PermisoCheckboxViewModel> Permisos { get; set; } = [];
}

public class PermisoCheckboxViewModel
{
    public int IdPermiso { get; set; }
    public string NombrePermiso { get; set; } = string.Empty;
    public bool Seleccionado { get; set; }
}

public class RolFormViewModel
{
    public int IdRol { get; set; }

    [Required(ErrorMessage = "Debe ingresar el nombre del rol.")]
    [StringLength(80, ErrorMessage = "El nombre del rol no puede superar los 80 caracteres.")]
    [Display(Name = "Nombre del rol")]
    public string NombreRol { get; set; } = string.Empty;
}

public class EliminarRolViewModel
{
    public int IdRol { get; set; }
    public string NombreRol { get; set; } = string.Empty;
}
