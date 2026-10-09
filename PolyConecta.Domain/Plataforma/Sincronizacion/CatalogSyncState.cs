namespace PolyConecta.Domain.Plataforma.Sincronizacion;

public enum ResultadoSincronizacion
{
    Exito,
    Error,
}

/// <summary>
/// Resultado de la última corrida de la sincronización de un catálogo (<c>plt.catalog_sync_state</c>,
/// data-model §2, R-05). Una fila por catálogo; solo la escribe la sincronización.
/// </summary>
public sealed class CatalogSyncState
{
    public const string Productos = "productos";
    public const string Clientes = "clientes";
    public const string Agentes = "agentes";
    public const string Almacenes = "almacenes";

    /// <summary>En el orden en que corren al sincronizar todo: lo que otros referencian primero.</summary>
    public static readonly IReadOnlyList<string> Catalogos = [Almacenes, Agentes, Clientes, Productos];

    public string Catalog { get; private set; } = string.Empty;

    public DateTimeOffset? LastRunAt { get; private set; }

    public DateTimeOffset? LastSuccessAt { get; private set; }

    public ResultadoSincronizacion? LastResult { get; private set; }

    public int RecordsRead { get; private set; }

    public int RecordsChanged { get; private set; }

    public int RecordsArchived { get; private set; }

    public long DurationMs { get; private set; }

    public string? LastError { get; private set; }

    private CatalogSyncState() { }

    public CatalogSyncState(string catalog)
    {
        if (!Catalogos.Contains(catalog)) throw new ArgumentException($"Catálogo desconocido: {catalog}.", nameof(catalog));
        Catalog = catalog;
    }

    public void RegistrarExito(DateTimeOffset cuando, int leidos, int cambiados, int archivados, long duracionMs)
    {
        LastRunAt = cuando;
        LastSuccessAt = cuando;
        LastResult = ResultadoSincronizacion.Exito;
        RecordsRead = leidos;
        RecordsChanged = cambiados;
        RecordsArchived = archivados;
        DurationMs = duracionMs;
        LastError = null;
    }

    /// <summary>La corrida falló: conserva los conteos de la última exitosa y guarda el motivo.</summary>
    public void RegistrarError(DateTimeOffset cuando, string motivo, long duracionMs)
    {
        LastRunAt = cuando;
        LastResult = ResultadoSincronizacion.Error;
        DurationMs = duracionMs;
        LastError = motivo.Length > 1000 ? motivo[..1000] : motivo;
    }
}
