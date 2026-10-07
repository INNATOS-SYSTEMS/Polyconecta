using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Erp;

public sealed class BridgeSyncService(
    PolyDbContext db, IEnumerable<ICommandPayloadTranslator> traductores, IClock clock, ICorrelationContext correlation)
    : IBridgeSyncService
{
    public async Task EncolarAsync<TDocumento>(TDocumento documento, string transicion, CancellationToken cancellationToken = default)
        where TDocumento : AuditableEntity, ISyncedDocument
    {
        var traductor = traductores.SingleOrDefault(t => t.TipoDocumento == typeof(TDocumento))
            ?? throw new InvalidOperationException($"No hay traductor al contrato para {typeof(TDocumento).Name}.");

        // La idempotency_key necesita el id del documento: si es nuevo, se guarda primero, dentro de
        // la misma transacción del caso de uso.
        if (documento.Id == 0)
            await db.SaveChangesAsync(cancellationToken);

        var tipo = typeof(TDocumento).Name;
        var ahora = clock.Now;
        db.OutboxMessages.Add(new OutboxMessage(
            traductor.CommandType,
            traductor.Variante(documento),
            JsonSerializer.Serialize(traductor.Carga(documento)),
            $"{tipo}:{documento.Id}:{transicion}",
            correlation.CorrelationId,
            tipo,
            documento.Id,
            traductor.Llaves(documento),
            ahora));
        documento.Sync.MarcarPendiente(ahora);
    }
}

public sealed class OutboxStore(PolyDbContext db) : IOutboxStore
{
    public Task<OutboxMessage?> PorLlaveAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        db.OutboxMessages.SingleOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<OutboxMessage?> PorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.OutboxMessages.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
}

/// <summary>Un tipo de documento que escribe en CONTPAQi (AddDocumentoSincronizable).</summary>
public sealed record DocumentoRegistrado(Type Tipo);

/// <summary>Tipos de documento que escriben en CONTPAQi, por su nombre (el DocumentType del outbox).</summary>
public sealed class RegistroDocumentosSincronizables(IEnumerable<DocumentoRegistrado> registrados)
{
    private readonly Dictionary<string, Type> _tipos = registrados.ToDictionary(r => r.Tipo.Name, r => r.Tipo, StringComparer.Ordinal);

    public Type? Tipo(string documentType) => _tipos.GetValueOrDefault(documentType);
}

public sealed class DocumentosSincronizables(PolyDbContext db, RegistroDocumentosSincronizables registro) : IDocumentosSincronizables
{
    public async Task<ISyncedDocument?> BuscarAsync(string documentType, long documentId, CancellationToken cancellationToken = default)
    {
        var tipo = registro.Tipo(documentType);
        if (tipo is null) return null;
        return await db.FindAsync(tipo, [documentId], cancellationToken) as ISyncedDocument;
    }
}
