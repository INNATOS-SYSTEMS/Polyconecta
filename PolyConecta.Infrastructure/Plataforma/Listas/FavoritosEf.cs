using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Plataforma.Listas;
using PolyConecta.Domain.Plataforma.Listas;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Plataforma.Listas;

public sealed class FavoritosEf(PolyDbContext db) : IFavoritos
{
    public async Task<IReadOnlyList<SavedSearch>> DelUsuarioAsync(long usuarioId, string llaveLista, CancellationToken cancellationToken = default) =>
        await db.Favoritos.Where(f => f.UserId == usuarioId && f.ListKey == llaveLista).ToListAsync(cancellationToken);

    public void Agregar(SavedSearch favorito) => db.Favoritos.Add(favorito);

    public void Quitar(SavedSearch favorito) => db.Favoritos.Remove(favorito);
}
