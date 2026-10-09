using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Plataforma.Chatter;
using PolyConecta.Domain.Plataforma.Chatter;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Plataforma.Chatter;

public sealed class MensajesChatterEf(PolyDbContext db) : IMensajesChatter
{
    public void Agregar(ChatterMessage mensaje) => db.MensajesChatter.Add(mensaje);

    public async Task<IReadOnlyList<ChatterMessage>> DelDocumentoAsync(
        string tipo, long id, long? antesDe, int cantidad, CancellationToken cancellationToken = default) =>
        await db.MensajesChatter.AsNoTracking()
            .Where(m => m.DocumentType == tipo && m.DocumentId == id && (antesDe == null || m.Id < antesDe))
            .OrderByDescending(m => m.Id)
            .Take(cantidad)
            .ToListAsync(cancellationToken);
}
