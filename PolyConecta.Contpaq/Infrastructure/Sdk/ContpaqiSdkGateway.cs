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

        static ContpaqiSdkGateway()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    NativeLibrary.SetDllImportResolver(typeof(ContpaqiSdkNative).Assembly, (libraryName, assembly, searchPath) =>
                    {
                        if (libraryName.Equals(ContpaqiSdkNative.DllName, StringComparison.OrdinalIgnoreCase) ||
                            libraryName.Equals("MGW_SDK.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            var candidatePaths = new[]
                            {
                                @"C:\Program Files (x86)\Compac\COMERCIAL",
                                @"C:\Program Files (x86)\Compac\Bancos\AdminPAQSDK",
                                @"C:\Program Files (x86)\Compac\AdminPAQSDK",
                                @"C:\Program Files (x86)\Compac\Facturacion",
                                @"C:\Program Files (x86)\Compac\AdminPAQ",
                                @"C:\Compac\COMERCIAL",
                                @"C:\Program Files\Compac\COMERCIAL"
                            };

                            string? resolvedPath = candidatePaths.FirstOrDefault(p => 
                                System.IO.File.Exists(System.IO.Path.Combine(p, libraryName)))
                                ?? candidatePaths.FirstOrDefault(p => System.IO.File.Exists(System.IO.Path.Combine(p, "MGWServicios.dll")));

                            if (!string.IsNullOrEmpty(resolvedPath))
                            {
                                var fullPath = System.IO.Path.Combine(resolvedPath, libraryName);
                                if (System.IO.File.Exists(fullPath))
                                {
                                    System.IO.Directory.SetCurrentDirectory(resolvedPath);
                                    var handle = LoadLibraryW(fullPath);
                                    if (handle != IntPtr.Zero)
                                    {
                                        return handle;
                                    }
                                }
                            }
                        }
                        return IntPtr.Zero;
                    });
                }
                catch
                {
                    // Ignore if resolver is already registered
                }
            }
        }

        public ContpaqiSdkGateway(IConfiguration configuration, ILogger<ContpaqiSdkGateway> logger)
        {
            _logger = logger;

            _sdkPath = configuration["BridgeConfig:SdkPath"] ?? @"C:\Program Files (x86)\Compac\COMERCIAL";
            _companyPath = configuration["BridgeConfig:CompanyPath"] ?? @"C:\Compac\Empresas\adPOLYEMPAQUES";
            _timeoutSeconds = int.TryParse(configuration["BridgeConfig:TransactionTimeoutSeconds"], out var t) ? t : 8;
            _idleTimeoutSeconds = int.TryParse(configuration["BridgeConfig:IdleSessionTimeoutSeconds"], out var i) ? i : 5;
        }

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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (Environment.Is64BitProcess)
                        {
                            _logger.LogError("[CRITICAL ARCHITECTURE MISMATCH] Contpaq.Bridge is running as a 64-bit process (x64), but CONTPAQi MGW_SDK.dll requires a 32-bit process (x86). Ensure 'enable32BitAppOnWin64=True' is enabled on IIS AppPool 'ContpaqBridgeAppPool'.");
                        }
                        else
                        {
                            _logger.LogInformation("[ARCHITECTURE CHECK] Contpaq.Bridge process running cleanly in 32-bit mode (x86).");
                        }

                        var candidatePaths = new[]
                        {
                            _sdkPath,
                            @"C:\Program Files (x86)\Compac\COMERCIAL",
                            @"C:\Program Files (x86)\Compac\Bancos\AdminPAQSDK",
                            @"C:\Program Files (x86)\Compac\AdminPAQSDK",
                            @"C:\Program Files (x86)\Compac\Facturacion",
                            @"C:\Program Files (x86)\Compac\AdminPAQ",
                            @"C:\Compac\COMERCIAL",
                            @"C:\Program Files\Compac\COMERCIAL"
                        };

                        string effectiveSdkPath = candidatePaths.FirstOrDefault(p => 
                            !string.IsNullOrWhiteSpace(p) && 
                            System.IO.File.Exists(System.IO.Path.Combine(p, "MGW_SDK.dll"))) 
                            ?? _sdkPath;

                        var mgwDllPath = System.IO.Path.Combine(effectiveSdkPath, "MGW_SDK.dll");

                        _logger.LogInformation("Resolved CONTPAQi SDK Directory: {SdkPath} (MGW_SDK.dll Exists: {Exists})", 
                            effectiveSdkPath, System.IO.File.Exists(mgwDllPath));

                        SetDllDirectory(effectiveSdkPath);

                        var commonFilesCompac = @"C:\Program Files (x86)\Common Files\Compac";
                        var borlandBde = @"C:\Program Files (x86)\Common Files\Borland Shared\BDE";
                        var comercialPath = @"C:\Program Files (x86)\Compac\COMERCIAL";
                        var bancosSdkPath = @"C:\Program Files (x86)\Compac\Bancos\AdminPAQSDK";
                        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                        
                        var newPath = $"{effectiveSdkPath};{comercialPath};{bancosSdkPath};{commonFilesCompac};{borlandBde};{currentPath}";
                        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Process);
                        try { SetEnvironmentVariableW("PATH", newPath); } catch { }

                        try
                        {
                            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_USER_DIRS);
                            AddDllDirectory(effectiveSdkPath);
                            if (System.IO.Directory.Exists(comercialPath)) AddDllDirectory(comercialPath);
                            if (System.IO.Directory.Exists(bancosSdkPath)) AddDllDirectory(bancosSdkPath);
                            if (System.IO.Directory.Exists(commonFilesCompac)) AddDllDirectory(commonFilesCompac);
                            if (System.IO.Directory.Exists(borlandBde)) AddDllDirectory(borlandBde);
                            _logger.LogInformation("Registered Win32 AddDllDirectory paths: {SdkPath}, {ComercialPath}, {BancosSdkPath}, {CompacCommon}, {BorlandBde}", 
                                effectiveSdkPath, comercialPath, bancosSdkPath, commonFilesCompac, borlandBde);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not call AddDllDirectory Win32 API");
                        }

                        if (System.IO.Directory.Exists(effectiveSdkPath))
                        {
                            _logger.LogInformation("Setting Current Directory to SDK directory: {SdkPath}", effectiveSdkPath);
                            System.IO.Directory.SetCurrentDirectory(effectiveSdkPath);
                        }

                        try
                        {
                            NativeLibrary.SetDllImportResolver(typeof(ContpaqiSdkNative).Assembly, (libraryName, assembly, searchPath) =>
                            {
                                if (libraryName.Equals("MGW_SDK.dll", StringComparison.OrdinalIgnoreCase) || libraryName.Equals("MGW_SDK", StringComparison.OrdinalIgnoreCase))
                                {
                                    string dllFullPath = System.IO.Path.Combine(effectiveSdkPath, "MGW_SDK.dll");
                                    if (NativeLibrary.TryLoad(dllFullPath, out IntPtr handle))
                                    {
                                        return handle;
                                    }
                                }
                                return IntPtr.Zero;
                            });
                        }
                        catch { }
                    }

                    // Note: According to CONTPAQi SDK documentation, fSetNombrePAQ is only needed
                    // for CONTPAQi Factura Electrónica ("CONTPAQ I Facturacion"). For Comercial Premium,
                    // skipping fSetNombrePAQ allows fInicializaSDK to connect cleanly to default Comercial session.
                    _logger.LogInformation("Calling native fInicializaSDK()...");
                    int initErr = ContpaqiSdkNative.fInicializaSDK();
                    if (initErr != ContpaqiSdkNative.kSIN_ERRORES)
                    {
                        _logger.LogError("Native fInicializaSDK failed with error code {ErrCode}: {Msg}", initErr, ContpaqiSdkNative.GetErrorMessage(initErr));
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

        private void PreloadNativeDependencies(string effectiveSdkPath, string commonFilesCompac, string borlandBde)
        {
            var dependencies = new[]
            {
                System.IO.Path.Combine(borlandBde, "idapi32.dll"),
                System.IO.Path.Combine(effectiveSdkPath, "CC3250MT.DLL"),
                System.IO.Path.Combine(effectiveSdkPath, "BORLNDMM.DLL"),
                System.IO.Path.Combine(effectiveSdkPath, "RuntimeAPI.dll"),
                System.IO.Path.Combine(effectiveSdkPath, "CAC000.DLL"),
                System.IO.Path.Combine(effectiveSdkPath, "CAC100.DLL"),
                System.IO.Path.Combine(effectiveSdkPath, "MGW000.DLL"),
                System.IO.Path.Combine(effectiveSdkPath, "MGW_SDK.dll")
            };

            foreach (var depPath in dependencies)
            {
                if (!System.IO.File.Exists(depPath))
                {
                    _logger.LogWarning("Dependency file missing at path: {Path}", depPath);
                    continue;
                }

                var handle = LoadLibraryW(depPath);
                if (handle != IntPtr.Zero)
                {
                    _logger.LogInformation("Successfully pre-loaded Win32 dependency: {Filename} (Handle: 0x{Handle:X})", System.IO.Path.GetFileName(depPath), handle.ToInt64());
                }
                else
                {
                    var lastErr = Marshal.GetLastWin32Error();
                    _logger.LogError("LoadLibraryW failed for {Filename} at {Path}. Win32 Error Code: {ErrCode} (0x{ErrCode:X8})", System.IO.Path.GetFileName(depPath), depPath, lastErr, lastErr);
                    InspectPeImports(depPath);
                }
            }
        }

        private void InspectPeImports(string dllPath)
        {
            try
            {
                var data = System.IO.File.ReadAllBytes(dllPath);
                int peOff = BitConverter.ToInt32(data, 0x3c);
                ushort numSections = BitConverter.ToUInt16(data, peOff + 6);
                ushort optHdrSize = BitConverter.ToUInt16(data, peOff + 20);
                uint importRva = BitConverter.ToUInt32(data, peOff + 0x80);

                var sections = new List<(uint va, uint vsize, uint raw, uint rsize)>();
                int secOff = peOff + 24 + optHdrSize;
                for (int i = 0; i < numSections; i++)
                {
                    uint vsize = BitConverter.ToUInt32(data, secOff + 8);
                    uint va = BitConverter.ToUInt32(data, secOff + 12);
                    uint rsize = BitConverter.ToUInt32(data, secOff + 16);
                    uint raw = BitConverter.ToUInt32(data, secOff + 20);
                    sections.Add((va, vsize, raw, rsize));
                    secOff += 40;
                }

                uint RvaToOffset(uint rva)
                {
                    foreach (var s in sections)
                    {
                        if (rva >= s.va && rva < s.va + s.vsize)
                            return s.raw + (rva - s.va);
                    }
                    return 0;
                }

                uint importOff = RvaToOffset(importRva);
                _logger.LogInformation("Inspecting PE Import Table for {Path} (RVA: 0x{Rva:X}, Offset: 0x{Off:X})", dllPath, importRva, importOff);

                if (importOff > 0)
                {
                    int idx = (int)importOff;
                    while (idx + 20 <= data.Length)
                    {
                        uint nameRva = BitConverter.ToUInt32(data, idx + 12);
                        if (nameRva == 0) break;

                        uint nameOff = RvaToOffset(nameRva);
                        if (nameOff > 0 && nameOff < data.Length)
                        {
                            int end = Array.IndexOf(data, (byte)0, (int)nameOff);
                            if (end > nameOff)
                            {
                                string importedDll = System.Text.Encoding.ASCII.GetString(data, (int)nameOff, end - (int)nameOff);
                                var h = LoadLibraryW(importedDll);
                                var err = Marshal.GetLastWin32Error();
                                if (h != IntPtr.Zero)
                                {
                                    _logger.LogInformation("  [PE Import OK] {DllName} -> Handle: 0x{Handle:X}", importedDll, h.ToInt64());
                                }
                                else
                                {
                                    _logger.LogError("  [PE Import MISSING/FAILED] {DllName} -> Win32 Error Code: {ErrCode} (0x{ErrCode:X8})", importedDll, err, err);
                                }
                            }
                        }
                        idx += 20;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PE import table for {Path}", dllPath);
            }
        }

        private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
        private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetEnvironmentVariableW(string lpName, string lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDefaultDllDirectories(uint directoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr AddDllDirectory(string newDirectory);

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
