using System;
using System.Globalization;

namespace Contpaq.Bridge.Infrastructure.Outbox
{
    /// <summary>
    /// Reinicio diario del bridge (FR-006, R-07, L1-T004): a la hora configurada (BridgeConfig__ReinicioDiario,
    /// HH:mm en hora local, 03:00 por omisión) el worker deja de tomar transacciones, termina la actual,
    /// cierra empresa y SDK y sale con código 0; el supervisor (scripts/vps/Start-BridgeSupervisado.ps1) lo vuelve a levantar. La hora se
    /// compara con la del arranque del proceso: el reinicio llega a la primera ocurrencia de la hora
    /// después de arrancar, así un proceso recién levantado a las 03:00:05 no vuelve a salir.
    /// </summary>
    public sealed class ReinicioDiario
    {
        public const string HoraPorOmision = "03:00";

        private readonly TimeProvider _reloj;
        private readonly DateTimeOffset _proximo;

        public ReinicioDiario(string? hora, TimeProvider reloj)
        {
            _reloj = reloj;
            var texto = string.IsNullOrWhiteSpace(hora) ? HoraPorOmision : hora.Trim();
            if (!TimeOnly.TryParseExact(texto, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var h))
                throw new ArgumentException($"BridgeConfig:ReinicioDiario debe ser HH:mm en hora local, no '{hora}'.", nameof(hora));
            Hora = h;

            var arranque = reloj.GetLocalNow();
            var candidato = Local(arranque.Date + h.ToTimeSpan());
            _proximo = candidato > arranque ? candidato : Local(arranque.Date.AddDays(1) + h.ToTimeSpan());
        }

        public TimeOnly Hora { get; }

        /// <summary>Cuándo se reinicia el proceso que arrancó ahora.</summary>
        public DateTimeOffset Proximo => _proximo;

        /// <summary>Ya llegó la hora del reinicio: no hay que tomar más transacciones.</summary>
        public bool Debido() => _reloj.GetUtcNow() >= _proximo;

        private DateTimeOffset Local(DateTime sinZona)
        {
            var fecha = DateTime.SpecifyKind(sinZona, DateTimeKind.Unspecified);
            return new DateTimeOffset(fecha, _reloj.LocalTimeZone.GetUtcOffset(fecha));
        }
    }

    /// <summary>Cómo sale el proceso: el worker no llama a Environment.Exit directo para poder probarlo.</summary>
    public interface ICierreProceso
    {
        void Salir(int codigo);
    }

    public sealed class CierreProceso : ICierreProceso
    {
        public void Salir(int codigo) => Environment.Exit(codigo);
    }
}
