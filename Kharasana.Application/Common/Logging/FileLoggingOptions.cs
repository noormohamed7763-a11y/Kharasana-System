namespace Kharasana.Application.Common.Logging
{
    /// <summary>
    /// خيارات تسجيل الملفات النصي (بديل بسيط لـ Serilog بلا حزم خارجية).
    /// </summary>
    public sealed class FileLoggingOptions
    {
        /// <summary>المجلد الذي تُكتب فيه ملفات السجلات اليومية.</summary>
        public string LogsDirectory { get; set; } = "Logs";

        /// <summary>أدنى مستوى يُسجَّل (الافتراضي Information).</summary>
        public Microsoft.Extensions.Logging.LogLevel MinimumLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Information;
    }
}