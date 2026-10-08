using System;
using System.Threading;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    /// <summary>Lo que el vigilante sabe de la transacción en curso, para registrar y responder el bloqueo.</summary>
    public sealed record ContextoLlamada(string TransactionId, string CorrelationId);

    /// <summary>
    /// Vigilante de tiempo límite por llamada nativa (FR-005, R-07, L1-T003). Un hilo bloqueado dentro de
    /// la DLL no se puede abortar sin dejar el SDK inconsistente: al vencer el tiempo se avisa con
    /// <see cref="AlVencer"/> (que responde SDK_TIMEOUT a la transacción en curso y registra el bloqueo)
    /// y el proceso sale con código distinto de 0 para que la tarea de D-115 lo levante.
    /// </summary>
    public sealed class VigilanteSdk(TimeSpan limite, TimeProvider reloj)
    {
        /// <summary>Código de salida del proceso cuando una llamada nativa no regresa.</summary>
        public const int CodigoSalidaTimeout = 3;

        private ContextoLlamada? _contexto;

        /// <summary>Transacción que está procesando el worker, o null (inicio del SDK, apertura de empresa).</summary>
        public ContextoLlamada? Contexto
        {
            get => Volatile.Read(ref _contexto);
            set => Volatile.Write(ref _contexto, value);
        }

        /// <summary>Se llama desde el hilo del temporizador, con la llamada que no regresó y la transacción en curso.</summary>
        public Action<string, ContextoLlamada?>? AlVencer { get; set; }

        public TimeSpan Limite => limite;

        public T Ejecutar<T>(string llamada, Func<T> accion)
        {
            using var temporizador = reloj.CreateTimer(_ => AlVencer?.Invoke(llamada, Contexto), null, limite, Timeout.InfiniteTimeSpan);
            return accion();
        }

        public void Ejecutar(string llamada, Action accion) => Ejecutar<object?>(llamada, () => { accion(); return null; });
    }

    /// <summary>Decorador: cada llamada nativa corre bajo el <see cref="VigilanteSdk"/>.</summary>
    public sealed class SdkNativoVigilado(ISdkNativo nativo, VigilanteSdk vigilante) : ISdkNativo
    {
        public string? Preparar(string sdkPath) => nativo.Preparar(sdkPath);

        public int InicializaSdk() => vigilante.Ejecutar(nameof(InicializaSdk), nativo.InicializaSdk);

        public void InicioSesionSdk(string usuario, string contrasena) =>
            vigilante.Ejecutar(nameof(InicioSesionSdk), () => nativo.InicioSesionSdk(usuario, contrasena));

        public int SetNombrePaq(string sistema) => vigilante.Ejecutar(nameof(SetNombrePaq), () => nativo.SetNombrePaq(sistema));

        public void InicioSesionSdkContpaqi(string usuario, string contrasena) =>
            vigilante.Ejecutar(nameof(InicioSesionSdkContpaqi), () => nativo.InicioSesionSdkContpaqi(usuario, contrasena));

        public int AbreEmpresa(string directorio) => vigilante.Ejecutar(nameof(AbreEmpresa), () => nativo.AbreEmpresa(directorio));

        public void CierraEmpresa() => vigilante.Ejecutar(nameof(CierraEmpresa), nativo.CierraEmpresa);

        public void TerminaSdk() => vigilante.Ejecutar(nameof(TerminaSdk), nativo.TerminaSdk);

        public string MensajeError(int codigo) => nativo.MensajeError(codigo);
    }
}
