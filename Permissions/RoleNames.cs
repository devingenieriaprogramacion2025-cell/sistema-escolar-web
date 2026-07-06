using SistemaEscolarWeb.Helpers;

namespace SistemaEscolarWeb.Permissions;

public static class RoleNames
{
    public const string Administrador = "Administrador";
    public const string Director = "Director";
    public const string Inspector = "Inspector";
    public const string EncargadoBiblioteca = "Encargado Biblioteca";
    public const string Profesor = "Profesor";
    public const string Secretaria = "Secretaria";
    public const string Auxiliar = "Auxiliar";
    public const string Chofer = "Chofer";

    public static readonly string[] All =
    {
        Administrador,
        Director,
        Inspector,
        EncargadoBiblioteca,
        Profesor,
        Secretaria,
        Auxiliar,
        Chofer
    };

    public static string Normalize(string? role)
    {
        var key = InputValidationHelper.NormalizeKey(role);

        return key switch
        {
            "ADMINISTRADOR" => Administrador,
            "DIRECTORA" => Director,
            "DIRECTOR" => Director,
            "INSPECTOR" => Inspector,
            "ENCARGADA BIBLIOTECA" => EncargadoBiblioteca,
            "ENCARGADO BIBLIOTECA" => EncargadoBiblioteca,
            "ENCARGADA LIBRERIA" => EncargadoBiblioteca,
            "ENCARGADO LIBRERIA" => EncargadoBiblioteca,
            "BIBLIOTECA" => EncargadoBiblioteca,
            "LIBRERIA" => EncargadoBiblioteca,
            "DOCENTE" => Profesor,
            "PROFESOR" => Profesor,
            "PROFESORA" => Profesor,
            "ENCARGADA TECNOLOGIA" => Secretaria,
            "ENCARGADO TECNOLOGIA" => Secretaria,
            "TECNOLOGIA" => Secretaria,
            "SECRETARIA" => Secretaria,
            "SECRETARIO" => Secretaria,
            "AUXILIAR" => Auxiliar,
            "CHOFER" => Chofer,
            _ => role?.Trim() ?? string.Empty
        };
    }

    public static int Order(string role)
    {
        var normalized = Normalize(role);
        var index = Array.IndexOf(All, normalized);
        return index >= 0 ? index : int.MaxValue;
    }
}
