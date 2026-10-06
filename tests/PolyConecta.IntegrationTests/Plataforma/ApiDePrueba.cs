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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PolyConecta", entorno.Conexion);
        builder.UseSetting("Erp:CallbackSecret", Secreto);
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
