using System.Text.Json;
using System.Text.RegularExpressions;
using PolyConecta.Application.Common;
using PolyConecta.Domain.Plataforma.Listas;

namespace PolyConecta.Application.Plataforma.Listas;

/// <summary>Favoritos guardados de un usuario (contracts/api-listas.md, Favoritos). Lo implementa Infrastructure.</summary>
public interface IFavoritos
{
    Task<IReadOnlyList<SavedSearch>> DelUsuarioAsync(long usuarioId, string llaveLista, CancellationToken cancellationToken = default);

    void Agregar(SavedSearch favorito);

    void Quitar(SavedSearch favorito);
}

/// <summary>La definición es la de 07 §4.2 tal como la manda la web: filtros, búsqueda, agrupación, orden, columnas y tamaño de página.</summary>
public sealed record FavoritoDto(string Nombre, JsonElement Definicion, bool PorOmision);

/// <summary>Sin permiso propio: cualquier usuario con sesión tiene sus favoritos, y solo los suyos.</summary>
public sealed record ListarFavoritos(string LlaveLista);

public sealed record GuardarFavorito(string LlaveLista, string Nombre, JsonElement Definicion, bool PorOmision);

public sealed record BorrarFavorito(string LlaveLista, string Nombre);

internal static partial class Favorito
{
    [GeneratedRegex(@"^[a-z]+\.[a-z_]+$")]
    private static partial Regex Llave();

    public static long Usuario(ICurrentUser usuario) =>
        usuario.UserId ?? throw new UnauthorizedAccessException("Los favoritos son de un usuario con sesión.");

    public static string LlaveValida(string llave) =>
        llave is { Length: <= 60 } && Llave().IsMatch(llave)
            ? llave
            : throw new ValidacionException([new ErrorValidacion("llaveLista", "La lista es `modulo.lista`, por ejemplo `ventas.pedidos`.")]);

    public static FavoritoDto Dto(SavedSearch f)
    {
        using var definicion = JsonDocument.Parse(f.Definition);
        return new FavoritoDto(f.Name, definicion.RootElement.Clone(), f.IsDefault);
    }
}

public sealed class ListarFavoritosCaso(IFavoritos favoritos, ICurrentUser usuario) : IUseCase<ListarFavoritos, IReadOnlyList<FavoritoDto>>
{
    public async Task<IReadOnlyList<FavoritoDto>> ExecuteAsync(ListarFavoritos request, CancellationToken cancellationToken = default)
    {
        var lista = await favoritos.DelUsuarioAsync(Favorito.Usuario(usuario), Favorito.LlaveValida(request.LlaveLista), cancellationToken);
        return lista.OrderBy(f => f.Name, StringComparer.CurrentCulture).Select(Favorito.Dto).ToList();
    }
}

/// <summary>Crea o reemplaza por nombre. Marcar uno por omisión desmarca el anterior de la misma lista.</summary>
public sealed class GuardarFavoritoCaso(IFavoritos favoritos, ICurrentUser usuario, IClock reloj, IUnitOfWork uow)
    : IUseCase<GuardarFavorito, FavoritoDto>
{
    public async Task<FavoritoDto> ExecuteAsync(GuardarFavorito request, CancellationToken cancellationToken = default)
    {
        if (request.Definicion.ValueKind != JsonValueKind.Object)
            throw new ValidacionException([new ErrorValidacion("definicion", "La definición es un objeto JSON.")]);
        var usuarioId = Favorito.Usuario(usuario);
        var llave = Favorito.LlaveValida(request.LlaveLista);
        var nombre = (request.Nombre ?? string.Empty).Trim();
        var definicion = request.Definicion.GetRawText();
        var existentes = await favoritos.DelUsuarioAsync(usuarioId, llave, cancellationToken);

        SavedSearch favorito;
        try
        {
            favorito = existentes.FirstOrDefault(f => string.Equals(f.Name, nombre, StringComparison.CurrentCultureIgnoreCase))
                ?? new SavedSearch(usuarioId, llave, nombre, definicion, false, reloj.Now);
            favorito.Renombrar(nombre);
            favorito.CambiarDefinicion(definicion);
        }
        catch (ArgumentException e)
        {
            throw new ValidacionException([new ErrorValidacion(e.ParamName == "definition" ? "definicion" : "nombre", e.Message.Split(" (")[0])]);
        }
        if (favorito.Id == 0) favoritos.Agregar(favorito);

        if (request.PorOmision)
        {
            // Primero se desmarca el anterior y se guarda: el índice único filtrado no admite dos a la vez.
            foreach (var otro in existentes.Where(f => f.IsDefault && f != favorito)) otro.MarcarPorOmision(false);
            await uow.SaveChangesAsync(cancellationToken);
        }
        favorito.MarcarPorOmision(request.PorOmision);
        await uow.SaveChangesAsync(cancellationToken);
        return Favorito.Dto(favorito);
    }
}

public sealed class BorrarFavoritoCaso(IFavoritos favoritos, ICurrentUser usuario, IUnitOfWork uow) : IUseCase<BorrarFavorito, Unit>
{
    public async Task<Unit> ExecuteAsync(BorrarFavorito request, CancellationToken cancellationToken = default)
    {
        var existentes = await favoritos.DelUsuarioAsync(Favorito.Usuario(usuario), Favorito.LlaveValida(request.LlaveLista), cancellationToken);
        var favorito = existentes.FirstOrDefault(f => string.Equals(f.Name, request.Nombre?.Trim(), StringComparison.CurrentCultureIgnoreCase))
            ?? throw new KeyNotFoundException($"No existe el favorito {request.Nombre}.");
        favoritos.Quitar(favorito);
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
