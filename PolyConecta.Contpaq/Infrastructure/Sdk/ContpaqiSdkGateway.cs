using System;
using System.Runtime.InteropServices;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    /// <summary>
    /// Gateway real: única puerta al SDK de CONTPAQi (MGWServicios.dll, Principio II). Separa dos ciclos
    /// de vida (R-07): el del SDK, una vez por proceso (<see cref="IniciarSdk"/>: directorio, los dos
    /// inicios de sesión de D-108), y el de la empresa, por lote (<see cref="AbrirEmpresa"/> y
    /// <see cref="CerrarEmpresa"/>). IdleSessionTimeoutSeconds solo cierra la empresa; fTerminaSDK se
    /// llama una vez, al apagar. El ciclo del outbox vive en OutboxWorker, que lo llama siempre desde su
    /// mismo hilo STA (D-122).
    /// </summary>
    public partial class ContpaqiSdkGateway : ISdkGateway
    {
        private readonly ILogger<ContpaqiSdkGateway> _logger;
        private readonly ISdkNativo _nativo;
        private readonly TimeProvider _reloj;

        private readonly string _sdkPath;
        private readonly string _companyPath;
        private readonly int _idleTimeoutSeconds;
        private readonly string? _comercialUsuario;
        private readonly string _comercialContrasena;
        private readonly string? _contpaqiUsuario;
        private readonly string _contpaqiContrasena;

        private bool _sdkIniciado;
        private bool _empresaAbierta;
        private DateTimeOffset _ultimaActividad = DateTimeOffset.MinValue;

        public bool EsReal => true;

        public bool SdkIniciado => _sdkIniciado;

        public bool SesionActiva => _empresaAbierta;

        public ContpaqiSdkGateway(IConfiguration configuration, ILogger<ContpaqiSdkGateway> logger, ISdkNativo nativo, TimeProvider? reloj = null)
        {
            _logger = logger;
            _nativo = nativo;
            _reloj = reloj ?? TimeProvider.System;

            _sdkPath = configuration["BridgeConfig:SdkPath"] ?? @"C:\Program Files (x86)\Compac\COMERCIAL";
            _companyPath = configuration["BridgeConfig:CompanyPath"] ?? @"C:\Compac\Empresas\adPOLYEMPAQUES";
            _idleTimeoutSeconds = int.TryParse(configuration["BridgeConfig:IdleSessionTimeoutSeconds"], out var i) ? i : 5;
            // Las dos sesiones de D-108. Solo de variables de entorno (CT-29): BridgeConfig__Sesion__ComercialUsuario, etc.
            _comercialUsuario = configuration["BridgeConfig:Sesion:ComercialUsuario"];
            _comercialContrasena = configuration["BridgeConfig:Sesion:ComercialContrasena"] ?? string.Empty;
            _contpaqiUsuario = configuration["BridgeConfig:Sesion:ContpaqiUsuario"];
            _contpaqiContrasena = configuration["BridgeConfig:Sesion:ContpaqiContrasena"] ?? string.Empty;
        }

        /// <summary>El SDK y la empresa listos para un lote: el SDK solo se inicia la primera vez.</summary>
        public bool AsegurarSesion() => IniciarSdk() && AbrirEmpresa();

        public void CerrarSiInactiva()
        {
            if (_empresaAbierta && (_reloj.GetUtcNow() - _ultimaActividad).TotalSeconds > _idleTimeoutSeconds)
                CerrarEmpresa();
        }

        public void Apagar()
        {
            CerrarEmpresa();
            TerminarSdk();
        }

        public void BombearMensajes() => PumpWin32Messages();

        /// <summary>
        /// En F0 ningún comando está implementado contra el SDK real: cada uno llega en su fase
        /// (ALTA_PEDIDO 2.4, TRASPASO 3.1, ALTA_ALMACEN 3.2, CIERRE_PRODUCCION 5.1, REMISION 6.1).
        /// El DOCUMENT_CREATE genérico se retiró con el contrato bridge-v1.
        /// </summary>
        public ResultadoEjecucion Ejecutar(string transactionId, ComandoLeido comando)
        {
            _ultimaActividad = _reloj.GetUtcNow();
            return ResultadoEjecucion.Fallo(ErrorContrato.De(CodigosError.SdkError,
                $"El comando {comando.CommandType} todavía no está implementado contra el SDK real.",
                new() { ["motivo"] = "COMANDO_NO_IMPLEMENTADO", ["command_type"] = comando.CommandType }));
        }

        /// <summary>
        /// Inicia el SDK una sola vez por proceso, con los dos inicios de sesión de D-108 en este orden:
        /// Comercial ANTES de fSetNombrePAQ y el usuario centralizado de CONTPAQi DESPUÉS. Sin ellos CONTPAQi
        /// abre una ventana de ingreso que nadie ve y la llamada espera para siempre (S-02). Sin usuarios
        /// configurados se usa fInicializaSDK.
        /// </summary>
        public bool IniciarSdk()
        {
            if (_sdkIniciado) return true;
            try
            {
                if (_nativo.Preparar(_sdkPath) is { } motivo)
                {
                    LogError(_logger, motivo);
                    return false;
                }
                LogSdk(_logger, _sdkPath);

                int initErr;
                if (!string.IsNullOrEmpty(_comercialUsuario) || !string.IsNullOrEmpty(_contpaqiUsuario))
                {
                    if (!string.IsNullOrEmpty(_comercialUsuario))
                    {
                        LogLlamada(_logger, "fInicioSesionSDK");
                        _nativo.InicioSesionSdk(_comercialUsuario, _comercialContrasena);
                    }
                    LogLlamada(_logger, "fSetNombrePAQ");
                    initErr = _nativo.SetNombrePaq("CONTPAQ I COMERCIAL");
                    if (initErr == ContpaqiSdkNative.kSIN_ERRORES && !string.IsNullOrEmpty(_contpaqiUsuario))
                    {
                        LogLlamada(_logger, "fInicioSesionSDKCONTPAQi");
                        _nativo.InicioSesionSdkContpaqi(_contpaqiUsuario, _contpaqiContrasena);
                    }
                }
                else
                {
                    LogSinSesion(_logger);
                    initErr = _nativo.InicializaSdk();
                }
                if (initErr != ContpaqiSdkNative.kSIN_ERRORES)
                {
                    LogInicioFallo(_logger, initErr, _nativo.MensajeError(initErr));
                    return false;
                }

                _sdkIniciado = true;
                LogIniciado(_logger);
                return true;
            }
            catch (Exception ex)
            {
                LogExcepcion(_logger, ex);
                return false;
            }
        }

        /// <summary>Abre la empresa para un lote. Si ya está abierta no hace nada.</summary>
        public bool AbrirEmpresa()
        {
            if (!_sdkIniciado) return false;
            if (_empresaAbierta) return true;
            try
            {
                LogLlamada(_logger, "fAbreEmpresa");
                var err = _nativo.AbreEmpresa(_companyPath);
                // 126209: la empresa ya está abierta en la sesión activa de Comercial; se sigue con esa.
                if (err == ContpaqiSdkNative.kSIN_ERRORES || err == 126209)
                {
                    _empresaAbierta = true;
                    _ultimaActividad = _reloj.GetUtcNow();
                    MetricCollectorService.IsSdkSessionActive = true;
                    LogEmpresaAbierta(_logger, _companyPath);
                    return true;
                }
                LogEmpresaFallo(_logger, _companyPath, err, _nativo.MensajeError(err));
                return false;
            }
            catch (Exception ex)
            {
                LogExcepcion(_logger, ex);
                return false;
            }
        }

        /// <summary>Cierra la empresa (al vaciarse la cola o por inactividad). El SDK sigue iniciado.</summary>
        public void CerrarEmpresa()
        {
            if (!_empresaAbierta) return;
            try
            {
                LogLlamada(_logger, "fCierraEmpresa");
                _nativo.CierraEmpresa();
            }
            catch (Exception ex)
            {
                LogCierreExcepcion(_logger, ex);
            }
            _empresaAbierta = false;
            MetricCollectorService.IsSdkSessionActive = false;
            LogEmpresaCerrada(_logger);
        }

        /// <summary>fTerminaSDK, una sola vez, al apagar el proceso.</summary>
        private void TerminarSdk()
        {
            if (!_sdkIniciado) return;
            try
            {
                LogLlamada(_logger, "fTerminaSDK");
                _nativo.TerminaSdk();
            }
            catch (Exception ex)
            {
                LogCierreExcepcion(_logger, ex);
            }
            _sdkIniciado = false;
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "{Motivo}")]
        private static partial void LogError(ILogger logger, string motivo);

        [LoggerMessage(Level = LogLevel.Information, Message = "[ARCHITECTURE CHECK] Proceso x86. SDK de CONTPAQi en {SdkPath}.")]
        private static partial void LogSdk(ILogger logger, string sdkPath);

        [LoggerMessage(Level = LogLevel.Information, Message = "Llamada nativa {Llamada}")]
        private static partial void LogLlamada(ILogger logger, string llamada);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Sin BridgeConfig__Sesion__*: se usa fInicializaSDK. Si CONTPAQi pide ingreso, la llamada se queda esperando (D-108).")]
        private static partial void LogSinSesion(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "La inicialización del SDK falló con el código {ErrCode}: {Msg}")]
        private static partial void LogInicioFallo(ILogger logger, int errCode, string msg);

        [LoggerMessage(Level = LogLevel.Information, Message = "SDK de CONTPAQi iniciado (una vez por proceso).")]
        private static partial void LogIniciado(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Empresa de CONTPAQi abierta: {Path}")]
        private static partial void LogEmpresaAbierta(ILogger logger, string path);

        [LoggerMessage(Level = LogLevel.Error, Message = "fAbreEmpresa falló para '{Path}' con el código {ErrCode}: {Msg}")]
        private static partial void LogEmpresaFallo(ILogger logger, string path, int errCode, string msg);

        [LoggerMessage(Level = LogLevel.Information, Message = "Empresa de CONTPAQi cerrada; el SDK sigue iniciado.")]
        private static partial void LogEmpresaCerrada(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo iniciar el SDK ni abrir la empresa")]
        private static partial void LogExcepcion(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Excepción al cerrar el SDK o la empresa")]
        private static partial void LogCierreExcepcion(ILogger logger, Exception ex);

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
