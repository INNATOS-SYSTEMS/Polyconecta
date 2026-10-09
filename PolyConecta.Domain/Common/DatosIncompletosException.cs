namespace PolyConecta.Domain.Common;

/// <summary>Un campo que falta o no es válido para una operación del dominio.</summary>
public sealed record CampoFaltante(string Campo, string Mensaje);

/// <summary>
/// La operación no procede porque faltan datos del documento (por ejemplo, confirmar un pedido sin
/// precio). La API la traduce a 400 VALIDACION con <c>errores[]</c>, para pintarlos en sus campos.
/// </summary>
public sealed class DatosIncompletosException(string mensaje, IReadOnlyList<CampoFaltante> faltantes) : InvalidOperationException(mensaje)
{
    public IReadOnlyList<CampoFaltante> Faltantes { get; } = faltantes;
}

/// <summary>Detalle de un documento: lo borra el propio documento al quitarlo (líneas, firmas). Su llave borra en cascada.</summary>
public interface IParteDeDocumento;
