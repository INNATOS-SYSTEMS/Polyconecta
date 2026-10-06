using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma;

namespace PolyConecta.Application.Plataforma.Erp;

/// <summary>
/// Puerta de toda escritura a CONTPAQi (Principio II, D-122): encola el comando del documento en el
/// outbox, en la misma transacción que el cambio de negocio (CT-20). La traducción del documento a
/// la carga del contrato vive en Infrastructure/Erp; el dominio no conoce el contrato.
/// </summary>
public interface IBridgeSyncService
{
    /// <param name="transicion">Transición que dispara el envío; forma la idempotency_key (CT-19).</param>
    Task EncolarAsync<TDocumento>(TDocumento documento, string transicion, CancellationToken cancellationToken = default)
        where TDocumento : AuditableEntity, ISyncedDocument;
}

/// <summary>Acceso al outbox desde los casos de uso.</summary>
public interface IOutboxStore
{
    Task<OutboxMessage?> PorLlaveAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<OutboxMessage?> PorIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Busca el documento al que pertenece un mensaje del outbox, por su tipo y su id.</summary>
public interface IDocumentosSincronizables
{
    Task<ISyncedDocument?> BuscarAsync(string documentType, long documentId, CancellationToken cancellationToken = default);
}
