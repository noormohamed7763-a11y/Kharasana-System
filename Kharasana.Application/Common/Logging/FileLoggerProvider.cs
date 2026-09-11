using System.Text;
using Microsoft.Extensions.Logging;

namespace Kharasana.Application.Common.Logging
{
    /// <summary>
    /// مزوّد تسجيل يكتب السجلات إلى ملف نصي يومي (log-YYYY-MM-dd.log).
    /// يعمّل عبر AddFileLogging() — يحفظ الأخطاء بعد وقوعها بدلاً من اختفائها
    /// في الكونسول، ويقتسمه مشروعا الويب والـ API من خلال طبقة Application.
    /// </summary>
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly object _lock = new();
        private readonly FileLoggingOptions _options;

        public FileLoggerProvider(FileLoggingOptions options)
        {
            _options = options;
            Directory.CreateDirectory(options.LogsDirectory);
        }

        public LogLevel MinimumLevel => _options.MinimumLevel;

        public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        internal void Write(string categoryName, string message, Exception? exception)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{categoryName}]\n    {message}";

            if (exception != null)
                line += $"\n    {exception}";

            lock (_lock)
            {
                var file = Path.Combine(_options.LogsDirectory, $"log-{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        public void Dispose() { }
    }

    /// <summary>
    /// كائن ILogger فردي يعمل مع FileLoggerProvider.
    /// </summary>
    internal sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        public FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _provider.MinimumLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            // نبثّ المستوى في رسالة محددة — هكذا تبقى نطاقات
            // Information/Warning/Error قابلة للتصفية في الملف
            var prefix = logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "FATAL",
                _ => "INFO"
            };

            _provider.Write($"{prefix} {_categoryName}", formatter(state, exception), exception);
        }
    }
}