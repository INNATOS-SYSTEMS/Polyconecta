namespace PolyConecta.Application.Common;

/// <summary>
/// Caso de uso: confirmar, autorizar, validar, cerrar… (CT-08). Los controladores y el
/// despachador solo lo invocan; la lógica transversal la ponen los decoradores (D-72).
/// </summary>
public interface IUseCase<in TRequest, TResult>
{
    Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Resultado vacío para casos de uso que no devuelven nada.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
