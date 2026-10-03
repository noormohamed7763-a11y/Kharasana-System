using System.Diagnostics;

namespace Kharasana.Web.Middlewares;

/// <summary>
/// وسيط تسجيل الطلبات: يسجّل سطراً واحداً لكل طلب مكتمل
/// (المسار، الطريقة، الحالة، المدة، TraceId) مع أي استثناء غير متوقع.
/// يُسجَّل كأول وسيط في الـ pipeline ليلتف حول معالج الأخطاء:
/// - الطلب الفاشل يُسجَّل مرة واحدة بالحالة 500 الحقيقية وتُعاد إلاقاء الاستثناء
///   كي يواصله UseExceptionHandler (لا ابتلاع هنا أبداً).
/// - إعادة التنفيذ /Home/Error لا تمر من هنا فلا تكرار في السجل.
/// الملفات الثابتة (CSS/JS/صور) تُتخطى لتقليل الضجيج.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly HashSet<string> _staticExtensions;
    private const int SlowRequestThresholdMs = 1000;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        _staticExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
            ".woff", ".woff2", ".ttf", ".eot", ".map", ".webmanifest", ".xml"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();
        var traceId = context.TraceIdentifier;

        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "فشل الطلب: {Method} {Path} ← 500 بعد {DurationMs} مللي ثانية (التتبّع={TraceId})",
                method, path, sw.ElapsedMilliseconds, traceId);
            throw; // يُواصله UseExceptionHandler — لا نبتلع الاستثناء
        }

        sw.Stop();

        var statusCode = context.Response.StatusCode;

        if (statusCode >= 500)
        {
            // حالة لم يقم أحد برفع استثناء لها صراحة (مثل خطأ من وسيط آخر دون throw)
            _logger.LogWarning(
                "انتهى الطلب بخطأ خادم: {Method} {Path} ← {StatusCode} بعد {DurationMs} مللي ثانية (التتبّع={TraceId})",
                method, path, statusCode, sw.ElapsedMilliseconds, traceId);
        }
        else if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            // تنبيه للطلبات البطيئة — تساعد على تحديد اختناقات الأداء
            _logger.LogWarning(
                "طلب بطيء: {Method} {Path} ← {StatusCode} بعد {DurationMs} مللي ثانية (التتبّع={TraceId})",
                method, path, statusCode, sw.ElapsedMilliseconds, traceId);
        }
        else
        {
            _logger.LogInformation(
                "الطلب: {Method} {Path} ← {StatusCode} بعد {DurationMs} مللي ثانية (التتبّع={TraceId})",
                method, path, statusCode, sw.ElapsedMilliseconds, traceId);
        }
    }

    private bool ShouldSkip(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return false;

        var lastSegment = path[(path.LastIndexOf('/') + 1)..];
        var dot = lastSegment.LastIndexOf('.');
        if (dot <= 0)
            return false;

        return _staticExtensions.Contains(lastSegment[dot..]);
    }
}