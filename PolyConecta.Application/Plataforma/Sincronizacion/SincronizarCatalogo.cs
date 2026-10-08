using System.Diagnostics;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Plataforma.Sincronizacion;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Application.Plataforma.Sincronizacion;

/// <summary>Bloqueo de aplicación por catálogo (R-05): una sola sincronización de cada catálogo a la vez.</summary>
public interface ICandadoDeSincronizacion
{
    /// <summary>Null si otra instancia ya lo tiene.</summary>
    Task<IAsyncDisposable?> TomarAsync(string catalogo, CancellationToken cancellationToken = default);
}

/// <summary>Estado de la última corrida de cada catálogo (<see cref="CatalogSyncState"/>).</summary>
public interface IEstadosDeSincronizacion
{
    /// <summary>El estado del catálogo; si no existe, lo crea y lo agrega al contexto.</summary>
    Task<CatalogSyncState> ObtenerAsync(string catalogo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogSyncState>> TodosAsync(CancellationToken cancellationToken = default);
}

public sealed record EstadoCatalogo(
    string Catalogo, DateTimeOffset? UltimaCorrida, DateTimeOffset? UltimaExitosa, string? Resultado, int Leidos, int Cambiados,
    int Archivados, long DuracionMs, string? Error)
{
    public static EstadoCatalogo De(CatalogSyncState s) => new(s.Catalog, s.LastRunAt, s.LastSuccessAt, s.LastResult?.ToString(),
        s.RecordsRead, s.RecordsChanged, s.RecordsArchived, s.DurationMs, s.LastError);
}

public sealed record SincronizarCatalogo(string Catalogo) : IRequierePermiso
{
    public string Permiso => Permisos.SincronizacionEjecutar;
}

public sealed record SincronizarTodo : IRequierePermiso
{
    public string Permiso => Permisos.SincronizacionEjecutar;
}

public sealed record EstadoDeSincronizacion : IRequierePermiso
{
    public string Permiso => Permisos.SincronizacionLeer;
}

/// <summary>
/// Sincroniza un catálogo de CONTPAQi (R-05, D-150): lee el catálogo completo por páginas de 500, hace
/// upsert por el id de CONTPAQi y solo escribe lo que cambió; archiva lo inactivo y, **al terminar la
/// lectura completa**, lo que ya no viene; restaura lo reactivado. Lo de PolyConecta no se pisa: la
/// clasificación solo se llena si está vacía (CT-14). Si la lectura falla, no escribe nada del catálogo
/// y deja el estado en Error con su motivo (US3, escenario 4).
/// </summary>
public sealed class Sincronizador(
    IBridgeLecturas bridge,
    ICandadoDeSincronizacion candado,
    IEstadosDeSincronizacion estados,
    IAlmacen<Product> productos,
    IAlmacen<ProductClassification> clasificaciones,
    IAlmacen<Customer> clientes,
    IAlmacen<ErpAgent> agentes,
    IAlmacen<ErpWarehouse> almacenes,
    IUnitOfWork uow,
    IClock clock)
{
    public const int TamanoPagina = 500;

    private sealed record Conteo(int Leidos, int Cambiados, int Archivados);

    public async Task<EstadoCatalogo> SincronizarAsync(string catalogo, CancellationToken ct)
    {
        if (!CatalogSyncState.Catalogos.Contains(catalogo))
            throw new KeyNotFoundException($"No existe el catálogo {catalogo}.");

        await using var bloqueo = await candado.TomarAsync(catalogo, ct)
            ?? throw new ReglaDeNegocioException("SINCRONIZACION_EN_CURSO", $"Ya hay una sincronización de {catalogo} en curso.");

        var reloj = Stopwatch.StartNew();
        var estado = await estados.ObtenerAsync(catalogo, ct);
        try
        {
            var conteo = catalogo switch
            {
                CatalogSyncState.Productos => await ProductosAsync(ct),
                CatalogSyncState.Clientes => await ClientesAsync(ct),
                CatalogSyncState.Agentes => await AgentesAsync(ct),
                _ => await AlmacenesAsync(ct),
            };
            estado.RegistrarExito(clock.Now, conteo.Leidos, conteo.Cambiados, conteo.Archivados, reloj.ElapsedMilliseconds);
        }
        catch (LecturaBridgeException ex)
        {
            estado.RegistrarError(clock.Now, ex.Message, reloj.ElapsedMilliseconds);
        }
        await uow.SaveChangesAsync(ct);
        return EstadoCatalogo.De(estado);
    }

    private static async Task<List<T>> LeerTodoAsync<T>(Func<string?, Task<PaginaLectura<T>>> pagina)
    {
        var todos = new List<T>();
        string? cursor = null;
        do
        {
            var p = await pagina(cursor);
            todos.AddRange(p.Items);
            if (p.Siguiente is not null && p.Siguiente == cursor)
                throw new LecturaBridgeException("El bridge devolvió el mismo cursor dos veces.");
            cursor = p.Siguiente;
        }
        while (cursor is not null);
        return todos;
    }

    private async Task<Conteo> ProductosAsync(CancellationToken ct)
    {
        var leidos = await LeerTodoAsync(c => bridge.ProductosAsync(TamanoPagina, c, ct));
        var ahora = clock.Now;

        // Las clasificaciones nuevas de CONTPAQi se crean antes, para tener su id (FR-017).
        var porValor = (await clasificaciones.ListarAsync(incluirArchivados: true, cancellationToken: ct))
            .Where(c => c.ErpValue is not null).ToDictionary(c => c.ErpValue!, StringComparer.OrdinalIgnoreCase);
        var codigos = (await clasificaciones.ListarAsync(incluirArchivados: true, cancellationToken: ct)).Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var c in leidos.Select(l => l.Clasificacion).OfType<ClasificacionLeida>().DistinctBy(c => c.Codigo, StringComparer.OrdinalIgnoreCase))
        {
            if (porValor.ContainsKey(c.Codigo)) continue;
            var codigo = codigos.Contains(c.Codigo) ? $"{c.Codigo}-ERP" : c.Codigo;
            var nueva = new ProductClassification(codigo, c.Nombre, c.Codigo);
            clasificaciones.Agregar(nueva);
            porValor[c.Codigo] = nueva;
            codigos.Add(codigo);
        }
        await uow.SaveChangesAsync(ct);

        var actuales = (await productos.ListarAsync(incluirArchivados: true, cancellationToken: ct)).ToDictionary(p => p.ErpProductId);
        int cambiados = 0, archivados = 0;
        foreach (var l in leidos)
        {
            if (!actuales.TryGetValue(l.Datos.IdErp, out var p))
            {
                p = Product.DesdeErp(l.Datos, ahora);
                productos.Agregar(p);
                actuales[l.Datos.IdErp] = p;
                cambiados++;
                if (!l.Datos.Activo) p.ArchivarPorErp();
            }
            else
            {
                var cambio = p.ActualizarDesdeErp(l.Datos, ahora);
                if (l.Datos.Activo) cambio |= p.RestaurarPorErp();
                else if (p.ArchivarPorErp()) archivados++;
                if (cambio) cambiados++;
            }
            if (l.Clasificacion is not null && p.ClassificationId is null)
                p.ClasificarSiVacia(porValor[l.Clasificacion.Codigo].Id);
        }
        var vigentes = leidos.Select(l => l.Datos.IdErp).ToHashSet();
        archivados += actuales.Values.Where(p => !vigentes.Contains(p.ErpProductId)).Count(p => p.ArchivarPorErp());
        return new Conteo(leidos.Count, cambiados, archivados);
    }

    private async Task<Conteo> ClientesAsync(CancellationToken ct)
    {
        var leidos = await LeerTodoAsync(c => bridge.ClientesAsync(TamanoPagina, c, ct));
        var actuales = (await clientes.ListarAsync(incluirArchivados: true, cancellationToken: ct)).ToDictionary(c => c.ErpCustomerId);
        int cambiados = 0, archivados = 0;
        foreach (var l in leidos)
        {
            if (!actuales.TryGetValue(l.IdErp, out var c))
            {
                c = Customer.DesdeErp(l);
                clientes.Agregar(c);
                actuales[l.IdErp] = c;
                cambiados++;
                if (!l.Activo) c.ArchivarPorErp();
                continue;
            }
            var cambio = c.ActualizarDesdeErp(l);
            if (l.Activo) cambio |= c.RestaurarPorErp();
            else if (c.ArchivarPorErp()) archivados++;
            if (cambio) cambiados++;
        }
        var vigentes = leidos.Select(l => l.IdErp).ToHashSet();
        archivados += actuales.Values.Where(c => !vigentes.Contains(c.ErpCustomerId)).Count(c => c.ArchivarPorErp());
        return new Conteo(leidos.Count, cambiados, archivados);
    }

    private async Task<Conteo> AgentesAsync(CancellationToken ct)
    {
        var leidos = await LeerTodoAsync(c => bridge.AgentesAsync(TamanoPagina, c, ct));
        var actuales = (await agentes.ListarAsync(incluirArchivados: true, cancellationToken: ct)).ToDictionary(a => a.ErpAgentId);
        int cambiados = 0;
        foreach (var l in leidos)
        {
            if (!actuales.TryGetValue(l.IdErp, out var a))
            {
                a = new ErpAgent(l.IdErp, l.Codigo, l.Nombre, l.Tipo);
                agentes.Agregar(a);
                actuales[l.IdErp] = a;
                cambiados++;
                continue;
            }
            var cambio = a.ActualizarDesdeErp(l.Codigo, l.Nombre, l.Tipo);
            if (!a.IsActive)
            {
                a.Restore();
                cambio = true;
            }
            if (cambio) cambiados++;
        }
        var vigentes = leidos.Select(l => l.IdErp).ToHashSet();
        var archivados = Archivar(actuales.Values.Where(a => !vigentes.Contains(a.ErpAgentId)));
        return new Conteo(leidos.Count, cambiados, archivados);
    }

    private async Task<Conteo> AlmacenesAsync(CancellationToken ct)
    {
        var leidos = await bridge.AlmacenesAsync(ct);
        var actuales = (await almacenes.ListarAsync(incluirArchivados: true, cancellationToken: ct)).ToDictionary(a => a.ErpWarehouseId);
        int cambiados = 0;
        foreach (var l in leidos)
        {
            if (!actuales.TryGetValue(l.IdErp, out var a))
            {
                a = new ErpWarehouse(l.IdErp, l.Codigo, l.Nombre);
                almacenes.Agregar(a);
                actuales[l.IdErp] = a;
                cambiados++;
                continue;
            }
            var cambio = a.ActualizarDesdeErp(l.Codigo, l.Nombre);
            if (!a.IsActive)
            {
                a.Restore();
                cambio = true;
            }
            if (cambio) cambiados++;
        }
        var vigentes = leidos.Select(l => l.IdErp).ToHashSet();
        var archivados = Archivar(actuales.Values.Where(a => !vigentes.Contains(a.ErpWarehouseId)));
        return new Conteo(leidos.Count, cambiados, archivados);
    }

    private static int Archivar(IEnumerable<ArchivableEntity> entidades)
    {
        var n = 0;
        foreach (var e in entidades.Where(e => e.IsActive))
        {
            e.Archive();
            n++;
        }
        return n;
    }
}

public sealed class SincronizarCatalogoCaso(Sincronizador sincronizador) : IUseCase<SincronizarCatalogo, EstadoCatalogo>
{
    public Task<EstadoCatalogo> ExecuteAsync(SincronizarCatalogo request, CancellationToken cancellationToken = default) =>
        sincronizador.SincronizarAsync(request.Catalogo, cancellationToken);
}

/// <summary>Los cuatro catálogos en orden (almacenes, agentes, clientes, productos). Uno en curso no detiene a los demás.</summary>
public sealed class SincronizarTodoCaso(Sincronizador sincronizador, IEstadosDeSincronizacion estados) : IUseCase<SincronizarTodo, IReadOnlyList<EstadoCatalogo>>
{
    public async Task<IReadOnlyList<EstadoCatalogo>> ExecuteAsync(SincronizarTodo request, CancellationToken cancellationToken = default)
    {
        var resultado = new List<EstadoCatalogo>();
        foreach (var catalogo in CatalogSyncState.Catalogos)
        {
            try
            {
                resultado.Add(await sincronizador.SincronizarAsync(catalogo, cancellationToken));
            }
            catch (ReglaDeNegocioException ex) when (ex.Codigo == "SINCRONIZACION_EN_CURSO")
            {
                resultado.Add(EstadoCatalogo.De(await estados.ObtenerAsync(catalogo, cancellationToken)) with { Error = ex.Razon });
            }
        }
        return resultado;
    }
}

public sealed class EstadoDeSincronizacionCaso(IEstadosDeSincronizacion estados) : IUseCase<EstadoDeSincronizacion, IReadOnlyList<EstadoCatalogo>>
{
    public async Task<IReadOnlyList<EstadoCatalogo>> ExecuteAsync(EstadoDeSincronizacion request, CancellationToken cancellationToken = default)
    {
        var todos = await estados.TodosAsync(cancellationToken);
        return CatalogSyncState.Catalogos
            .Select(c => todos.FirstOrDefault(s => s.Catalog == c) is { } s ? EstadoCatalogo.De(s) : new EstadoCatalogo(c, null, null, null, 0, 0, 0, 0, null))
            .ToList();
    }
}
