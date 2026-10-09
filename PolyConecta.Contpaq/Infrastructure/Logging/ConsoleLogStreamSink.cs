using System;
using System.Globalization;
using System.IO;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Core.Services;
using Serilog.Core;
using Serilog.Events;

namespace Contpaq.Bridge.Infrastructure.Logging
{
    public class ConsoleLogStreamSink : ILogEventSink
    {
        private readonly ConsoleLogStreamService _logStreamService;

        public ConsoleLogStreamSink(ConsoleLogStreamService logStreamService)
        {
            _logStreamService = logStreamService;
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                var level = logEvent.Level switch
                {
                    LogEventLevel.Verbose => "DEBUG",
                    LogEventLevel.Debug => "DEBUG",
                    LogEventLevel.Information => "INFO",
                    LogEventLevel.Warning => "WARN",
                    LogEventLevel.Error => "ERROR",
                    LogEventLevel.Fatal => "FATAL",
                    _ => "INFO"
                };

                using var writer = new StringWriter();
                logEvent.RenderMessage(writer, CultureInfo.InvariantCulture);
                var message = writer.ToString();

                string? sourceContext = null;
                if (logEvent.Properties.TryGetValue("SourceContext", out var ctxValue))
                {
                    sourceContext = ctxValue.ToString().Trim('"');
                    // Shorten namespace prefix for cleaner display
                    if (sourceContext.StartsWith("Contpaq.Bridge.", StringComparison.Ordinal))
                    {
                        sourceContext = sourceContext.Substring("Contpaq.Bridge.".Length);
                    }
                }

                var entry = new ConsoleLogEntry
                {
                    Timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    Level = level,
                    SourceContext = sourceContext ?? "",
                    Message = message,
                    Exception = logEvent.Exception?.ToString()
                };

                _logStreamService.AddLogEntry(entry);
            }
            catch
            {
                // Suppress any errors inside log sink to protect main loop
            }
        }
    }
}
