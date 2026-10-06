using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Folios;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Plataforma;

/// <summary>
/// Lee la secuencia con UPDLOCK, ROWLOCK dentro de la transacción del caso de uso, calcula el
/// folio en el dominio y escribe solo esa fila. El bloqueo se libera al confirmar la transacción,
/// así que dos peticiones concurrentes nunca obtienen el mismo folio. Si no hay transacción
/// abierta, abre y confirma la suya.
/// </summary>
public sealed class ReferenceSequenceService(PolyDbContext db, IClock clock) : IReferenceSequenceService
{
    public async Task<string> NextAsync(
        string documentType, IReadOnlyDictionary<string, string>? contexto = null, CancellationToken cancellationToken = default)
    {
        var propia = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var secuencia = await db.ReferenceSequences
                .FromSql($"SELECT * FROM plt.reference_sequence WITH (UPDLOCK, ROWLOCK) WHERE DocumentType = {documentType}")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new SecuenciaNoConfiguradaException(documentType);

            var folio = secuencia.Siguiente(clock.Now, contexto);
            await db.Database.ExecuteSqlAsync(
                $"UPDATE plt.reference_sequence SET NextNumber = {secuencia.NextNumber}, CurrentPeriod = {secuencia.CurrentPeriod} WHERE DocumentType = {documentType}",
                cancellationToken);

            if (propia is not null) await propia.CommitAsync(cancellationToken);
            return folio;
        }
        finally
        {
            if (propia is not null) await propia.DisposeAsync();
        }
    }
}
