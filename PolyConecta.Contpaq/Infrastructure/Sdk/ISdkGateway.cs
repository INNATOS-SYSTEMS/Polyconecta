using Contpaq.Bridge.Core.Contract;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    /// <summary>
    /// Adaptador de escritura del bridge (D-122): ContpaqiSdkGateway en modo real y
    /// SimulatedSdkGateway en modo simulado. Es síncrono porque el SDK real exige un solo hilo STA;
    /// OutboxWorker lo llama siempre desde el suyo.
    /// </summary>
    public interface ISdkGateway
    {
        bool EsReal { get; }

        bool SesionActiva { get; }

        bool AsegurarSesion();

        void CerrarSiInactiva();

        void Apagar();

        void BombearMensajes();

        /// <summary>Ejecuta un comando ya validado (CT-39).</summary>
        ResultadoEjecucion Ejecutar(string transactionId, ComandoLeido comando);
    }
}
