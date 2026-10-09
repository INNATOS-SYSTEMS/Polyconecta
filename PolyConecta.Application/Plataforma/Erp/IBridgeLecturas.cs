using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Application.Plataforma.Erp;

/// <summary>Una página de una lectura paginada del contrato (§6): sus filas y el cursor de la siguiente.</summary>
public sealed record PaginaLectura<T>(IReadOnlyList<T> Items, string? Siguiente);

/// <summary>Valor de "TIPO DE PRODUCTOS" de CONTPAQi (`clasificacion`, contrato 1.1).</summary>
public sealed record ClasificacionLeida(string Codigo, string Nombre);

public sealed record ProductoLeido(DatosErpProducto Datos, ClasificacionLeida? Clasificacion);

public sealed record AgenteLeido(long IdErp, string Codigo, string Nombre, TipoAgente Tipo);

public sealed record AlmacenLeido(long IdErp, string Codigo, string Nombre);

/// <summary>
/// Lecturas de catálogos del bridge (contrato §6, R-05). Lo implementa Infrastructure sobre HTTP; las
/// pruebas de aplicación usan uno falso. Toda falla de red o del bridge es <see cref="LecturaBridgeException"/>.
/// </summary>
public interface IBridgeLecturas
{
    Task<PaginaLectura<ProductoLeido>> ProductosAsync(int limite, string? cursor, CancellationToken cancellationToken = default);

    Task<PaginaLectura<DatosErpCliente>> ClientesAsync(int limite, string? cursor, CancellationToken cancellationToken = default);

    Task<PaginaLectura<AgenteLeido>> AgentesAsync(int limite, string? cursor, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlmacenLeido>> AlmacenesAsync(CancellationToken cancellationToken = default);
}

/// <summary>El bridge no respondió o respondió algo que no cumple el contrato. La sincronización queda en Error.</summary>
public sealed class LecturaBridgeException(string mensaje, Exception? causa = null) : Exception(mensaje, causa);
