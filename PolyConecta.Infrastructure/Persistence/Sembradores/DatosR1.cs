using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Infrastructure.Plataforma.Identidad;

namespace PolyConecta.Infrastructure.Persistence.Sembradores;

/// <summary>
/// Usuarios de ejemplo de la revisión R1 (quickstart, tabla de usuarios de R1; L2-T033). Solo en
/// desarrollo o con <c>Seguridad__SembrarDatosR1=true</c>, y con la contraseña de
/// <c>Seguridad__DatosR1__Contrasena</c> (CT-29: ninguna contraseña en el repositorio; <c>run.sh</c> la genera en
/// <c>.env.local</c>). No toca a un usuario que ya existe. Corre después de <see cref="SembradorSeguridad"/>.
/// </summary>
public sealed partial class DatosR1(
    PolyDbContext db,
    UserManager<CredencialUsuario> credenciales,
    IConfiguration configuracion,
    IHostEnvironment entorno,
    ILogger<DatosR1> logger) : IDataSeeder
{
    private sealed record UsuarioR1(string Usuario, string Nombre, params (string Grupo, bool Suplente)[] Grupos);

    private static readonly UsuarioR1[] Usuarios =
    [
        new("sistemas", "Sergio Sistemas", (GruposIniciales.Sistemas, false)),
        new("ac1", "Celia Villarreal", (GruposIniciales.AtencionClientes, false)),
        new("comercial1", "Ana Treviño", (GruposIniciales.Comercial, false)),
        new("cobranza1", "Beto Garza", (GruposIniciales.Cobranza, false)),
        new("cobranza-suplente", "Carlos Suplente", (GruposIniciales.Cobranza, true)),
        new("doble", "Daniel Doble", (GruposIniciales.Comercial, false), (GruposIniciales.Cobranza, false)),
        new("planner-pim", "Pedro Planner", (GruposIniciales.Planner, false)),
    ];

    /// <summary>Planta de todas las asignaciones de R1.</summary>
    public const string Planta = "PIM";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!entorno.IsDevelopment() && !configuracion.GetValue<bool>("Seguridad:SembrarDatosR1")) return;
        var contrasena = configuracion["Seguridad:DatosR1:Contrasena"];
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            LogSinContrasena(logger);
            return;
        }

        var existentes = await db.Usuarios.IgnoreQueryFilters().Select(u => u.UserName).ToListAsync(cancellationToken);
        var grupos = await db.Groups.ToDictionaryAsync(g => g.Code, g => g.Id, cancellationToken);
        var planta = await db.Plants.Where(p => p.Code == Planta).Select(p => p.Id).SingleAsync(cancellationToken);
        foreach (var r1 in Usuarios.Where(u => !existentes.Contains(u.Usuario)))
        {
            var usuario = new User(r1.Usuario, r1.Nombre, null,
                r1.Grupos.Select(g => new AsignacionSolicitada(grupos[g.Grupo], planta, g.Suplente)));
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync(cancellationToken);
            var resultado = await credenciales.CreateAsync(
                new CredencialUsuario { UserName = r1.Usuario, UserId = usuario.Id, LockoutEnabled = true }, contrasena);
            if (!resultado.Succeeded)
                throw new InvalidOperationException($"La contraseña de los usuarios de R1 no cumple la política: "
                    + string.Join(" ", resultado.Errors.Select(e => e.Description)));
            LogUsuarioCreado(logger, r1.Usuario);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No se sembraron los usuarios de R1: falta Seguridad__DatosR1__Contrasena.")]
    private static partial void LogSinContrasena(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Usuario de R1 creado: {Usuario}.")]
    private static partial void LogUsuarioCreado(ILogger logger, string usuario);
}
