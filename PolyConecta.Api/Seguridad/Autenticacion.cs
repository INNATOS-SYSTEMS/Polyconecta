using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.Infrastructure.Plataforma.Identidad;

namespace PolyConecta.Api.Seguridad;

/// <summary>
/// Sesión con cookie (R-01): HttpOnly, SameSite=Lax, deslizante de 10 horas, 401 en vez de redirigir y
/// toda ruta con sesión salvo las marcadas <c>[AllowAnonymous]</c> (entrar y el callback del bridge, FR-009).
/// </summary>
public static class Autenticacion
{
    public const string EncabezadoAntiFalsificacion = "X-Requested-With";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddAutenticacionPolyConecta(this IServiceCollection services, IHostEnvironment entorno)
    {
        services.AddIdentityCore<CredencialUsuario>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<FabricaDeClaims>();
        services.AddScoped<ServicioDeSesion>();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies(cookies => cookies.ApplicationCookie!.Configure(o =>
            {
                o.Cookie.Name = "polyconecta.sesion";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = entorno.IsDevelopment() || entorno.IsEnvironment("Testing")
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                o.ExpireTimeSpan = TimeSpan.FromHours(10);
                o.SlidingExpiration = true;
                o.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = c => EscribirAsync(c.Response, StatusCodes.Status401Unauthorized, "SIN_SESION", "Inicia sesión para continuar."),
                    OnRedirectToAccessDenied = c => EscribirAsync(c.Response, StatusCodes.Status403Forbidden, "PERMISO_DENEGADO", "No tienes permiso."),
                    OnValidatePrincipal = async c =>
                    {
                        var db = c.HttpContext.RequestServices.GetRequiredService<PolyDbContext>();
                        if (c.Principal is null || !await ValidacionDeSesion.UsuarioActivoAsync(c.Principal, db, c.HttpContext.RequestAborted))
                            c.RejectPrincipal();
                    },
                };
            }));

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    /// <summary>
    /// Protección de la cookie contra CSRF (R-01): toda petición a /api que escribe lleva
    /// <c>X-Requested-With</c>, salvo el callback del bridge, que se autentica con su firma.
    /// </summary>
    public static IApplicationBuilder UseEncabezadoAntiFalsificacion(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            var p = contexto.Request;
            var escribe = !HttpMethods.IsGet(p.Method) && !HttpMethods.IsHead(p.Method) && !HttpMethods.IsOptions(p.Method);
            if (escribe && p.Path.StartsWithSegments("/api")
                && !p.Path.StartsWithSegments("/api/v1/plataforma/bridge/callbacks")
                && string.IsNullOrWhiteSpace(p.Headers[EncabezadoAntiFalsificacion]))
            {
                await EscribirAsync(contexto.Response, StatusCodes.Status400BadRequest, "FALTA_X_REQUESTED_WITH",
                    "Toda petición que escribe lleva el encabezado X-Requested-With.");
                return;
            }
            await siguiente(contexto);
        });

    public static Task EscribirAsync(HttpResponse respuesta, int estado, string codigo, string razon)
    {
        respuesta.StatusCode = estado;
        respuesta.ContentType = "application/problem+json";
        return respuesta.WriteAsync(JsonSerializer.Serialize(new { status = estado, title = codigo, code = codigo, razon }, Json));
    }
}
