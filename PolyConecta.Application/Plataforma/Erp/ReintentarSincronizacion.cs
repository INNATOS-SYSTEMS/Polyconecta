using PolyConecta.Application.Common;
using PolyConecta.Domain.Plataforma;

namespace PolyConecta.Application.Plataforma.Erp;

public sealed record ReintentarSincronizacionRequest(Guid OutboxMessageId);

/// <summary>
/// Sistemas reintenta un comando en Error sin tocar la base (CT-20): vuelve a Pendiente con la
/// misma idempotency_key, así el bridge nunca duplica el documento. Los comandos que quedaron
/// Bloqueado detrás de él se liberan solos cuando el despachador vuelve a evaluar.
/// </summary>
public sealed class ReintentarSincronizacion(IOutboxStore outbox, IDocumentosSincronizables documentos, IClock clock)
    : IUseCase<ReintentarSincronizacionRequest, Unit>
{
    public async Task<Unit> ExecuteAsync(ReintentarSincronizacionRequest request, CancellationToken cancellationToken = default)
    {
        var mensaje = await outbox.PorIdAsync(request.OutboxMessageId, cancellationToken)
            ?? throw new ValidacionException([new ErrorValidacion(nameof(request.OutboxMessageId), "No existe el comando.")]);
        if (mensaje.Status != OutboxStatus.Error)
            throw new ValidacionException([new ErrorValidacion(nameof(request.OutboxMessageId), $"Solo se reintenta un comando en Error; está en {mensaje.Status}.")]);

        mensaje.Reintentar();
        var documento = await documentos.BuscarAsync(mensaje.DocumentType, mensaje.DocumentId, cancellationToken);
        documento?.Sync.Reintentar(clock.Now);
        return Unit.Value;
    }
}
