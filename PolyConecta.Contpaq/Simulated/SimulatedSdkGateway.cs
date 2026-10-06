using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Microsoft.Extensions.Configuration;

namespace Contpaq.Bridge.Simulated
{
    /// <summary>
    /// Adaptador de escritura del modo simulado (CT-21, D-122): asigna folios simulados por
    /// concepto y devuelve los documentos que crearía CONTPAQi, sin SDK. La validación, la
    /// idempotencia y el callback son el mismo código que en modo real. Las existencias del
    /// catálogo semilla no se descuentan.
    /// </summary>
    public sealed class SimulatedSdkGateway(
        SimulatedStore store, ConfiguracionConceptos conceptos, FaultStore fallos, IReadRepository lecturas, IConfiguration config)
        : ISdkGateway
    {
        private readonly int _demoraMs = int.TryParse(config["BridgeConfig:Simulated:DelayMs"], out var d) ? d : 20;

        public bool EsReal => false;

        public bool SesionActiva => true;

        public bool AsegurarSesion() => true;

        public void CerrarSiInactiva() { }

        public void Apagar() { }

        public void BombearMensajes() { }

        public ResultadoEjecucion Ejecutar(string transactionId, ComandoLeido comando)
        {
            var regla = fallos.Tomar(comando.CommandType, Referencia(comando.Carga));
            Thread.Sleep(regla?.DelayMs ?? _demoraMs);
            if (regla?.ErrorCode is { } codigo)
                return ResultadoEjecucion.Fallo(ErrorContrato.De(codigo, $"Error simulado ({codigo}).", new() { ["simulado"] = true }))
                    with { OmitirCallback = regla.DropCallback };

            var resultado = comando.Carga switch
            {
                CargaTraspaso => Documentos(comando, "salida", "entrada"),
                CargaCierreProduccion c => Documentos(comando,
                    new[] { "consumo", "entrada" }.Concat(c.Subproductos!.Count > 0 ? new[] { "subproducto" } : new string[0]).ToArray()),
                CargaAltaPedido => Unico(comando, "pedido"),
                CargaRemision m => Remision(comando, m),
                CargaAltaAlmacen a => AltaAlmacen(a),
                _ => ResultadoEjecucion.Fallo(ErrorContrato.De(CodigosError.SdkError, "Comando sin simulación.")),
            };
            return resultado with { OmitirCallback = regla?.DropCallback == true };
        }

        private ResultadoEjecucion Documentos(ComandoLeido comando, params string[] roles) =>
            ResultadoEjecucion.Exito(new Resultado
            {
                Documentos = roles.Select(r => store.NuevoDocumento(r, conceptos.Concepto(comando.CommandType, comando.Variant, r)!)).ToList(),
            });

        private ResultadoEjecucion Unico(ComandoLeido comando, string rol)
        {
            var doc = store.NuevoDocumento(rol, conceptos.Concepto(comando.CommandType, comando.Variant, rol)!);
            return ResultadoEjecucion.Exito(new Resultado { Folio = doc.Folio, IdErp = doc.IdErp });
        }

        private ResultadoEjecucion Remision(ComandoLeido comando, CargaRemision carga)
        {
            var doc = store.NuevoDocumento("remision", conceptos.Concepto(comando.CommandType, comando.Variant, "remision")!);
            return ResultadoEjecucion.Exito(new Resultado { Folio = doc.Folio, IdErp = doc.IdErp, PedidoCancelado = carga.CierraPedido == true });
        }

        /// <summary>Mismo código y nombre: devuelve el existente. Otro nombre: ALMACEN_YA_EXISTE (§5.3).</summary>
        private ResultadoEjecucion AltaAlmacen(CargaAltaAlmacen carga)
        {
            var existente = lecturas.AlmacenAsync(carga.Codigo).GetAwaiter().GetResult();
            if (existente is not null)
            {
                return existente.Nombre == carga.Nombre
                    ? ResultadoEjecucion.Exito(new Resultado { IdErp = existente.IdErp })
                    : ResultadoEjecucion.Fallo(ErrorContrato.De(CodigosError.AlmacenYaExiste,
                        $"El almacén {carga.Codigo} ya existe con el nombre '{existente.Nombre}'.",
                        new() { ["almacen"] = carga.Codigo, ["nombre_existente"] = existente.Nombre }));
            }
            return ResultadoEjecucion.Exito(new Resultado { IdErp = store.CrearAlmacen(carga.Codigo, carga.Nombre).IdErp });
        }

        private static string? Referencia(ICarga carga) => carga switch
        {
            CargaTraspaso t => t.ReferenciaNegocio,
            CargaAltaPedido p => p.ReferenciaNegocio,
            CargaCierreProduccion c => c.ReferenciaNegocio,
            CargaRemision m => m.ReferenciaNegocio,
            CargaAltaAlmacen a => a.Codigo,
            _ => null,
        };
    }
}
