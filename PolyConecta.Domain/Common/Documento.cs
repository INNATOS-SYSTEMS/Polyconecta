namespace PolyConecta.Domain.Common;

/// <summary>Documento con un estado cerrado que solo cambia por transiciones con nombre (04 §1).</summary>
public interface IStatefulDocument<out TState> where TState : struct, Enum
{
    TState State { get; }
}

/// <summary>
/// Base de los documentos con ciclo de vida. Cada método de transición (Confirmar, Cancelar…)
/// llama a <see cref="Transicionar"/>, que valida el estado de origen y deja la transición
/// pendiente de registrar en StateTransitionLog al guardar (CT-32).
/// </summary>
public abstract class DocumentoConEstado<TState> : ArchivableEntity, IStatefulDocument<TState>
    where TState : struct, Enum
{
    public TState State { get; private set; }

    protected DocumentoConEstado(TState estadoInicial) => State = estadoInicial;

    /// <summary>Cambia a <paramref name="destino"/> si el estado actual está en <paramref name="desde"/>.</summary>
    protected void Transicionar(TState destino, string? nota, params TState[] desde)
    {
        if (!desde.Contains(State))
            throw new TransicionInvalidaException(GetType().Name, State.ToString(), destino.ToString());

        var anterior = State;
        State = destino;
        AgregarTransicion(new TransicionRegistrada(anterior.ToString(), destino.ToString(), nota));
    }
}

/// <summary>Se intentó una transición que no parte de un estado permitido.</summary>
public sealed class TransicionInvalidaException(string documento, string estadoActual, string estadoPedido)
    : InvalidOperationException($"{documento}: no se puede pasar de {estadoActual} a {estadoPedido}.")
{
    public string Documento { get; } = documento;

    public string EstadoActual { get; } = estadoActual;

    public string EstadoPedido { get; } = estadoPedido;
}
