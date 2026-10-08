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

        /// <summary>El SDK ya hizo sus inicios de sesión (una vez por proceso, R-07).</summary>
        bool SdkIniciado { get; }

        /// <summary>La empresa está abierta para un lote.</summary>
        bool SesionActiva { get; }

        /// <summary>Inicia el SDK si todavía no lo está. El worker lo llama al arrancar.</summary>
        bool IniciarSdk();

        /// <summary>El SDK iniciado y la empresa abierta, para un lote.</summary>
        bool AsegurarSesion();

        void CerrarSiInactiva();

        void Apagar();

        void BombearMensajes();

        /// <summary>Ejecuta un comando ya validado (CT-39).</summary>
        ResultadoEjecucion Ejecutar(string transactionId, ComandoLeido comando);
    }
}
