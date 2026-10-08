using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;

namespace Contpaq.Bridge.Infrastructure.Persistence
{
    /// <summary>
    /// Lecturas del contrato (§6) y búsquedas de las validaciones (CT-39). Tiene dos adaptadores,
    /// real y simulado, que se eligen con BridgeConfig__Mode (D-122).
    /// </summary>
    public interface IReadRepository
    {
        Task<Pagina<ProductoContrato>> ProductosAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor);

        Task<ProductoContrato?> ProductoAsync(string codigo);

        Task<Pagina<ClienteContrato>> ClientesAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor);

        Task<ClienteContrato?> ClienteAsync(string codigo);

        /// <summary>Desde 1.1 (D-153). El cursor es el id del último agente de la página anterior.</summary>
        Task<Pagina<AgenteContrato>> AgentesAsync(int limit, string? cursor);

        Task<AgenteContrato?> AgenteAsync(string codigo);

        Task<IReadOnlyList<AlmacenContrato>> AlmacenesAsync();

        Task<AlmacenContrato?> AlmacenAsync(string codigo);

        /// <summary>Existencias en la unidad base de cada producto, por almacén y lote.</summary>
        Task<IReadOnlyList<ExistenciaContrato>> ExistenciasAsync(IReadOnlyCollection<string> productos, string? almacen);

        Task<Pagina<RecepcionCompraContrato>> RecepcionesCompraAsync(DateTimeOffset? modifiedSince, int limit, string? cursor);
    }

    /// <summary>La lectura todavía no existe en este modo. La API responde 501.</summary>
    public sealed class LecturaNoDisponibleException(string mensaje) : NotSupportedException(mensaje);
}

namespace Contpaq.Bridge.Infrastructure.Persistence
{
    /// <summary>Ningún ejercicio de admEjercicios contiene la fecha de hoy: F-01 no puede leer la existencia por producto (D-156).</summary>
    public sealed class EjercicioVigenteException() : InvalidOperationException(
        "Ningún ejercicio de CONTPAQi contiene la fecha de hoy; la existencia por producto no se puede leer.");
}
