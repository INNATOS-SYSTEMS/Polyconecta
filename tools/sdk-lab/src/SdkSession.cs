using System.Runtime.InteropServices;
using Contpaq.Bridge.Infrastructure.Sdk;

namespace SdkLab;

/// <summary>Sesión sin estado: init → abrir empresa de laboratorio → ejecutar → cerrar. Un proceso, una sesión.</summary>
internal static class SdkSession
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    public static string Msg(int rc) => rc == 0 ? "" : ContpaqiSdkNative.GetErrorMessage(rc);

    public static T Run<T>(LabConfig cfg, Func<T> body) => Run(cfg, body, timings: null);

    /// <summary>Igual que <see cref="Run{T}(LabConfig, Func{T})"/>, y anota en <paramref name="timings"/> los milisegundos de cada fase (S-01).</summary>
    public static T Run<T>(LabConfig cfg, Func<T> body, Dictionary<string, long>? timings)
    {
        Progress.Init(cfg);
        Progress.Log("sesión: inicio");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        void Mark(string phase) { if (timings is not null) timings[phase] = sw.ElapsedMilliseconds; sw.Restart(); }
        if (IntPtr.Size != 4)
            throw new LabException("NOT_X86", "El proceso es de 64 bits; MGW_SDK.dll es Win32. Publica con win-x86.");
        if (!OperatingSystem.IsWindows())
            throw new LabException("NOT_WINDOWS", "El SDK de CONTPAQi solo existe en Windows.");
        if (!File.Exists(Path.Combine(cfg.SdkPath, ContpaqiSdkNative.DllName)))
            throw new LabException("SDK_DLL_MISSING", $"No se encontró {ContpaqiSdkNative.DllName} en {cfg.SdkPath}.");

        SetDllDirectory(cfg.SdkPath);
        Directory.SetCurrentDirectory(cfg.SdkPath);

        // Dos inicios de sesión, en este orden (S-02, 1-oct-2026; mismo orden que ARSoftware.Contpaqi.Comercial):
        //   1. Usuario de Comercial (SDKLAB_COMERCIAL_USER, p. ej. SUPERVISOR) con fInicioSesionSDK, ANTES de fSetNombrePAQ.
        //      Sin él, fSetNombrePAQ abre la ventana "Ingreso a CONTPAQi COMERCIAL" y espera para siempre.
        //   2. Usuario centralizado de CONTPAQi (SDKLAB_CONTPAQI_USER) con fInicioSesionSDKCONTPAQi, DESPUÉS de fSetNombrePAQ.
        //      Sin él, fAbreEmpresa espera a la ventana "Ingreso a CONTPAQi" del proceso SDKCONTPAQNG.
        // Credenciales solo desde variables de entorno, nunca desde archivos. Contraseña vacía si no hay variable.
        var comercialUser = Environment.GetEnvironmentVariable("SDKLAB_COMERCIAL_USER");
        var comercialPassword = Environment.GetEnvironmentVariable("SDKLAB_COMERCIAL_PASSWORD") ?? "";
        var sdkUser = Environment.GetEnvironmentVariable("SDKLAB_CONTPAQI_USER");
        var sdkPassword = Environment.GetEnvironmentVariable("SDKLAB_CONTPAQI_PASSWORD") ?? "";
        int initRc;
        if (!string.IsNullOrEmpty(comercialUser) || !string.IsNullOrEmpty(sdkUser))
        {
            if (!string.IsNullOrEmpty(comercialUser))
                Progress.Call("fInicioSesionSDK", () => LabNative.fInicioSesionSDK(comercialUser, comercialPassword));
            initRc = Progress.Call("fSetNombrePAQ", () => ContpaqiSdkNative.fSetNombrePAQ("CONTPAQ I COMERCIAL"));
            if (initRc == 0 && !string.IsNullOrEmpty(sdkUser))
                Progress.Call("fInicioSesionSDKCONTPAQi", () => LabNative.fInicioSesionSDKCONTPAQi(sdkUser, sdkPassword));
        }
        else
        {
            initRc = Progress.Call("fInicializaSDK", ContpaqiSdkNative.fInicializaSDK);
        }
        Mark("iniciarSdkMs");
        if (initRc != 0) throw new LabException("SDK_INIT_FAILED", $"{(string.IsNullOrEmpty(sdkUser) ? "fInicializaSDK" : "fSetNombrePAQ")} rc={initRc}: {Msg(initRc)}");
        try
        {
            var openRc = Progress.Call("fAbreEmpresa", () => ContpaqiSdkNative.fAbreEmpresa(cfg.LabCompanyPath));
            Mark("abrirEmpresaMs");
            if (openRc != 0) throw new LabException("SDK_OPEN_FAILED", $"fAbreEmpresa rc={openRc}: {Msg(openRc)}");
            try { var result = body(); Mark("cuerpoMs"); return result; }
            finally { Progress.Call("fCierraEmpresa", ContpaqiSdkNative.fCierraEmpresa); Mark("cerrarEmpresaMs"); }
        }
        finally { Progress.Call("fTerminaSDK", ContpaqiSdkNative.fTerminaSDK); Mark("terminarSdkMs"); }
    }
}
