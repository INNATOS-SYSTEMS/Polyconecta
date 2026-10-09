using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PolyConecta.Infrastructure.Common;

namespace PolyConecta.Infrastructure.Plataforma.Identidad;

/// <summary>Claims que la cookie de sesión lleva desde que se entra (R-01).</summary>
public static class ClaimsDeSesion
{
    public const string UserId = "pc:user_id";
    public const string UserName = "pc:user_name";
    public const string NombreVisible = "pc:display_name";
}

/// <summary>
/// Usuario de la petición, leído de los claims de la cookie. No consulta la base: el interceptor de
/// auditoría depende de <c>ICurrentUser</c> y el contexto depende del interceptor. Sin petición o sin
/// sesión se comporta como "sistema".
/// </summary>
public sealed class CurrentUserDesdeCookie(IHttpContextAccessor http) : SistemaCurrentUser
{
    private ClaimsPrincipal? Principal =>
        http.HttpContext?.User is { Identity.IsAuthenticated: true } p ? p : null;

    public override string UserName => Principal?.FindFirst(ClaimsDeSesion.UserName)?.Value ?? base.UserName;

    public override long? UserId =>
        long.TryParse(Principal?.FindFirst(ClaimsDeSesion.UserId)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public override string NombreVisible => Principal?.FindFirst(ClaimsDeSesion.NombreVisible)?.Value ?? base.NombreVisible;
}
