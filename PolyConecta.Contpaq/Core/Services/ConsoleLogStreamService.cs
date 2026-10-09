using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Api.Hubs;
using Contpaq.Bridge.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Contpaq.Bridge.Core.Services
{
    public class ConsoleLogStreamService
    {
        private readonly ConcurrentQueue<ConsoleLogEntry> _recentLogs = new();
        private const int MaxLogCapacity = 200;
        private IHubContext<DashboardHub>? _hubContext;

        public void SetHubContext(IHubContext<DashboardHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void AddLogEntry(ConsoleLogEntry entry)
        {
            _recentLogs.Enqueue(entry);
            while (_recentLogs.Count > MaxLogCapacity)
            {
                _recentLogs.TryDequeue(out _);
            }

            _ = _hubContext?.Clients.All.SendAsync("ReceiveLogLine", entry);
        }

        public IEnumerable<ConsoleLogEntry> GetRecentLogs(int limit = 100)
        {
            var inMemory = _recentLogs.TakeLast(limit).ToList();
            if (inMemory.Count >= limit)
            {
                return inMemory;
            }

            var needed = limit - inMemory.Count;
            var fileLogs = ReadLatestLogFileTail(needed);
            return fileLogs.Concat(inMemory).TakeLast(limit);
        }

        private static List<ConsoleLogEntry> ReadLatestLogFileTail(int count)
        {
            var result = new List<ConsoleLogEntry>();
            try
            {
                var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logsDir)) return result;

                var latestFile = new DirectoryInfo(logsDir)
                    .GetFiles("bridge-*.log")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latestFile == null || !latestFile.Exists) return result;

                using var fs = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);

                var lines = new List<string>();
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        lines.Add(line);
                }

                var tail = lines.TakeLast(count);
                foreach (var rawLine in tail)
                {
                    result.Add(ParseRawLogLine(rawLine));
                }
            }
            catch
            {
                // Fallback silently if log file reading encounters an issue
            }

            return result;
        }

        public static ConsoleLogEntry ParseRawLogLine(string rawLine)
        {
            string level = "INFO";
            if (rawLine.Contains("[ERR]") || rawLine.Contains("[FTL]")) level = "ERROR";
            else if (rawLine.Contains("[WRN]")) level = "WARN";
            else if (rawLine.Contains("[DBG]") || rawLine.Contains("[VRB]")) level = "DEBUG";

            string timestamp = "";
            string sourceContext = "";
            string message = rawLine;

            try
            {
                var firstBracket = rawLine.IndexOf('[');
                if (firstBracket > 0)
                {
                    timestamp = rawLine.Substring(0, firstBracket).Trim();
                    var remaining = rawLine.Substring(firstBracket);

                    var endLevel = remaining.IndexOf(']');
                    if (endLevel > 0)
                    {
                        remaining = remaining.Substring(endLevel + 1).Trim();
                    }

                    if (remaining.StartsWith('['))
                    {
                        var endCtx = remaining.IndexOf(']');
                        if (endCtx > 0)
                        {
                            sourceContext = remaining.Substring(1, endCtx - 1).Trim();
                            if (sourceContext.StartsWith("Contpaq.Bridge.", StringComparison.Ordinal))
                            {
                                sourceContext = sourceContext.Substring("Contpaq.Bridge.".Length);
                            }
                            message = remaining.Substring(endCtx + 1).Trim();
                        }
                        else
                        {
                            message = remaining;
                        }
                    }
                    else
                    {
                        message = remaining;
                    }
                }
            }
            catch
            {
                message = rawLine;
            }

            return new ConsoleLogEntry
            {
                Timestamp = string.IsNullOrWhiteSpace(timestamp) ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) : timestamp,
                Level = level,
                SourceContext = sourceContext,
                Message = message
            };
        }
    }
}
