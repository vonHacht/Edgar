using Edgar.Config;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace Edgar.Logging
{
    public static class EdgarLogger
    {
        private static ILoggerFactory? _loggerFactory;

        private static AppSettings _settings = AppSettings.Load();

        public static ILogger<T> CreateLogger<T>()
        {
            EnsureInitialized();
            return _loggerFactory!.CreateLogger<T>();
        }

        private static void EnsureInitialized()
        {
            if (_loggerFactory != null)
                return;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(_settings.OutputDir, Config.Filepaths.logging),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _loggerFactory = new SerilogLoggerFactory(Log.Logger);
        }
    }
}
