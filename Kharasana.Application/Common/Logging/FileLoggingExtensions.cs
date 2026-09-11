using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kharasana.Application.Common.Logging
{
    /// <summary>
    /// تسجيل ملفات Text بدون أي حزم خارجية — يضيف FileLoggerProvider
    /// إلى ILoggingBuilder في Web أو API.
    /// </summary>
    public static class FileLoggingExtensions
    {
        public static ILoggingBuilder AddFileLogging(
            this ILoggingBuilder builder,
            LogLevel minimumLevel = LogLevel.Information,
            string? logsDirectory = null)
        {
            builder.Services.AddSingleton<ILoggerProvider>(_ =>
                new FileLoggerProvider(new FileLoggingOptions
                {
                    MinimumLevel = minimumLevel,
                    LogsDirectory = logsDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs")
                }));

            return builder;
        }
    }
}