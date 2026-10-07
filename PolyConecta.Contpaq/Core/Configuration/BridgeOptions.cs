using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace Contpaq.Bridge.Core.Configuration
{
    public enum BridgeMode
    {
        Real,
        Simulated,
    }

    /// <summary>Configuración del bridge que comparten los dos modos.</summary>
    public sealed class BridgeOptions
    {
        public BridgeMode Mode { get; init; }

        /// <summary>Secreto compartido con PolyConecta para firmar los callbacks (D-121, CT-29).</summary>
        public string CallbackSecret { get; init; } = string.Empty;

        public int MaxRetries { get; init; } = 5;

        /// <summary>
        /// BridgeConfig__Mode=Simulated|Real. Sin valor: Real en Windows y Simulated en otro sistema,
        /// porque fuera de Windows no hay SDK (D-122).
        /// </summary>
        public static BridgeOptions Leer(IConfiguration config)
        {
            var modo = config["BridgeConfig:Mode"];
            var bridgeMode = string.IsNullOrWhiteSpace(modo)
                ? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? BridgeMode.Real : BridgeMode.Simulated)
                : Enum.Parse<BridgeMode>(modo, ignoreCase: true);
            return new BridgeOptions
            {
                Mode = bridgeMode,
                CallbackSecret = config["BridgeConfig:CallbackSecret"] ?? string.Empty,
                MaxRetries = int.TryParse(config["BridgeConfig:MaxRetries"], out var m) ? m : 5,
            };
        }
    }

    /// <summary>
    /// Conceptos de CONTPAQi por comando, variante y rol del documento (D-121). PolyConecta no los
    /// manda: los elige el bridge. Clave: BridgeConfig:Conceptos:{COMANDO}:{VARIANTE o _}:{rol}.
    /// </summary>
    public sealed class ConfiguracionConceptos(IConfiguration config)
    {
        public static readonly IReadOnlyDictionary<string, string[]> RolesPorComando = new Dictionary<string, string[]>
        {
            ["TRASPASO"] = new[] { "salida", "entrada" },
            ["ALTA_PEDIDO"] = new[] { "pedido" },
            ["CIERRE_PRODUCCION"] = new[] { "consumo", "entrada", "subproducto" },
            ["REMISION"] = new[] { "remision" },
            ["ALTA_ALMACEN"] = Array.Empty<string>(),
        };

        public string? Concepto(string comando, string? variante, string rol) =>
            config[$"BridgeConfig:Conceptos:{comando}:{(string.IsNullOrEmpty(variante) ? "_" : variante)}:{rol}"];

        /// <summary>Roles sin concepto configurado para el comando y la variante.</summary>
        public IReadOnlyList<string> RolesSinConcepto(string comando, string? variante) =>
            RolesPorComando.TryGetValue(comando, out var roles)
                ? roles.Where(r => string.IsNullOrWhiteSpace(Concepto(comando, variante, r))).ToList()
                : Array.Empty<string>();

        /// <summary>CIDMONEDA de un código ISO. admMonedas no guarda el ISO (contrato §5.2).</summary>
        public int? IdMoneda(string codigoIso) =>
            int.TryParse(config[$"BridgeConfig:Monedas:{codigoIso}"], out var id) ? id : null;
    }
}
