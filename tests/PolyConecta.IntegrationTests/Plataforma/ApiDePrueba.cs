using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure;
using PolyConecta.Infrastructure.Common;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.Tests.Compartido;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>La API real contra la base de un <see cref="Entorno"/>, con el documento de prueba registrado.</summary>
public sealed class ApiDePrueba(Entorno entorno, IDictionary<string, string?>? ajustes = null) : WebApplicationFactory<Program>
{
    public const string Secreto = "secreto-callback";

    /// <summary>Contraseña del Administrador inicial que siembra la API (Seguridad__AdministradorInicial__Contrasena).</summary>
    public const string ContrasenaAdmin = "Admin2026x";

    /// <summary>Contraseña de los usuarios que crean las pruebas.</summary>
    public const string Contrasena = "Prueba2026x";

    /// <summary>
    /// Cliente con sesión (FR-009): entra con el usuario y manda X-Requested-With en toda petición, como
    /// la web. La cookie la guarda el cliente de prueba.
    /// </summary>
    public async Task<HttpClient> ClienteAsync(string usuario = "admin", string? contrasena = null)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Requested-With", "PolyConecta");
        var r = await cliente.PostAsync("/api/v1/plataforma/sesion", System.Net.Http.Json.JsonContent.Create(new
        {
            usuario,
            contrasena = contrasena ?? (usuario == "admin" ? ContrasenaAdmin : Contrasena),
        }));
        if (!r.IsSuccessStatusCode)
            throw new InvalidOperationException($"No se pudo entrar como {usuario}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return cliente;
    }

    /// <summary>Crea un usuario con sus grupos por planta (códigos) y la contraseña <see cref="Contrasena"/>.</summary>
    public async Task<long> CrearUsuarioAsync(string usuario, string nombre, params (string Grupo, string Planta, bool Suplente)[] grupos)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PolyDbContext>();
        var idsGrupo = await db.Groups.ToDictionaryAsync(g => g.Code, g => g.Id);
        var idsPlanta = await db.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);
        var user = new PolyConecta.Domain.Plataforma.Seguridad.User(usuario, nombre, null,
            grupos.Select(g => new PolyConecta.Domain.Plataforma.Seguridad.AsignacionSolicitada(idsGrupo[g.Grupo], idsPlanta[g.Planta], g.Suplente)));
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        var credenciales = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<PolyConecta.Infrastructure.Plataforma.Identidad.CredencialUsuario>>();
        var r = await credenciales.CreateAsync(new PolyConecta.Infrastructure.Plataforma.Identidad.CredencialUsuario
        {
            UserName = usuario, UserId = user.Id, LockoutEnabled = true,
        }, Contrasena);
        if (!r.Succeeded) throw new InvalidOperationException(string.Join(" ", r.Errors.Select(e => e.Description)));
        return user.Id;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PolyConecta", entorno.Conexion);
        builder.UseSetting("Erp:CallbackSecret", Secreto);
        builder.UseSetting("Seguridad:AdministradorInicial:Contrasena", ContrasenaAdmin);
        // La sincronización periódica no corre en las pruebas: cada prueba la pide cuando la necesita.
        builder.UseSetting("Erp:Sincronizacion:Habilitada", "false");
        foreach (var (clave, valor) in ajustes ?? new Dictionary<string, string?>())
            builder.UseSetting(clave, valor);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IClock>(entorno.Reloj);
            services.AddScoped<PolyDbContext>(sp => new PruebasDbContext(
                new DbContextOptionsBuilder<PolyDbContext>()
                    .UseSqlServer(entorno.Conexion)
                    .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>())
                    .Options));
            services.AddScoped(sp => (PruebasDbContext)sp.GetRequiredService<PolyDbContext>());
            services.AddDocumentoSincronizable<DocumentoDePrueba, TraductorDocumentoDePrueba>();
        });
    }
}
