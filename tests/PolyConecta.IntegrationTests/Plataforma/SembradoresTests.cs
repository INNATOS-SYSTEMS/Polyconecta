using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.Infrastructure.Persistence.Sembradores;
using PolyConecta.Infrastructure.Plataforma.Identidad;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>Migración F1_Seguridad y siembra de seguridad al arrancar la API (L2-T003, L2-T007).</summary>
public class SembradoresTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Al_arrancar_siembra_plantas_permisos_grupos_y_el_Administrador_una_sola_vez()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var api = new ApiDePrueba(entorno);
        _ = api.Services; // arranca el host y siembra
        await api.Services.SembrarAsync(); // una segunda vez no duplica nada

        await using var db = entorno.Contexto();
        (await db.Plants.Select(p => p.Code).ToListAsync()).Should().BeEquivalentTo(["PIM", "SC", "MTM"]);
        (await db.Permissions.CountAsync()).Should().Be(Permisos.Catalogo.Count);
        (await db.Groups.CountAsync()).Should().Be(10);

        var comercial = await db.Groups.SingleAsync(g => g.Code == GruposIniciales.Comercial);
        var claves = await db.Permissions.Where(p => comercial.Permissions.Select(x => x.PermissionId).Contains(p.Id))
            .Select(p => p.Key).ToListAsync();
        claves.Should().BeEquivalentTo(GruposIniciales.Catalogo.Single(g => g.Codigo == GruposIniciales.Comercial).Permisos);

        var admin = await db.Usuarios.SingleAsync();
        admin.UserName.Should().Be("admin");
        admin.Assignments.Should().HaveCount(2);
        await using var scope = api.Services.CreateAsyncScope();
        var credenciales = scope.ServiceProvider.GetRequiredService<UserManager<CredencialUsuario>>();
        var credencial = await credenciales.FindByNameAsync("admin");
        credencial!.UserId.Should().Be(admin.Id);
        (await credenciales.CheckPasswordAsync(credencial, ApiDePrueba.ContrasenaAdmin)).Should().BeTrue();
    }

    [Fact]
    public async Task Un_permiso_que_sale_del_codigo_se_archiva_y_los_grupos_editados_no_se_resiembran()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var api = new ApiDePrueba(entorno);
        _ = api.Services;

        await using (var db = entorno.Contexto())
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO plt.permission ([Key], Module, [Object], [Action], ObjectKind, ModuleLabel, ObjectLabel, Label, IsActive, CreatedAt, CreatedBy) " +
                "VALUES ('viejo.cosa.hacer', 'viejo', 'cosa', 'hacer', 'Funcionalidad', 'V', 'C', 'H', 1, SYSDATETIMEOFFSET(), 'x')");
            var sistemas = await db.Groups.SingleAsync(g => g.Code == GruposIniciales.Sistemas);
            sistemas.AsignarPermisos([]);
            await db.SaveChangesAsync();
        }

        await api.Services.SembrarAsync();

        await using var verificar = entorno.Contexto();
        (await verificar.Permissions.IgnoreQueryFilters().SingleAsync(p => p.Key == "viejo.cosa.hacer")).IsActive.Should().BeFalse();
        (await verificar.Groups.SingleAsync(g => g.Code == GruposIniciales.Sistemas)).Permissions.Should().BeEmpty();
    }
}
