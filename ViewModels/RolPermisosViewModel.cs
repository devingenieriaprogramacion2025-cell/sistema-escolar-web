namespace SistemaEscolarWeb.ViewModels;

public class RolPermisosViewModel
{
    public string NombreRol { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public List<string> Permisos { get; set; } = [];
}
