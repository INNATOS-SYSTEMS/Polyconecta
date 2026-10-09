namespace PolyConecta.Domain.Plataforma.Listas;

/// <summary>
/// Favorito de una lista (<c>plt.saved_search</c>, data-model §2; forma de <c>Favorito</c> de 07 §4.2).
/// Es de un usuario: nadie más lo ve. El nombre es único por usuario y lista, y solo uno por lista es
/// el de por omisión (lo garantiza el caso de uso al marcarlo y un índice único filtrado).
/// </summary>
public sealed class SavedSearch
{
    public const int LargoNombre = 80;

    public long Id { get; private set; }

    public long UserId { get; private set; }

    /// <summary>La lista a la que pertenece: <c>ventas.pedidos</c>.</summary>
    public string ListKey { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>JSON con filtros, búsqueda, agrupación, orden, columnas y tamaño de página; la web lo interpreta.</summary>
    public string Definition { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private SavedSearch() { }

    public SavedSearch(long userId, string listKey, string name, string definition, bool isDefault, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(listKey)) throw new ArgumentException("Falta la lista.", nameof(listKey));
        UserId = userId;
        ListKey = listKey;
        Name = ValidarNombre(name);
        Definition = ValidarDefinicion(definition);
        IsDefault = isDefault;
        CreatedAt = createdAt;
    }

    public void Renombrar(string name) => Name = ValidarNombre(name);

    public void CambiarDefinicion(string definition) => Definition = ValidarDefinicion(definition);

    public void MarcarPorOmision(bool porOmision) => IsDefault = porOmision;

    private static string ValidarNombre(string name)
    {
        var nombre = name?.Trim() ?? string.Empty;
        if (nombre.Length == 0) throw new ArgumentException("El favorito necesita un nombre.", nameof(name));
        if (nombre.Length > LargoNombre) throw new ArgumentException($"El nombre pasa de {LargoNombre} caracteres.", nameof(name));
        return nombre;
    }

    private static string ValidarDefinicion(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition)) throw new ArgumentException("Falta la definición del favorito.", nameof(definition));
        return definition;
    }
}
