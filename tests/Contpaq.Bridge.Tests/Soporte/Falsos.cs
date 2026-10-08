using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Contpaq.Bridge.Infrastructure.Outbox;
using Contpaq.Bridge.Infrastructure.Sdk;

namespace Contpaq.Bridge.Tests.Soporte
{
    /// <summary>SDK nativo falso: anota cada llamada, en orden, y deja programar qué responde o si se bloquea.</summary>
    public sealed class SdkNativoFalso : ISdkNativo
    {
        private readonly object _candado = new();
        private readonly List<string> _llamadas = new();

        public IReadOnlyList<string> Llamadas { get { lock (_candado) return _llamadas.ToList(); } }

        public int Veces(string llamada) => Llamadas.Count(l => l == llamada);

        public string? MotivoPreparar { get; set; }

        public int ResultadoSetNombrePaq { get; set; }

        public int ResultadoInicializa { get; set; }

        public int ResultadoAbreEmpresa { get; set; }

        /// <summary>Si se asigna, AbreEmpresa se queda esperando a que se libere (una llamada que no regresa).</summary>
        public ManualResetEventSlim? BloquearAbreEmpresa { get; set; }

        private void Anotar(string llamada) { lock (_candado) _llamadas.Add(llamada); }

        public string? Preparar(string sdkPath) { Anotar("Preparar"); return MotivoPreparar; }

        public int InicializaSdk() { Anotar("InicializaSdk"); return ResultadoInicializa; }

        public void InicioSesionSdk(string usuario, string contrasena) => Anotar("InicioSesionSdk");

        public int SetNombrePaq(string sistema) { Anotar("SetNombrePaq"); return ResultadoSetNombrePaq; }

        public void InicioSesionSdkContpaqi(string usuario, string contrasena) => Anotar("InicioSesionSdkContpaqi");

        public int AbreEmpresa(string directorio)
        {
            Anotar("AbreEmpresa");
            BloquearAbreEmpresa?.Wait(TimeSpan.FromSeconds(10));
            return ResultadoAbreEmpresa;
        }

        public void CierraEmpresa() => Anotar("CierraEmpresa");

        public void TerminaSdk() => Anotar("TerminaSdk");

        public string MensajeError(int codigo) => $"error {codigo}";
    }

    /// <summary>Reloj que solo avanza cuando la prueba lo manda; en UTC para que la hora local sea determinista.</summary>
    public sealed class RelojFalso(DateTimeOffset inicio) : TimeProvider
    {
        private DateTimeOffset _ahora = inicio;

        public override DateTimeOffset GetUtcNow() => _ahora;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;
    }
}
