using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PolyConecta.Application;
using PolyConecta.Application.Common;
using Xunit;

namespace PolyConecta.Application.Tests.Common;

public class DecoradoresTests
{
    private sealed record Peticion(string Valor);

    private sealed class CasoDePrueba(List<string> bitacora) : IUseCase<Peticion, string>
    {
        public bool Fallar { get; set; }

        public Task<string> ExecuteAsync(Peticion request, CancellationToken cancellationToken = default)
        {
            bitacora.Add("caso");
            if (Fallar) throw new InvalidOperationException("falla de negocio");
            return Task.FromResult(request.Valor.ToUpperInvariant());
        }
    }

    private sealed class ValidadorNoVacio : IValidator<Peticion>
    {
        public IEnumerable<ErrorValidacion> Validate(Peticion request) =>
            string.IsNullOrEmpty(request.Valor) ? [new ErrorValidacion("Valor", "es obligatorio")] : [];
    }

    private sealed class UnidadDeTrabajoFalsa(List<string> bitacora) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            bitacora.Add("begin");
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = false;
            bitacora.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = false;
            bitacora.Add("rollback");
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            bitacora.Add("save");
            return Task.FromResult(0);
        }
    }

    private sealed class Correlacion : ICorrelationContext
    {
        public string CorrelationId => "prueba";
    }

    private sealed class SinPermisos : IPermisosDelUsuario
    {
        public Task<IReadOnlyList<AsignacionEfectiva>> AsignacionesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AsignacionEfectiva>>([]);
    }

    private static (IUseCase<Peticion, string> UseCase, List<string> Bitacora, CasoDePrueba Caso) Construir()
    {
        var bitacora = new List<string>();
        var caso = new CasoDePrueba(bitacora);
        var services = new ServiceCollection()
            .AddSingleton(caso)
            .AddSingleton<IUnitOfWork>(new UnidadDeTrabajoFalsa(bitacora))
            .AddSingleton<ICorrelationContext, Correlacion>()
            .AddSingleton<IValidator<Peticion>, ValidadorNoVacio>()
            // La cadena de decoradores incluye la autorización (L2-T005); esta petición no exige permiso.
            .AddSingleton<ICurrentUser>(new PolyConecta.Tests.Compartido.UsuarioFijo("sistema"))
            .AddSingleton<IPermisosDelUsuario, SinPermisos>()
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddUseCase<Peticion, string, CasoDePrueba>();
        // AddUseCase registra el caso como Scoped; la instancia de prueba se resuelve igual.
        services.AddScoped(_ => caso);
        var proveedor = services.BuildServiceProvider();
        return (proveedor.CreateScope().ServiceProvider.GetRequiredService<IUseCase<Peticion, string>>(), bitacora, caso);
    }

    [Fact]
    public async Task Exito_AbreGuardaYConfirmaAlrededorDelCaso()
    {
        var (useCase, bitacora, _) = Construir();

        var resultado = await useCase.ExecuteAsync(new Peticion("hola"), TestContext.Current.CancellationToken);

        resultado.Should().Be("HOLA");
        bitacora.Should().Equal("begin", "caso", "save", "commit");
    }

    [Fact]
    public async Task Falla_DelCaso_RevierteSinGuardar()
    {
        var (useCase, bitacora, caso) = Construir();
        caso.Fallar = true;

        var accion = () => useCase.ExecuteAsync(new Peticion("hola"), TestContext.Current.CancellationToken);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        bitacora.Should().Equal("begin", "caso", "rollback");
    }

    [Fact]
    public async Task PeticionInvalida_NoAbreTransaccionNiEjecuta()
    {
        var (useCase, bitacora, _) = Construir();

        var accion = () => useCase.ExecuteAsync(new Peticion(""), TestContext.Current.CancellationToken);

        (await accion.Should().ThrowAsync<ValidacionException>()).Which.Errores.Should().ContainSingle();
        bitacora.Should().BeEmpty();
    }
}
