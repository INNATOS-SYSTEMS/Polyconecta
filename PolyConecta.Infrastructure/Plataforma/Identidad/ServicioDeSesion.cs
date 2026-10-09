using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Plataforma.Identidad;

public sealed record UsuarioDeSesion(long Id, string Usuario, string Nombre, long? AgenteId);

public sealed record AsignacionDeSesion(string Grupo, string NombreGrupo, string Planta, bool Suplente);

/// <summary>Lo que la web necesita de la sesión (contracts/api-f1.md, Sesión). La regla la decide la API (CT-11).</summary>
public sealed record Sesion(UsuarioDeSesion Usuario, IReadOnlyList<AsignacionDeSesion> Asignaciones, IReadOnlyList<string> Permisos);

public enum ResultadoEntrada
{
    Correcto,
    CredencialesInvalidas,
    Bloqueado,
}

/// <summary>
/// Entrar, salir y leer la sesión (R-01). No es un caso de uso de negocio: traduce Identity y la cookie.
/// Un usuario archivado no entra aunque su contraseña sea correcta (US2, escenario 5).
/// </summary>
public sealed class ServicioDeSesion(
    PolyDbContext db,
    SignInManager<CredencialUsuario> entrada,
    UserManager<CredencialUsuario> credenciales,
    IPermisosDelUsuario permisos)
{
    public async Task<(ResultadoEntrada Resultado, long? UserId)> EntrarAsync(string usuario, string contrasena, CancellationToken ct)
    {
        var credencial = await credenciales.FindByNameAsync(usuario.Trim());
        if (credencial is null) return (ResultadoEntrada.CredencialesInvalidas, null);
        if (!await db.Usuarios.AnyAsync(u => u.Id == credencial.UserId, ct)) return (ResultadoEntrada.CredencialesInvalidas, null);

        var r = await entrada.PasswordSignInAsync(credencial, contrasena, isPersistent: false, lockoutOnFailure: true);
        if (r.IsLockedOut) return (ResultadoEntrada.Bloqueado, null);
        return r.Succeeded ? (ResultadoEntrada.Correcto, credencial.UserId) : (ResultadoEntrada.CredencialesInvalidas, null);
    }

    public Task SalirAsync() => entrada.SignOutAsync();

    /// <summary>La sesión del usuario <paramref name="userId"/>; las asignaciones salen de <see cref="IPermisosDelUsuario"/>.</summary>
    public async Task<Sesion?> SesionAsync(long userId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (usuario is null) return null;
        var asignaciones = await new PermisosDelUsuario(db, new UsuarioConId(userId)).AsignacionesAsync(ct);
        return new Sesion(
            new UsuarioDeSesion(usuario.Id, usuario.UserName, usuario.DisplayName, usuario.ErpAgentId),
            asignaciones.Select(a => new AsignacionDeSesion(a.GrupoCodigo, a.GrupoNombre, a.PlantaCodigo, a.EsSuplente)).ToList(),
            asignaciones.SelectMany(a => a.Permisos).Distinct().Order(StringComparer.Ordinal).ToList());
    }

    /// <summary>Usuario fijo para leer las asignaciones de quien acaba de entrar (su cookie llega hasta la siguiente petición).</summary>
    private sealed class UsuarioConId(long id) : Common.SistemaCurrentUser
    {
        public override long? UserId => id;
    }
}

/// <summary>Claims de la cookie: el usuario de dominio, con lo que entra y su nombre visible (R-01).</summary>
public sealed class FabricaDeClaims(UserManager<CredencialUsuario> credenciales, IOptions<IdentityOptions> opciones, PolyDbContext db)
    : UserClaimsPrincipalFactory<CredencialUsuario>(credenciales, opciones)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(CredencialUsuario user)
    {
        var identidad = await base.GenerateClaimsAsync(user);
        var usuario = await db.Usuarios.AsNoTracking().IgnoreQueryFilters().SingleAsync(u => u.Id == user.UserId);
        identidad.AddClaim(new Claim(ClaimsDeSesion.UserId, usuario.Id.ToString(CultureInfo.InvariantCulture)));
        identidad.AddClaim(new Claim(ClaimsDeSesion.UserName, usuario.UserName));
        identidad.AddClaim(new Claim(ClaimsDeSesion.NombreVisible, usuario.DisplayName));
        return identidad;
    }
}

/// <summary>En cada petición con cookie: si el usuario se archivó, la sesión deja de valer (US2, escenario 5).</summary>
public static class ValidacionDeSesion
{
    public static async Task<bool> UsuarioActivoAsync(ClaimsPrincipal principal, PolyDbContext db, CancellationToken ct)
    {
        if (!long.TryParse(principal.FindFirst(ClaimsDeSesion.UserId)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return false;
        return await db.Usuarios.AnyAsync(u => u.Id == id, ct);
    }
}
