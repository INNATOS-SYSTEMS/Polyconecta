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

        var initRc = ContpaqiSdkNative.fInicializaSDK();
        if (initRc != 0) throw new LabException("SDK_INIT_FAILED", $"fInicializaSDK rc={initRc}: {Msg(initRc)}");
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
