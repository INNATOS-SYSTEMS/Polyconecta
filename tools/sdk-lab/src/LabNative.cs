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
}
