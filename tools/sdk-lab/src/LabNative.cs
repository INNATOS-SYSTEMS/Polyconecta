using System.Runtime.InteropServices;
using System.Text;
using Contpaq.Bridge.Infrastructure.Sdk;

namespace SdkLab;

/// <summary>
/// Funciones que la matriz necesita y el bridge aún no importa. Firmas tomadas de
/// docs/contpaq/Referencia_SDK_CONTPAQi.md. NO se importa fAltaMovimientoSeriesCapas_Param:
/// la referencia documenta todos sus parámetros como Cadena (incluido el id), lo cual es
/// dudoso, y una firma stdcall equivocada en x86 corrompe la pila.
/// </summary>
internal static class LabNative
{
    private const string Dll = ContpaqiSdkNative.DllName;

    [DllImport(Dll, EntryPoint = "fCalculaMovtoSerieCapa", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int fCalculaMovtoSerieCapa(int aIdMovimiento);

    [DllImport(Dll, EntryPoint = "fObtieneUnidadesPendientes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int fObtieneUnidadesPendientes(
        [MarshalAs(UnmanagedType.LPStr)] string aConceptoDocto,
        [MarshalAs(UnmanagedType.LPStr)] string aCodigoProducto,
        [MarshalAs(UnmanagedType.LPStr)] string aCodigoAlmacen,
        StringBuilder aUnidades);

    // Inicio de sesión sin ventana (Comercial). No está en docs/contpaq/Referencia_SDK_CONTPAQi.md; firma y orden de
    // llamada verificados contra la wiki de ARSoftware.Contpaqi.Comercial ("Inicializar y finalizar SDK"): se llama
    // ANTES de inicializar. Se declara void a propósito: en stdcall x86 ignorar un int devuelto es seguro.
    [DllImport(Dll, EntryPoint = "fInicioSesionSDK", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern void fInicioSesionSDK(
        [MarshalAs(UnmanagedType.LPStr)] string aUsuario,
        [MarshalAs(UnmanagedType.LPStr)] string aContrasenia);

    // Variante para usuarios centralizados de CONTPAQi (CONTPAQi Usuarios, v11+). Misma forma que fInicioSesionSDK.
    [DllImport(Dll, EntryPoint = "fInicioSesionSDKCONTPAQi", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern void fInicioSesionSDKCONTPAQi(
        [MarshalAs(UnmanagedType.LPStr)] string aUsuario,
        [MarshalAs(UnmanagedType.LPStr)] string aContrasenia);
}
