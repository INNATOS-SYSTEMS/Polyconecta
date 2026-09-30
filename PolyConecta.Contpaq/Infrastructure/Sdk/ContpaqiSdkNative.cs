using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    // Estructuras según docs/contpaq/Referencia_SDK_CONTPAQi.md, "Definición de las estructuras de datos".
    // Longitudes = constante + 1 (terminador): kLongCodigo 30, kLongSerie 11, kLongFecha 23,
    // kLongReferencia 20, kLongDescripcion 60. El orden y el tamaño de cada campo definen la posición en memoria:
    // un tamaño distinto desplaza todos los campos siguientes.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct tDocumento
    {
        public double aFolio;
        public int aNumMoneda;
        public double aTipoCambio;
        public double aImporte;
        public double aDescuentoDoc1;
        public double aDescuentoDoc2;
        public int aSistemaOrigen;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aCodConcepto;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string aSeries;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string aFecha;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aCodigoCteProv;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aCodigoAgente;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
        public string aReferencia;
        public int aAfecta;
        public double aGasto1;
        public double aGasto2;
        public double aGasto3;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct tMovimiento
    {
        public int aConsecutivo;
        public double aUnidades;
        public double aPrecio;
        public double aCosto;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aCodProdSer;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aCodAlmacen;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
        public string aReferencia;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aCodClasific;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct tSeriesCapas
    {
        public double aUnidades;
        public double aTipoCambio;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string aSeries;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 61)]
        public string aPedimento;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 61)]
        public string aAgencia;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string aFechaPedimento;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 61)]
        public string aNumeroLote;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string aFechaFabricacion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string aFechaCaducidad;
    }

    public static class ContpaqiSdkNative
    {
        public const int kSIN_ERRORES = 0;
        // Interfaz del SDK para CONTPAQi Comercial Premium (docs/contpaq/Referencia_SDK_CONTPAQi.md,
        // "Archivos usados por el SDK"). MGW_SDK.dll es la de Factura Electrónica y depende de MGW100.dll,
        // que no existe en la carpeta de Comercial.
        public const string DllName = "MGWServicios.dll";

        [DllImport(DllName, EntryPoint = "fInicializaSDK", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fInicializaSDK();

        [DllImport(DllName, EntryPoint = "fTerminaSDK", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void fTerminaSDK();

        [DllImport(DllName, EntryPoint = "fSetNombrePAQ", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fSetNombrePAQ([MarshalAs(UnmanagedType.LPStr)] string aSistema);

        [DllImport(DllName, EntryPoint = "fAbreEmpresa", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fAbreEmpresa([MarshalAs(UnmanagedType.LPStr)] string aDirectorioEmpresa);

        [DllImport(DllName, EntryPoint = "fCierraEmpresa", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void fCierraEmpresa();

        [DllImport(DllName, EntryPoint = "fError", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void fError(int aNumError, [MarshalAs(UnmanagedType.LPStr)] StringBuilder aMensaje, int aLen);

        [DllImport(DllName, EntryPoint = "fAltaDocumento", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fAltaDocumento(ref int aIdDocumento, ref tDocumento aDocumento);

        [DllImport(DllName, EntryPoint = "fAltaMovimiento", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fAltaMovimiento(int aIdDocumento, ref int aIdMovimiento, ref tMovimiento aMovimiento);

        [DllImport(DllName, EntryPoint = "fAltaMovimientoSeriesCapas", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fAltaMovimientoSeriesCapas(int aIdMovimiento, ref tSeriesCapas aSeriesCapas);

        [DllImport(DllName, EntryPoint = "fAfectaDocto_Param", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int fAfectaDocto_Param([MarshalAs(UnmanagedType.LPStr)] string aCodConcepto, [MarshalAs(UnmanagedType.LPStr)] string aSerie, double aFolio, bool aAfecta);

        public static string GetErrorMessage(int errorCode)
        {
            if (errorCode == kSIN_ERRORES) return "Success";
            var sb = new StringBuilder(512);
            try
            {
                fError(errorCode, sb, 512);
                var msg = sb.ToString().Trim();
                return string.IsNullOrEmpty(msg) ? $"SDK Error #{errorCode}" : msg;
            }
            catch (Exception ex)
            {
                return $"SDK Error #{errorCode} (Failed to resolve message: {ex.Message})";
            }
        }
    }
}
