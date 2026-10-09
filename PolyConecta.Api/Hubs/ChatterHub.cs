using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Chatter;

namespace PolyConecta.Api.Hubs;

/// <summary>
/// Chatter en vivo (contracts/api-f1.md, Chatter; R-04). Exige sesión: la misma cookie por el mismo origen.
/// Cada cliente se une al grupo del documento que abre (<c>{tipo}:{id}</c>); los mensajes se guardan con el
/// autor de la sesión por el caso de uso y llegan al grupo como <c>MensajeChatter</c> al confirmarse.
/// </summary>
[Authorize]
public class ChatterHub(IUseCase<PublicarMensaje, MensajeChatterDto> publicar) : Hub
{
    public const string EventoMensaje = "MensajeChatter";

    public static string Grupo(string tipo, long id) => $"{tipo}:{id}";

    public Task UnirseADocumento(string tipo, long id) => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(tipo, id));

    public Task SalirDeDocumento(string tipo, long id) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(tipo, id));

    /// <summary><c>clase</c>: <c>Mensaje</c> o <c>Nota</c>. La transmisión la hace el notificador al confirmar.</summary>
    public Task<MensajeChatterDto> EnviarMensaje(string tipo, long id, string clase, string texto) =>
        publicar.ExecuteAsync(new PublicarMensaje(tipo, id, clase, texto), Context.ConnectionAborted);

    /// <summary>Pantallas que siguen en memoria (folio, sin guardar) hasta que su fase las conecte (R-04).</summary>
    public async Task SendMessage(string documentId, string author, string text)
    {
        await Clients.All.SendAsync("ReceiveChatterMessage", documentId, author, text, DateTime.UtcNow.ToString("g", System.Globalization.CultureInfo.CurrentCulture));
    }
}

/// <summary>Transmite al grupo de su documento los mensajes encolados, después del <c>Commit</c> (R-04).</summary>
public sealed partial class ChatterNotificadorSignalR(IHubContext<ChatterHub> hub, ILogger<ChatterNotificadorSignalR> log) : IChatterNotificador
{
    private readonly List<Domain.Plataforma.Chatter.ChatterMessage> _pendientes = [];

    public void Encolar(Domain.Plataforma.Chatter.ChatterMessage mensaje) => _pendientes.Add(mensaje);

    public void Descartar() => _pendientes.Clear();

    public async Task EnviarAsync(CancellationToken cancellationToken = default)
    {
        var enviar = _pendientes.ToList();
        _pendientes.Clear();
        foreach (var m in enviar)
        {
            try
            {
                await hub.Clients.Group(ChatterHub.Grupo(m.DocumentType, m.DocumentId))
                    .SendAsync(ChatterHub.EventoMensaje, MensajeChatterDto.De(m), cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Ya está guardado: quien abra el documento lo verá en el historial.
                LogNoTransmitido(log, e, m.Id);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se transmitió el mensaje {Id} del chatter")]
    private static partial void LogNoTransmitido(ILogger logger, Exception error, long id);
}
