namespace PolyConecta.Infrastructure.Outbox;

/// <summary>
/// Publicador previo a F0: solo guarda en memoria y nunca envía nada. Lo sustituye
/// IBridgeSyncService (tarea L2-T019 de la spec 002) y se borra junto con sus usos.
/// </summary>
public interface IOutboxPublisher
{
    Task EnqueueAsync(string messageType, object payload, CancellationToken cancellationToken = default);
}

public class OutboxPublisher : IOutboxPublisher
{
    public Task EnqueueAsync(string messageType, object payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
