using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Infrastructure.Plataforma.Identidad;

namespace PolyConecta.Infrastructure.Persistence.Sembradores;

/// <summary>
/// Datos iniciales de seguridad (data-model §5): plantas, el catálogo de permisos sincronizado con
/// <see cref="Permisos"/>, los diez grupos con su matriz (solo si no hay grupos) y el Administrador
/// inicial con la contraseña de <c>Seguridad__AdministradorInicial__Contrasena</c> (CT-29).
/// </summary>
public sealed partial class SembradorSeguridad(
    PolyDbContext db,
    UserManager<CredencialUsuario> credenciales,
    IConfiguration configuracion,
    ILogger<SembradorSeguridad> logger) : IDataSeeder
{
    public const string UsuarioAdministrador = "admin";

    public static readonly (string Codigo, string Nombre)[] Plantas =
    [
        ("PIM", "PIM (Apodaca)"),
        ("SC", "Santa Cruz"),
        ("MTM", "Montemorelos"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SembrarPlantasAsync(cancellationToken);
        await SincronizarPermisosAsync(cancellationToken);
        await SembrarGruposAsync(cancellationToken);
        await SembrarAdministradorAsync(cancellationToken);
    }

    private async Task SembrarPlantasAsync(CancellationToken ct)
    {
        var existentes = await db.Plants.IgnoreQueryFilters().Select(p => p.Code).ToListAsync(ct);
        foreach (var (codigo, nombre) in Plantas.Where(p => !existentes.Contains(p.Codigo)))
            db.Plants.Add(new Plant(codigo, nombre));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Agrega lo nuevo, actualiza etiquetas y archiva lo que ya no está en el código (R-02).</summary>
    private async Task SincronizarPermisosAsync(CancellationToken ct)
    {
        var actuales = await db.Permissions.IgnoreQueryFilters().ToListAsync(ct);
        foreach (var definicion in Permisos.Catalogo)
        {
            var permiso = actuales.FirstOrDefault(p => p.Key == definicion.Clave);
            if (permiso is null)
            {
                db.Permissions.Add(new Permission(definicion));
                continue;
            }
            permiso.Actualizar(definicion);
            if (!permiso.IsActive) permiso.Restore();
        }
        foreach (var sobrante in actuales.Where(p => p.IsActive && Permisos.Buscar(p.Key) is null))
            sobrante.Archive();
        await db.SaveChangesAsync(ct);
    }

    private async Task SembrarGruposAsync(CancellationToken ct)
    {
        if (await db.Groups.IgnoreQueryFilters().AnyAsync(ct)) return;
        var permisos = await db.Permissions.ToDictionaryAsync(p => p.Key, p => p.Id, ct);
        foreach (var definicion in GruposIniciales.Catalogo)
        {
            var grupo = new Group(definicion.Codigo, definicion.Nombre, definicion.Descripcion);
            grupo.AsignarPermisos(definicion.Permisos.Select(k => permisos[k]));
            db.Groups.Add(grupo);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SembrarAdministradorAsync(CancellationToken ct)
    {
        var contrasena = configuracion["Seguridad:AdministradorInicial:Contrasena"];
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.UserName == UsuarioAdministrador, ct)) return;
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            LogSinAdministrador(logger);
            return;
        }

        var admin = await db.Groups.SingleAsync(g => g.Code == GruposIniciales.Administrador, ct);
        var plantas = await db.Plants.Where(p => p.Code == "PIM" || p.Code == "SC").Select(p => p.Id).ToListAsync(ct);
        var usuario = new User(UsuarioAdministrador, "Administrador", null,
            plantas.Select(p => new AsignacionSolicitada(admin.Id, p, false)));
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);

        var resultado = await credenciales.CreateAsync(
            new CredencialUsuario { UserName = UsuarioAdministrador, UserId = usuario.Id, LockoutEnabled = true }, contrasena);
        if (!resultado.Succeeded)
            throw new InvalidOperationException("La contraseña del Administrador inicial no cumple la política: "
                + string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No se creó el Administrador inicial: falta Seguridad__AdministradorInicial__Contrasena.")]
    private static partial void LogSinAdministrador(ILogger logger);
}
