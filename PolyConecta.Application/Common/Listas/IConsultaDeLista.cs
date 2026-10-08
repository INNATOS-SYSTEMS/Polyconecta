namespace PolyConecta.Application.Common.Listas;

/// <summary>
/// Contrato común para ejecutar consultas y conjuntos de una lista (contracts/api-listas.md, D-151).
/// </summary>
public interface IConsultaDeLista
{
    string Modulo { get; }
    string Lista { get; }
    string Llave { get; }
    string PermisoLectura { get; }

    VistaDeBusquedaDto DescribirVista();

    Task<ResultadoConjunto<IReadOnlyDictionary<string, object?>>> ConjuntoAsync(CancellationToken cancellationToken = default);

    Task<ResultadoLista<IReadOnlyDictionary<string, object?>>> ConsultarAsync(ConsultaLista consulta, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contrato tipado con la entidad del dominio <typeparamref name="T"/>.
/// </summary>
public interface IConsultaDeLista<T> : IConsultaDeLista where T : class
{
    VistaDeBusqueda<T> Vista { get; }
}
