using PolyConecta.Application.Common;
using PolyConecta.Domain.Plataforma.Chatter;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Application.Plataforma.Chatter;

/// <summary>
/// Documentos cuyo chatter se guarda (R-04). Un tipo que no está aquí no se persiste: las pantallas
/// que siguen en memoria conservan el hub sin guardar hasta que su fase las conecte.
/// </summary>
public sealed class DocumentosConChatter(IAlmacen<SalesOrder> pedidos)
{
    public const string Pedido = "ventas.pedido";

    private static readonly Dictionary<Type, string> PorEntidad = new() { [typeof(SalesOrder)] = Pedido };

    /// <summary>El tipo de documento de una entidad con transiciones, o null si su chatter no se guarda.</summary>
    public static string? TipoDe(Type entidad) => PorEntidad.GetValueOrDefault(entidad);

    /// <summary>Leer el chatter exige el permiso de lectura del documento (contracts/api-f1.md, Chatter).</summary>
    public static string PermisoDeLectura(string tipo) => tipo == Pedido ? Permisos.PedidoLeer : $"{tipo}.leer";

    /// <summary>404 si el documento no existe o las reglas de fila no lo dejan ver (D-154).</summary>
    public async Task ExigirVisibleAsync(string tipo, long id, CancellationToken cancellationToken)
    {
        if (tipo != Pedido) throw new KeyNotFoundException($"El documento {tipo} no tiene chatter guardado.");
        await pedidos.ObtenerAsync(id, "el pedido", cancellationToken);
    }
}

/// <summary>Mensajes guardados (<c>plt.chatter_message</c>). Lo implementa Infrastructure.</summary>
public interface IMensajesChatter
{
    void Agregar(ChatterMessage mensaje);

    /// <summary>Del más reciente al más antiguo; <paramref name="antesDe"/> es el id del último que ya se tiene.</summary>
    Task<IReadOnlyList<ChatterMessage>> DelDocumentoAsync(string tipo, long id, long? antesDe, int cantidad, CancellationToken cancellationToken = default);
}

/// <summary>
/// Transmite al grupo del documento los mensajes guardados, solo después de confirmar la transacción:
/// una transacción revertida no deja ni mensaje ni transmisión (R-04). Lo implementa la API sobre SignalR.
/// </summary>
public interface IChatterNotificador
{
    void Encolar(ChatterMessage mensaje);

    Task EnviarAsync(CancellationToken cancellationToken = default);

    void Descartar();
}

/// <summary><c>MensajeChatter</c> de contracts/api-f1.md: lo que devuelve el historial y lo que transmite el hub.</summary>
public sealed record MensajeChatterDto(long Id, string Tipo, long DocumentoId, string Clase, string Texto, string Autor, string? Grupo, DateTimeOffset Fecha)
{
    public static MensajeChatterDto De(ChatterMessage m) =>
        new(m.Id, m.DocumentType, m.DocumentId, m.Kind.ToString(), m.Body, m.AuthorName, m.GroupExercised, m.CreatedAt);
}

public sealed record ObtenerChatter(string Tipo, long Id, long? AntesDe) : IRequierePermiso
{
    public const int TamanoPagina = 50;

    public string Permiso => DocumentosConChatter.PermisoDeLectura(Tipo);
}

/// <summary><c>Clase</c>: <c>Mensaje</c> o <c>Nota</c>. El autor sale de la sesión, nunca del cliente.</summary>
public sealed record PublicarMensaje(string Tipo, long Id, string Clase, string Texto) : IRequierePermiso
{
    public string Permiso => DocumentosConChatter.PermisoDeLectura(Tipo);
}

public sealed class ObtenerChatterCaso(DocumentosConChatter documentos, IMensajesChatter mensajes)
    : IUseCase<ObtenerChatter, IReadOnlyList<MensajeChatterDto>>
{
    public async Task<IReadOnlyList<MensajeChatterDto>> ExecuteAsync(ObtenerChatter request, CancellationToken cancellationToken = default)
    {
        await documentos.ExigirVisibleAsync(request.Tipo, request.Id, cancellationToken);
        var pagina = await mensajes.DelDocumentoAsync(request.Tipo, request.Id, request.AntesDe, ObtenerChatter.TamanoPagina, cancellationToken);
        return pagina.Select(MensajeChatterDto.De).ToList();
    }
}

public sealed class PublicarMensajeCaso(
    DocumentosConChatter documentos, IMensajesChatter mensajes, IChatterNotificador notificador,
    ICurrentUser usuario, IClock reloj, IUnitOfWork uow) : IUseCase<PublicarMensaje, MensajeChatterDto>
{
    public async Task<MensajeChatterDto> ExecuteAsync(PublicarMensaje request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<TipoMensaje>(request.Clase, ignoreCase: true, out var clase) || clase == TipoMensaje.Cambio)
            throw new ValidacionException([new ErrorValidacion("clase", "La clase es Mensaje o Nota.")]);
        await documentos.ExigirVisibleAsync(request.Tipo, request.Id, cancellationToken);

        ChatterMessage mensaje;
        try
        {
            mensaje = ChatterMessage.Publicar(request.Tipo, request.Id, clase, request.Texto,
                usuario.UserId ?? throw new UnauthorizedAccessException("El chatter exige sesión."),
                usuario.NombreVisible, usuario.GrupoEjercido, reloj.Now);
        }
        catch (ArgumentException e)
        {
            throw new ValidacionException([new ErrorValidacion("texto", e.Message.Split(" (")[0])]);
        }
        mensajes.Agregar(mensaje);
        await uow.SaveChangesAsync(cancellationToken);
        notificador.Encolar(mensaje);
        return MensajeChatterDto.De(mensaje);
    }
}
