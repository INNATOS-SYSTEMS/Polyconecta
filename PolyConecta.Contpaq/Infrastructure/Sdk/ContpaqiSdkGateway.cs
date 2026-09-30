using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Commands;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Core.Services;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    public interface IContpaqiSdkGateway
    {
        bool IsCompanyOpen { get; }
        bool IsMockMode { get; }
    }

    public class ContpaqiSdkGateway : BackgroundService, IContpaqiSdkGateway
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly IWebhookDispatcher _webhookDispatcher;
        private readonly ILogger<ContpaqiSdkGateway> _logger;
        private readonly CircuitBreakerPolicy _circuitBreaker;

        private readonly string _sdkPath;
        private readonly string _companyPath;
        private readonly int _timeoutSeconds;
        private readonly int _idleTimeoutSeconds;
        private readonly bool _forceMockMode;

        private bool _isSdkInitialized = false;
        private bool _isCompanyOpen = false;
        private DateTime _lastActivityTime = DateTime.MinValue;
        private int _mockDocCounter = 1000;

        public bool IsCompanyOpen => _isCompanyOpen;
        public bool IsMockMode => _forceMockMode || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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

        public ContpaqiSdkGateway(
            IOutboxRepository outboxRepository,
            IWebhookDispatcher webhookDispatcher,
            IConfiguration configuration,
            ILogger<ContpaqiSdkGateway> logger)
        {
            _outboxRepository = outboxRepository;
            _webhookDispatcher = webhookDispatcher;
            _logger = logger;

            _sdkPath = configuration["BridgeConfig:SdkPath"] ?? @"C:\Program Files (x86)\Compac\COMERCIAL";
            _companyPath = configuration["BridgeConfig:CompanyPath"] ?? @"C:\Compac\Empresas\adPOLYEMPAQUES";
            _timeoutSeconds = int.TryParse(configuration["BridgeConfig:TransactionTimeoutSeconds"], out var t) ? t : 8;
            _idleTimeoutSeconds = int.TryParse(configuration["BridgeConfig:IdleSessionTimeoutSeconds"], out var i) ? i : 5;
            _forceMockMode = bool.TryParse(configuration["BridgeConfig:UseMockSdk"], out var m) && m;

            var threshold = int.TryParse(configuration["BridgeConfig:CircuitBreakerFailureThreshold"], out var f) ? f : 3;
            var cooldown = int.TryParse(configuration["BridgeConfig:CircuitBreakerCooldownSeconds"], out var c) ? c : 15;
            _circuitBreaker = new CircuitBreakerPolicy(threshold, cooldown);

            if (IsMockMode)
            {
                _logger.LogWarning("Running Contpaq.Bridge in MOCK SDK MODE (non-Windows platform or UseMockSdk=true). Native DLL calls are simulated.");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource();
            var thread = new Thread(() =>
            {
                try
                {
                    RunWorkerLoop(stoppingToken);
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                thread.SetApartmentState(ApartmentState.STA);
            }
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }

        private void RunWorkerLoop(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Contpaq.Bridge Worker Loop (MockMode={IsMockMode})", IsMockMode);

            while (!stoppingToken.IsCancellationRequested)
            {
                PumpWin32Messages();
                try
                {
                    MetricCollectorService.CircuitState = _circuitBreaker.State;

                    if (!_circuitBreaker.AllowExecution())
                    {
                        _logger.LogWarning("Circuit Breaker is OPEN. Pausing SDK queue consumption.");
                        Thread.Sleep(2000);
                        PumpWin32Messages();
                        continue;
                    }

                    var pendingItems = _outboxRepository.GetPendingTransactionsAsync(10).GetAwaiter().GetResult().ToList();

                    if (!pendingItems.Any())
                    {
                        if (_isCompanyOpen && (DateTime.UtcNow - _lastActivityTime).TotalSeconds > _idleTimeoutSeconds)
                        {
                            CloseCompanySession();
                        }

                        Thread.Sleep(500);
                        continue;
                    }

                    if (!EnsureCompanySessionOpen())
                    {
                        _circuitBreaker.RecordFailure();
                        Thread.Sleep(3000);
                        continue;
                    }

                    var swBatch = Stopwatch.StartNew();
                    int batchCount = 0;

                    foreach (var tx in pendingItems)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        ProcessSingleTransaction(tx);
                        PerformanceMetrics.RecordWriteOp();
                        batchCount++;
                        _lastActivityTime = DateTime.UtcNow;
                    }

                    swBatch.Stop();
                    if (swBatch.ElapsedMilliseconds > 0 && batchCount > 0)
                    {
                        MetricCollectorService.AverageSdkLatencyMs = swBatch.ElapsedMilliseconds / (double)batchCount;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in SDK worker loop");
                    _circuitBreaker.RecordFailure();
                    Thread.Sleep(2000);
                }
            }

            ShutdownSdk();
        }

        private bool EnsureCompanySessionOpen()
        {
            if (_isCompanyOpen) return true;

            if (IsMockMode)
            {
                _isSdkInitialized = true;
                _isCompanyOpen = true;
                _lastActivityTime = DateTime.UtcNow;
                MetricCollectorService.IsSdkSessionActive = true;
                _logger.LogInformation("[MOCK] Opened simulated CONTPAQi Company session: {Path}", _companyPath);
                return true;
            }

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
            if (IsMockMode)
            {
                _isCompanyOpen = false;
                MetricCollectorService.IsSdkSessionActive = false;
                _logger.LogInformation("[MOCK] Closed simulated CONTPAQi Company session due to idle timeout");
                return;
            }

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

            if (_isSdkInitialized && !IsMockMode)
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

        private void ProcessSingleTransaction(BridgeTransaction tx)
        {
            _outboxRepository.UpdateStatusAsync(tx.TransactionId, "PROCESSING").GetAwaiter().GetResult();
            var sw = Stopwatch.StartNew();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));

            try
            {
                var payload = JsonSerializer.Deserialize<TransactionPayload>(tx.PayloadJson);
                if (payload == null)
                {
                    FailTransaction(tx, 400, "Invalid JSON payload", sw.ElapsedMilliseconds);
                    return;
                }

                if (IsMockMode)
                {
                    // Simulated Mock Execution
                    Thread.Sleep(20); // Simulate 20ms SDK processing delay
                    int mockDocId = Interlocked.Increment(ref _mockDocCounter);
                    string mockFolio = $"MOCK-F{mockDocId}";

                    LogSdkStep(tx, 1, "fAltaDocumento", 0, 5);
                    foreach (var mov in payload.Movimientos)
                    {
                        LogSdkStep(tx, 1, "fAltaMovimiento", 0, 5);
                        if (mov.Lote != null)
                        {
                            LogSdkStep(tx, 1, "fAltaMovimientoSeriesCapas", 0, 5);
                        }
                    }
                    LogSdkStep(tx, 1, "fAfectaDocto_Param", 0, 5);

                    sw.Stop();
                    MetricCollectorService.AverageSdkLatencyMs = sw.ElapsedMilliseconds;

                    _outboxRepository.UpdateStatusAsync(tx.TransactionId, "COMPLETED", mockDocId, mockFolio).GetAwaiter().GetResult();
                    _circuitBreaker.RecordSuccess();

                    tx.Status = "COMPLETED";
                    tx.ContpaqiDocId = mockDocId;
                    tx.ContpaqiFolio = mockFolio;
                    _webhookDispatcher.DeliverAsync(tx).GetAwaiter().GetResult();

                    _logger.LogInformation("[MOCK] Processed transaction {TxId} -> Assigned Mock DocId #{DocId} Folio {Folio}", tx.TransactionId, mockDocId, mockFolio);
                    return;
                }

                string docFecha = DateTime.Now.ToString("MM/dd/yyyy");
                if (!string.IsNullOrWhiteSpace(payload.Fecha))
                {
                    if (DateTime.TryParse(payload.Fecha, out var dt))
                    {
                        docFecha = dt.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        docFecha = payload.Fecha;
                    }
                }

                int docId = 0;
                var docto = new tDocumento
                {
                    aFolio = 0,
                    aNumMoneda = 1,
                    aTipoCambio = 1.0,
                    aImporte = 0,
                    aDescuentoDoc1 = 0,
                    aDescuentoDoc2 = 0,
                    aSistemaOrigen = 0,
                    aCodConcepto = payload.CodigoConcepto,
                    aSeries = "",
                    aFecha = docFecha,
                    aCodigoCteProv = payload.CodigoClienteProveedor,
                    aCodigoAgente = payload.CodigoAgente ?? "",
                    aReferencia = payload.Referencia ?? ""
                    // tDocumento no tiene observaciones: requieren fSetDatoDocumento, pendiente en el bridge.
                };

                var docErr = ContpaqiSdkNative.fAltaDocumento(ref docId, ref docto);
                LogSdkStep(tx, 1, "fAltaDocumento", docErr, sw.ElapsedMilliseconds);

                if (docErr != ContpaqiSdkNative.kSIN_ERRORES)
                {
                    FailTransaction(tx, docErr, ContpaqiSdkNative.GetErrorMessage(docErr), sw.ElapsedMilliseconds);
                    return;
                }

                foreach (var movPayload in payload.Movimientos)
                {
                    int movId = 0;
                    var mov = new tMovimiento
                    {
                        aConsecutivo = 0,
                        aUnidades = movPayload.Unidades,
                        aPrecio = movPayload.Precio,
                        aCosto = 0,
                        aCodProdSer = movPayload.CodigoProducto,
                        aCodAlmacen = string.IsNullOrWhiteSpace(movPayload.CodigoAlmacen) ? "1" : movPayload.CodigoAlmacen,
                        aReferencia = "",
                        aCodClasific = ""
                    };

                    var movErr = ContpaqiSdkNative.fAltaMovimiento(docId, ref movId, ref mov);
                    LogSdkStep(tx, 1, "fAltaMovimiento", movErr, sw.ElapsedMilliseconds);

                    if (movErr != ContpaqiSdkNative.kSIN_ERRORES)
                    {
                        FailTransaction(tx, movErr, ContpaqiSdkNative.GetErrorMessage(movErr), sw.ElapsedMilliseconds);
                        return;
                    }

                    if (movPayload.Lote != null && !string.IsNullOrEmpty(movPayload.Lote.NumeroLote))
                    {
                        var seriesCapas = new tSeriesCapas
                        {
                            aUnidades = movPayload.Unidades,
                            aTipoCambio = 1.0,
                            aSeries = "",
                            aPedimento = movPayload.Lote.Pedimento ?? "",
                            aFechaPedimento = "",
                            aAgencia = "",
                            aFechaFabricacion = "",
                            aFechaCaducidad = movPayload.Lote.FechaCaducidad ?? "",
                            aNumeroLote = movPayload.Lote.NumeroLote
                        };

                        var lotErr = ContpaqiSdkNative.fAltaMovimientoSeriesCapas(movId, ref seriesCapas);
                        LogSdkStep(tx, 1, "fAltaMovimientoSeriesCapas", lotErr, sw.ElapsedMilliseconds);

                        if (lotErr != ContpaqiSdkNative.kSIN_ERRORES)
                        {
                            FailTransaction(tx, lotErr, ContpaqiSdkNative.GetErrorMessage(lotErr), sw.ElapsedMilliseconds);
                            return;
                        }
                    }
                }

                if (docto.aFolio > 0 && payload.CodigoConcepto != "1")
                {
                    try
                    {
                        var affectErr = ContpaqiSdkNative.fAfectaDocto_Param(payload.CodigoConcepto, docto.aSeries ?? "", docto.aFolio, true);
                        LogSdkStep(tx, 1, "fAfectaDocto_Param", affectErr, sw.ElapsedMilliseconds);
                        if (affectErr != ContpaqiSdkNative.kSIN_ERRORES)
                        {
                            _logger.LogWarning("fAfectaDocto_Param returned code {ErrCode}: {Msg} (Document #{DocId} Folio {Folio} was created successfully)", affectErr, ContpaqiSdkNative.GetErrorMessage(affectErr), docId, docto.aFolio);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "fAfectaDocto_Param threw exception (Document #{DocId} Folio {Folio} was created successfully)", docId, docto.aFolio);
                    }
                }

                sw.Stop();
                MetricCollectorService.AverageSdkLatencyMs = sw.ElapsedMilliseconds;

                string assignedFolio = docto.aFolio > 0 ? docto.aFolio.ToString() : docId.ToString();
                _outboxRepository.UpdateStatusAsync(tx.TransactionId, "COMPLETED", docId, assignedFolio).GetAwaiter().GetResult();
                _circuitBreaker.RecordSuccess();

                tx.Status = "COMPLETED";
                tx.ContpaqiDocId = docId;
                tx.ContpaqiFolio = assignedFolio;
                _webhookDispatcher.DeliverAsync(tx).GetAwaiter().GetResult();

                _logger.LogInformation("Successfully created CONTPAQi Document #{DocId} Folio '{Folio}' for Transaction {TxId}", docId, assignedFolio, tx.TransactionId);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                FailTransaction(tx, 999, $"SDK execution timed out (> {_timeoutSeconds}s)", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                FailTransaction(tx, 998, ex.Message, sw.ElapsedMilliseconds);
            }
        }

        private void FailTransaction(BridgeTransaction tx, int errorCode, string message, long durationMs)
        {
            _circuitBreaker.RecordFailure();
            var nextRetryCount = tx.RetryCount + 1;
            var delaySeconds = GetExponentialBackoffSeconds(nextRetryCount);
            var nextAttempt = DateTime.UtcNow.AddSeconds(delaySeconds);

            _outboxRepository.IncrementRetryAsync(tx.TransactionId, nextRetryCount, nextAttempt, errorCode, message).GetAwaiter().GetResult();

            tx.RetryCount = nextRetryCount;
            tx.Status = nextRetryCount >= 5 ? "DEAD_LETTER_QUEUE" : "PENDING";
            tx.LastErrorCode = errorCode;
            tx.LastErrorMessage = message;

            if (tx.Status == "DEAD_LETTER_QUEUE")
            {
                _webhookDispatcher.DeliverAsync(tx).GetAwaiter().GetResult();
            }

            _logger.LogError("Transaction {TxId} failed (Attempt {Retry}/5): [{ErrCode}] {Msg}", tx.TransactionId, nextRetryCount, errorCode, message);
        }

        private int GetExponentialBackoffSeconds(int retryCount)
        {
            var random = new Random();
            var jitter = random.Next(0, 3);
            return retryCount switch
            {
                1 => 1 + jitter,
                2 => 5 + jitter,
                3 => 15 + jitter,
                4 => 60 + jitter,
                _ => 300 + jitter
            };
        }

        private void LogSdkStep(BridgeTransaction tx, int attempt, string functionName, int errCode, long durationMs)
        {
            var log = new TransactionLog
            {
                LogId = Guid.NewGuid().ToString(),
                TransactionId = tx.TransactionId,
                CorrelationId = tx.CorrelationId,
                AttemptNumber = attempt,
                SdkFunctionName = functionName,
                SdkErrorCode = errCode,
                ErrorMessage = errCode == 0 ? null : (IsMockMode ? "Mock Success" : ContpaqiSdkNative.GetErrorMessage(errCode)),
                DurationMs = durationMs,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            _outboxRepository.AddLogAsync(log).GetAwaiter().GetResult();
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
