using System;
using System.Runtime.InteropServices;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    /// <summary>
    /// Las llamadas nativas al SDK de CONTPAQi (MGWServicios.dll) que usa el gateway, detrás de una
    /// interfaz para poder probar el ciclo de vida sin SDK (R-07, L1-T001). Todas se llaman desde el
    /// hilo STA del OutboxWorker.
    /// </summary>
    public interface ISdkNativo
    {
        /// <summary>Verifica el entorno (Windows, proceso de 32 bits, DLL) y apunta el proceso a la carpeta del SDK. Devuelve el motivo si no se puede, o null.</summary>
        string? Preparar(string sdkPath);

        int InicializaSdk();

        void InicioSesionSdk(string usuario, string contrasena);

        int SetNombrePaq(string sistema);

        void InicioSesionSdkContpaqi(string usuario, string contrasena);

        int AbreEmpresa(string directorio);

        void CierraEmpresa();

        void TerminaSdk();

        string MensajeError(int codigo);
    }

    /// <summary>El SDK real: pasa cada llamada a <see cref="ContpaqiSdkNative"/>.</summary>
    public sealed class SdkNativo : ISdkNativo
    {
        public string? Preparar(string sdkPath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "El SDK de CONTPAQi solo existe en Windows: usa el modo Simulated.";
            if (Environment.Is64BitProcess)
                return "[ARCHITECTURE] El proceso es de 64 bits y MGW_SDK.dll es de 32: compila con -p:Bridge32=true (scripts/vps).";
            if (!System.IO.File.Exists(System.IO.Path.Combine(sdkPath, ContpaqiSdkNative.DllName)))
                return $"No está {ContpaqiSdkNative.DllName} en BridgeConfig:SdkPath ({sdkPath}).";

            // Una sola carpeta, la del SDK de Comercial, como en sdk-lab: mezclar DLL de otros productos
            // (Bancos, AdminPAQ, Facturación) carga versiones que no coinciden.
            SetDllDirectory(sdkPath);
            System.IO.Directory.SetCurrentDirectory(sdkPath);
            return null;
        }

        public int InicializaSdk() => ContpaqiSdkNative.fInicializaSDK();

        public void InicioSesionSdk(string usuario, string contrasena) => ContpaqiSdkNative.fInicioSesionSDK(usuario, contrasena);

        public int SetNombrePaq(string sistema) => ContpaqiSdkNative.fSetNombrePAQ(sistema);

        public void InicioSesionSdkContpaqi(string usuario, string contrasena) => ContpaqiSdkNative.fInicioSesionSDKCONTPAQi(usuario, contrasena);

        public int AbreEmpresa(string directorio) => ContpaqiSdkNative.fAbreEmpresa(directorio);

        public void CierraEmpresa() => ContpaqiSdkNative.fCierraEmpresa();

        public void TerminaSdk() => ContpaqiSdkNative.fTerminaSDK();

        public string MensajeError(int codigo) => ContpaqiSdkNative.GetErrorMessage(codigo);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
