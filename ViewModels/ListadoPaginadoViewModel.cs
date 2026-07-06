namespace SistemaEscolarWeb.ViewModels;

public interface IListadoPaginado
{
    int PaginaActual { get; }
    int TotalPaginas { get; }
    int TotalRegistros { get; }
    int RegistrosPorPagina { get; }
    string? Ordenar { get; }
    string Direccion { get; }
    string? Busqueda { get; }
    bool TienePaginaAnterior { get; }
    bool TienePaginaSiguiente { get; }
}

public class ListadoPaginadoViewModel<T> : IListadoPaginado
{
    public IEnumerable<T> Items { get; set; } = [];
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

public static class ListadoPaginado
{
    public const int RegistrosPorPaginaDefault = 15;

    public static ListadoPaginadoViewModel<T> Crear<T>(
        IEnumerable<T> items,
        string? ordenar,
        string? direccion,
        int pagina,
        string? busqueda = null,
        int registrosPorPagina = RegistrosPorPaginaDefault)
    {
        var lista = items.ToList();
        var totalRegistros = lista.Count;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        return new ListadoPaginadoViewModel<T>
        {
            Items = lista.Skip((pagina - 1) * registrosPorPagina).Take(registrosPorPagina),
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalRegistros = totalRegistros,
            RegistrosPorPagina = registrosPorPagina,
            Ordenar = string.IsNullOrWhiteSpace(ordenar) ? null : ordenar,
            Direccion = EsDescendente(direccion) ? "desc" : "asc",
            Busqueda = busqueda
        };
    }

    public static List<T> Ordenar<T, TKey>(
        IEnumerable<T> items,
        string columna,
        string? ordenar,
        string? direccion,
        Func<T, TKey> selector)
    {
        if (!string.Equals(ordenar, columna, StringComparison.OrdinalIgnoreCase))
            return items.ToList();

        return EsDescendente(direccion)
            ? items.OrderByDescending(selector).ToList()
            : items.OrderBy(selector).ToList();
    }

    public static bool EsDescendente(string? direccion)
        => string.Equals(direccion, "desc", StringComparison.OrdinalIgnoreCase);
}

public static class ListadoVista
{
    public static string ProximaDireccion(IListadoPaginado listado, string columna)
        => string.Equals(listado.Ordenar, columna, StringComparison.OrdinalIgnoreCase) && listado.Direccion == "asc"
            ? "desc"
            : "asc";

    public static string IconoOrden(IListadoPaginado listado, string columna)
    {
        if (!string.Equals(listado.Ordenar, columna, StringComparison.OrdinalIgnoreCase))
            return "bi-arrow-down-up";

        return listado.Direccion == "asc" ? "bi-sort-alpha-down" : "bi-sort-alpha-up";
    }
}
