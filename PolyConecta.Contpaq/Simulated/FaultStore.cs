using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Contpaq.Bridge.Simulated
{
    /// <summary>
    /// Regla de fallo del modo simulado (FR-008 de la spec 002): provoca un error, una demora o un
    /// callback perdido sin cambiar código. Aplica al comando y, si se indica, a una referencia.
    /// </summary>
    public sealed class FaultRule
    {
        [JsonPropertyName("command_type")] public string CommandType { get; set; } = string.Empty;
        [JsonPropertyName("referencia_negocio")] public string? ReferenciaNegocio { get; set; }
        [JsonPropertyName("error_code")] public string? ErrorCode { get; set; }
        [JsonPropertyName("delay_ms")] public int? DelayMs { get; set; }
        [JsonPropertyName("drop_callback")] public bool DropCallback { get; set; }
        /// <summary>Cuántas veces aplica; sin valor, siempre.</summary>
        [JsonPropertyName("veces")] public int? Veces { get; set; }
    }

    /// <summary>
    /// Reglas vigentes. Se leen de BridgeConfig__Simulated__Faults al arrancar y se reemplazan con
    /// PUT /admin/simulated/faults, que solo existe en modo simulado.
    /// </summary>
    public sealed class FaultStore
    {
        private readonly object _lock = new();
        private List<FaultRule> _reglas;

        public FaultStore(IConfiguration config)
        {
            _reglas = config.GetSection("BridgeConfig:Simulated:Faults").Get<List<FaultRule>>() ?? new List<FaultRule>();
        }

        public IReadOnlyList<FaultRule> Reglas
        {
            get { lock (_lock) return _reglas.ToList(); }
        }

        public void Reemplazar(IEnumerable<FaultRule> reglas)
        {
            lock (_lock) _reglas = reglas.ToList();
        }

        /// <summary>La primera regla que aplica, descontando una vez si está limitada.</summary>
        public FaultRule? Tomar(string commandType, string? referenciaNegocio)
        {
            lock (_lock)
            {
                var regla = _reglas.FirstOrDefault(r =>
                    r.CommandType == commandType &&
                    (r.ReferenciaNegocio is null || r.ReferenciaNegocio == referenciaNegocio) &&
                    r.Veces is null or > 0);
                if (regla?.Veces is not null) regla.Veces--;
                return regla;
            }
        }
    }
}
