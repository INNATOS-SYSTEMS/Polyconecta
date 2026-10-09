using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Api.Hubs;
using Contpaq.Bridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace Contpaq.Bridge.Core.Services
{
    public static class PerformanceMetrics
    {
        private static long _totalReadOps;
        private static long _totalWriteOps;
        private static long _readOpsInCurrentWindow;
        private static long _writeOpsInCurrentWindow;
        private static double _totalQueryLatencyMs;
        private static long _queryCount;

        public static long TotalReadOps => Interlocked.Read(ref _totalReadOps);
        public static long TotalWriteOps => Interlocked.Read(ref _totalWriteOps);
        public static double AvgQueryLatencyMs => _queryCount == 0 ? 1.8 : _totalQueryLatencyMs / _queryCount;

        public static void RecordReadQuery(double elapsedMs)
        {
            Interlocked.Increment(ref _totalReadOps);
            Interlocked.Increment(ref _readOpsInCurrentWindow);
            _totalQueryLatencyMs += elapsedMs;
            _queryCount++;
        }

        public static void RecordWriteOp()
        {
            Interlocked.Increment(ref _totalWriteOps);
            Interlocked.Increment(ref _writeOpsInCurrentWindow);
        }

        public static (double readsPerSec, double writesPerSec) ConsumeWindowRates()
        {
            var reads = Interlocked.Exchange(ref _readOpsInCurrentWindow, 0);
            var writes = Interlocked.Exchange(ref _writeOpsInCurrentWindow, 0);
            return (reads, writes);
        }
    }

    public class MetricCollectorService : BackgroundService
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly IHubContext<DashboardHub> _dashboardHub;

        public static double AverageSdkLatencyMs { get; set; } = 12.5;
        public static double ErrorRatePercent { get; set; }
        public static bool IsSdkSessionActive { get; set; }
        public static string CircuitState { get; set; } = "CLOSED";

        public MetricCollectorService(IOutboxRepository outboxRepository, IHubContext<DashboardHub> dashboardHub)
        {
            _outboxRepository = outboxRepository;
            _dashboardHub = dashboardHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (readsInWindow, writesInWindow) = PerformanceMetrics.ConsumeWindowRates();
                    var readsPerSec = Math.Round(readsInWindow / 5.0, 1);
                    var writesPerSec = Math.Round(writesInWindow / 5.0, 1);
                    var queueDepth = await _outboxRepository.GetQueueDepthAsync();

                    var snapshot = new
                    {
                        timestamp = DateTime.UtcNow.ToString("o"),
                        reads_per_sec = readsPerSec,
                        writes_per_sec = writesPerSec,
                        total_reads = PerformanceMetrics.TotalReadOps,
                        total_writes = PerformanceMetrics.TotalWriteOps,
                        avg_query_latency_ms = Math.Round(PerformanceMetrics.AvgQueryLatencyMs, 2),
                        avg_sdk_latency_ms = Math.Round(AverageSdkLatencyMs, 2),
                        queue_depth = queueDepth,
                        error_rate_percent = ErrorRatePercent,
                        active_sdk_session = IsSdkSessionActive,
                        circuit_state = CircuitState
                    };

                    await _dashboardHub.Clients.All.SendAsync("ReceiveMetricsSnapshot", snapshot, stoppingToken);
                }
                catch
                {
                    // Metrics sampling exception swallowed
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
