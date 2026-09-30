using System.Runtime.InteropServices;
using Contpaq.Bridge.Infrastructure.Sdk;

namespace SdkLab;

/// <summary>Sesión sin estado: init → abrir empresa de laboratorio → ejecutar → cerrar. Un proceso, una sesión.</summary>
internal static class SdkSession
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    public static string Msg(int rc) => rc == 0 ? "" : ContpaqiSdkNative.GetErrorMessage(rc);

    public static T Run<T>(LabConfig cfg, Func<T> body)
    {
        if (IntPtr.Size != 4)
            throw new LabException("NOT_X86", "El proceso es de 64 bits; MGW_SDK.dll es Win32. Publica con win-x86.");
        if (!OperatingSystem.IsWindows())
            throw new LabException("NOT_WINDOWS", "El SDK de CONTPAQi solo existe en Windows.");
        if (!File.Exists(Path.Combine(cfg.SdkPath, ContpaqiSdkNative.DllName)))
            throw new LabException("SDK_DLL_MISSING", $"No se encontró {ContpaqiSdkNative.DllName} en {cfg.SdkPath}.");

        SetDllDirectory(cfg.SdkPath);
        Directory.SetCurrentDirectory(cfg.SdkPath);

        // Sin credenciales, el SDK abre una ventana de autenticación que bloquea al proceso si nadie la contesta.
        // Usuario y contraseña de CONTPAQi Comercial: solo desde variables de entorno, nunca desde archivos.
        var sdkUser = Environment.GetEnvironmentVariable("SDKLAB_CONTPAQI_USER");
        var sdkPassword = Environment.GetEnvironmentVariable("SDKLAB_CONTPAQI_PASSWORD");
        // Con credenciales, la secuencia documentada es fInicioSesionSDK + fSetNombrePAQ("CONTPAQ I COMERCIAL"),
        // que sustituye a fInicializaSDK. fInicializaSDK ignora la sesión y busca el "último usuario" de Windows
        // (rc=41719 "No existe último usuario" en empresas con usuarios).
        int initRc;
        if (!string.IsNullOrEmpty(sdkUser))
        {
            // Usuarios centralizados de CONTPAQi (por omisión) o usuarios propios de Comercial (SDKLAB_LOGIN_MODE=comercial).
            if (string.Equals(Environment.GetEnvironmentVariable("SDKLAB_LOGIN_MODE"), "comercial", StringComparison.OrdinalIgnoreCase))
                LabNative.fInicioSesionSDK(sdkUser, sdkPassword ?? "");
            else
                LabNative.fInicioSesionSDKCONTPAQi(sdkUser, sdkPassword ?? "");
            initRc = ContpaqiSdkNative.fSetNombrePAQ("CONTPAQ I COMERCIAL");
        }
        else
        {
            initRc = ContpaqiSdkNative.fInicializaSDK();
        }
        if (initRc != 0) throw new LabException("SDK_INIT_FAILED", $"{(string.IsNullOrEmpty(sdkUser) ? "fInicializaSDK" : "fSetNombrePAQ")} rc={initRc}: {Msg(initRc)}");
        try
        {
            var openRc = ContpaqiSdkNative.fAbreEmpresa(cfg.LabCompanyPath);
            if (openRc != 0) throw new LabException("SDK_OPEN_FAILED", $"fAbreEmpresa rc={openRc}: {Msg(openRc)}");
            try { return body(); }
            finally { ContpaqiSdkNative.fCierraEmpresa(); }
        }
        finally { ContpaqiSdkNative.fTerminaSDK(); }
    }
}
