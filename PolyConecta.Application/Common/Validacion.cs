namespace PolyConecta.Application.Common;

/// <summary>Validador propio de la petición de un caso de uso.</summary>
public interface IValidator<in TRequest>
{
    IEnumerable<ErrorValidacion> Validate(TRequest request);
}

public sealed record ErrorValidacion(string Campo, string Mensaje);

/// <summary>La petición no cumple sus reglas. La API la traduce a 400.</summary>
public sealed class ValidacionException(IReadOnlyList<ErrorValidacion> errores)
    : Exception("La petición no es válida: " + string.Join("; ", errores.Select(e => $"{e.Campo}: {e.Mensaje}")))
{
    public IReadOnlyList<ErrorValidacion> Errores { get; } = errores;
}
