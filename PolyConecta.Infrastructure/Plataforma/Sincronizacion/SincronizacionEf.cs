using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using PolyConecta.Application.Plataforma.Sincronizacion;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Sincronizacion;
using PolyConecta.Domain.Ventas;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.Infrastructure.Persistence.Almacenes;

namespace PolyConecta.Infrastructure.Plataforma.Sincronizacion;

/// <summary>
/// <c>sp_getapplock</c> por catálogo (<c>sync:productos</c>…) con dueño la transacción del caso de uso:
/// se libera al confirmar o revertir, y una segunda instancia no espera (LockTimeout 0) (R-05).
/// </summary>
public sealed class CandadoSqlServer(PolyDbContext db) : ICandadoDeSincronizacion
{
    private sealed class Liberado : IAsyncDisposable
    {
        public static readonly Liberado Instancia = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public async Task<IAsyncDisposable?> TomarAsync(string catalogo, CancellationToken cancellationToken = default)
    {
        var transaccion = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("La sincronización corre dentro de la transacción del caso de uso.");
        var conexion = db.Database.GetDbConnection();
        await using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion.GetDbTransaction();
        comando.CommandText = "DECLARE @r int; EXEC @r = sp_getapplock @Resource = @recurso, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; SELECT @r;";
        var p = comando.CreateParameter();
        p.ParameterName = "@recurso";
        p.Value = "sync:" + catalogo;
        p.DbType = DbType.String;
        comando.Parameters.Add(p);
        var r = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return r >= 0 ? Liberado.Instancia : null;
    }
}

public sealed class EstadosDeSincronizacion(PolyDbContext db) : IEstadosDeSincronizacion
{
    public async Task<CatalogSyncState> ObtenerAsync(string catalogo, CancellationToken cancellationToken = default)
    {
        var estado = await db.Set<CatalogSyncState>().SingleOrDefaultAsync(s => s.Catalog == catalogo, cancellationToken);
        if (estado is not null) return estado;
        estado = new CatalogSyncState(catalogo);
        db.Add(estado);
        return estado;
    }

    public async Task<IReadOnlyList<CatalogSyncState>> TodosAsync(CancellationToken cancellationToken = default) =>
        await db.Set<CatalogSyncState>().AsNoTracking().ToListAsync(cancellationToken);
}

public sealed class CatalogSyncStateConfiguration : IEntityTypeConfiguration<CatalogSyncState>
{
    public void Configure(EntityTypeBuilder<CatalogSyncState> builder)
    {
        builder.ToTable("catalog_sync_state", "plt");
        builder.HasKey(x => x.Catalog);
        builder.Property(x => x.Catalog).HasMaxLength(20);
        builder.Property(x => x.LastResult).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.LastError).HasMaxLength(1000);
    }
}

/// <summary>El producto se carga con su unidad y su ficha técnica.</summary>
public sealed class IncluirProducto : IIncluirEnAlmacen<Product>
{
    public IQueryable<Product> Incluir(IQueryable<Product> consulta) =>
        consulta.Include(p => p.PackagingUnits).Include(p => p.Roll).Include(p => p.Pt!).ThenInclude(pt => pt.RelatedRoll).AsSplitQuery();
}

/// <summary>El cliente se carga con sus domicilios.</summary>
public sealed class IncluirCliente : IIncluirEnAlmacen<Customer>
{
    public IQueryable<Customer> Incluir(IQueryable<Customer> consulta) => consulta.Include(c => c.Addresses);
}

/// <summary>El pedido se carga con su cliente y domicilios, sus líneas con producto y unidad, y sus firmas.</summary>
public sealed class IncluirPedido : IIncluirEnAlmacen<SalesOrder>
{
    public IQueryable<SalesOrder> Incluir(IQueryable<SalesOrder> consulta) =>
        consulta.Include(p => p.Customer).ThenInclude(c => c.Addresses)
            .Include(p => p.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.PackagingUnits)
            .Include(p => p.Signatures)
            .AsSplitQuery();
}

/// <summary>Monedas del pedido desde la sección Erp (D-146).</summary>
public sealed class ConfiguracionDeMonedas(Microsoft.Extensions.Options.IOptions<Erp.ErpOptions> opciones) : Application.Ventas.IConfiguracionDeMonedas
{
    public ReglasDeMoneda Monedas { get; } = new(
        opciones.Value.MonedaBase.ToUpperInvariant(),
        opciones.Value.Monedas.Select(m => m.ToUpperInvariant()).Append(opciones.Value.MonedaBase.ToUpperInvariant()).Distinct().ToList());
}
