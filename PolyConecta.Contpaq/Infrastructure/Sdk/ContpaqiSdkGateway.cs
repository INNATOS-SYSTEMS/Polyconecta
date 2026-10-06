using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    /// <summary>
    /// Gateway real: única puerta al SDK de CONTPAQi (MGWServicios.dll, Principio II). Conserva la
    /// sesión y las llamadas nativas; el ciclo del outbox vive en OutboxWorker, que lo llama siempre
    /// desde su mismo hilo STA (D-122).
    /// </summary>
    public class ContpaqiSdkGateway : ISdkGateway
    {
        private readonly ILogger<ContpaqiSdkGateway> _logger;

        private readonly string _sdkPath;
        private readonly string _companyPath;
        private readonly int _timeoutSeconds;
        private readonly int _idleTimeoutSeconds;

        private bool _isSdkInitialized = false;
        private bool _isCompanyOpen = false;
        private DateTime _lastActivityTime = DateTime.MinValue;

        public bool EsReal => true;

        public bool SesionActiva => _isCompanyOpen;

        public ContpaqiSdkGateway(IConfiguration configuration, ILogger<ContpaqiSdkGateway> logger)
        {
            _logger = logger;

            _sdkPath = configuration["BridgeConfig:SdkPath"] ?? @"C:\Program Files (x86)\Compac\COMERCIAL";
            _companyPath = configuration["BridgeConfig:CompanyPath"] ?? @"C:\Compac\Empresas\adPOLYEMPAQUES";
            _timeoutSeconds = int.TryParse(configuration["BridgeConfig:TransactionTimeoutSeconds"], out var t) ? t : 8;
            _idleTimeoutSeconds = int.TryParse(configuration["BridgeConfig:IdleSessionTimeoutSeconds"], out var i) ? i : 5;
            // Las dos sesiones de D-108. Solo de variables de entorno (CT-29): BridgeConfig__Sesion__ComercialUsuario, etc.
            _comercialUsuario = configuration["BridgeConfig:Sesion:ComercialUsuario"];
            _comercialContrasena = configuration["BridgeConfig:Sesion:ComercialContrasena"] ?? string.Empty;
            _contpaqiUsuario = configuration["BridgeConfig:Sesion:ContpaqiUsuario"];
            _contpaqiContrasena = configuration["BridgeConfig:Sesion:ContpaqiContrasena"] ?? string.Empty;
        }

        private readonly string? _comercialUsuario;
        private readonly string _comercialContrasena;
        private readonly string? _contpaqiUsuario;
        private readonly string _contpaqiContrasena;

        public bool AsegurarSesion() => EnsureCompanySessionOpen();

        public void CerrarSiInactiva()
        {
            if (_isCompanyOpen && (DateTime.UtcNow - _lastActivityTime).TotalSeconds > _idleTimeoutSeconds)
                CloseCompanySession();
        }

        public void Apagar() => ShutdownSdk();

        public void BombearMensajes() => PumpWin32Messages();

        /// <summary>
        /// En F0 ningún comando está implementado contra el SDK real: cada uno llega en su fase
        /// (ALTA_PEDIDO 2.4, TRASPASO 3.1, ALTA_ALMACEN 3.2, CIERRE_PRODUCCION 5.1, REMISION 6.1).
        /// El DOCUMENT_CREATE genérico se retiró con el contrato bridge-v1.
        /// </summary>
        public ResultadoEjecucion Ejecutar(string transactionId, ComandoLeido comando)
        {
            _lastActivityTime = DateTime.UtcNow;
            return ResultadoEjecucion.Fallo(ErrorContrato.De(CodigosError.SdkError,
                $"El comando {comando.CommandType} todavía no está implementado contra el SDK real.",
                new() { ["motivo"] = "COMANDO_NO_IMPLEMENTADO", ["command_type"] = comando.CommandType }));
        }

        private bool EnsureCompanySessionOpen()
        {
            if (_isCompanyOpen) return true;

            try
            {
                if (!_isSdkInitialized)
                {
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        _logger.LogError("El SDK de CONTPAQi solo existe en Windows: usa el modo Simulated.");
                        return false;
                    }
                    if (Environment.Is64BitProcess)
                    {
                        _logger.LogError("[ARCHITECTURE] El proceso es de 64 bits y MGW_SDK.dll es de 32: compila con -p:Bridge32=true (scripts/vps).");
                        return false;
                    }
                    if (!System.IO.File.Exists(System.IO.Path.Combine(_sdkPath, ContpaqiSdkNative.DllName)))
                    {
                        _logger.LogError("No está {Dll} en BridgeConfig:SdkPath ({SdkPath}).", ContpaqiSdkNative.DllName, _sdkPath);
                        return false;
                    }

                    // Igual que sdk-lab, que abrió la empresa en todas las pruebas de la matriz: una sola carpeta,
                    // la del SDK de Comercial. Mezclar DLL de otros productos (Bancos, AdminPAQ, Facturación)
                    // carga versiones que no coinciden ("entry point _gSaciEndpointDSL could not be located").
                    _logger.LogInformation("[ARCHITECTURE CHECK] Proceso x86. SDK de CONTPAQi en {SdkPath}.", _sdkPath);
                    SetDllDirectory(_sdkPath);
                    System.IO.Directory.SetCurrentDirectory(_sdkPath);

                    // Dos inicios de sesión, en este orden (D-108, S-02): Comercial ANTES de fSetNombrePAQ y el usuario
                    // centralizado de CONTPAQi DESPUÉS. Sin ellos CONTPAQi abre una ventana de ingreso que nadie ve
                    // y la llamada espera para siempre. Sin usuarios configurados se usa fInicializaSDK.
                    int initErr;
                    if (!string.IsNullOrEmpty(_comercialUsuario) || !string.IsNullOrEmpty(_contpaqiUsuario))
                    {
                        if (!string.IsNullOrEmpty(_comercialUsuario))
                        {
                            _logger.LogInformation("Calling native fInicioSesionSDK('{Usuario}')...", _comercialUsuario);
                            ContpaqiSdkNative.fInicioSesionSDK(_comercialUsuario, _comercialContrasena);
                        }
                        _logger.LogInformation("Calling native fSetNombrePAQ('CONTPAQ I COMERCIAL')...");
                        initErr = ContpaqiSdkNative.fSetNombrePAQ("CONTPAQ I COMERCIAL");
                        if (initErr == ContpaqiSdkNative.kSIN_ERRORES && !string.IsNullOrEmpty(_contpaqiUsuario))
                        {
                            _logger.LogInformation("Calling native fInicioSesionSDKCONTPAQi('{Usuario}')...", _contpaqiUsuario);
                            ContpaqiSdkNative.fInicioSesionSDKCONTPAQi(_contpaqiUsuario, _contpaqiContrasena);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Sin BridgeConfig__Sesion__*: se usa fInicializaSDK. Si CONTPAQi pide ingreso, la llamada se queda esperando (D-108).");
                        initErr = ContpaqiSdkNative.fInicializaSDK();
                    }
                    if (initErr != ContpaqiSdkNative.kSIN_ERRORES)
                    {
                        _logger.LogError("La inicialización del SDK falló con el código {ErrCode}: {Msg}", initErr, ContpaqiSdkNative.GetErrorMessage(initErr));
                        return false;
                    }

                    _isSdkInitialized = true;
                    _logger.LogInformation("CONTPAQi Native SDK Initialized Successfully.");
                }

                _logger.LogInformation("Calling native fAbreEmpresa('{CompanyPath}')...", _companyPath);
                int openErr = ContpaqiSdkNative.fAbreEmpresa(_companyPath);
                if (openErr == ContpaqiSdkNative.kSIN_ERRORES || openErr == 126209)
                {
                    if (openErr == 126209)
                    {
                        _logger.LogInformation("fAbreEmpresa returned 126209: Company is already open in active CONTPAQi Comercial session. Proceeding with active session.");
                    }
                    _isCompanyOpen = true;
                    _lastActivityTime = DateTime.UtcNow;
                    MetricCollectorService.IsSdkSessionActive = true;
                    _logger.LogInformation("Opened CONTPAQi Company session successfully: {Path}", _companyPath);
                    return true;
                }

                _logger.LogError("Native fAbreEmpresa failed for company path '{Path}' with error code {ErrCode}: {Msg}", _companyPath, openErr, ContpaqiSdkNative.GetErrorMessage(openErr));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize native SDK DLL bindings or open company session");
                return false;
            }
        }

        private void CloseCompanySession()
        {
            if (_isCompanyOpen)
            {
                try 
                { 
                    _logger.LogInformation("Calling native fCierraEmpresa()...");
                    ContpaqiSdkNative.fCierraEmpresa(); 
                } 
                catch (Exception ex) 
                { 
                    _logger.LogWarning(ex, "fCierraEmpresa encountered an exception during close"); 
                }
                _isCompanyOpen = false;
                MetricCollectorService.IsSdkSessionActive = false;
                _logger.LogInformation("Closed CONTPAQi Company session");
            }
        }

        private void ShutdownSdk()
        {
            CloseCompanySession();

            if (_isSdkInitialized)
            {
                try 
                { 
                    _logger.LogInformation("Calling native fTerminaSDK() on worker thread shutdown...");
                    ContpaqiSdkNative.fTerminaSDK(); 
                } 
                catch (Exception ex) 
                { 
                    _logger.LogWarning(ex, "fTerminaSDK encountered an exception during SDK shutdown"); 
                }
                _isSdkInitialized = false;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }

        private static void PumpWin32Messages()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1)) // 1 = PM_REMOVE
                    {
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                }
            }
            catch
            {
                // Non-critical Win32 message pumping fallback
            }
        }
    }
}
