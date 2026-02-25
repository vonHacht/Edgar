using Edgar.Config;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace Edgar.Logging
{
    public static class EdgarLogger
    {
        private static ILoggerFactory? _loggerFactory;
        private static readonly object _lock = new();

        private static AppSettings? _settings;

        public static ILogger<T> CreateLogger<T>(AppSettings? settings = null)
        {
            _settings = settings == null ? AppSettings.Load() : settings;

            EnsureInitialized();            

            return _loggerFactory!.CreateLogger<T>();
        }

        private static void EnsureInitialized()
        {
            if (_loggerFactory != null)
                return;

            lock (_lock)
            {
                if (_loggerFactory != null)
                    return;

                Directory.CreateDirectory(_settings.OutputDir);

                var level = ParseLevel(_settings.LogLevel);

                var config = new LoggerConfiguration()
                    .MinimumLevel.Is(level)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext();

                if (_settings.LogToConsole)
                {
                    config = config.WriteTo.Console(
                        outputTemplate:
                        "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    );
                }

                if (_settings.LogToFile)
                {
                    config = config.WriteTo.File(
                        _settings.LogFilename,
                        rollingInterval: RollingInterval.Minute,
                        retainedFileCountLimit: 7,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    );
                }

                Log.Logger = config.CreateLogger();

                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog(Log.Logger, dispose: true);
                    builder.SetMinimumLevel(LogLevel.Trace);
                });
            }
        }

        private static LogEventLevel ParseLevel(string? level)
        {
            return level?.ToLowerInvariant() switch
            {
                "verbose" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }
    }
}

