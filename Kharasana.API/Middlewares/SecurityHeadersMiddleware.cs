namespace Kharasana.API.Middlewares;

/// <summary>
/// إضافة ترويسات أمان على كل استجابة API — API يخدم JSON بشكل أساسي، لذا السياسة صارمة
/// مع استثناء لصفحة Swagger في وضع التطوير (تحتاج inline/unsafe-eval لعرض الواجهة).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IWebHostEnvironment env,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isSwagger = context.Request.Path
            .StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);

        var headers = context.Response.Headers;

        // CSP: سياسة صارمة افتراضياً، مع استثناء /swagger في التطوير حتى لا تنكسر الواجهة
        if (isSwagger && _env.IsDevelopment())
        {
            headers["Content-Security-Policy"] =
                "default-src 'self' 'unsafe-inline' 'unsafe-eval' data: blob:; " +
                "frame-ancestors 'none';";
        }
        else
        {
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";
        }

        // منع المتصفح من تأويل المحتوى كنوع آخر (يردّ على مرفوعات SVG/HTML المخادعة)
        headers["X-Content-Type-Options"] = "nosniff";

        // منع تضمين الاستجابة في iframe (ضد clickjacking)
        headers["X-Frame-Options"] = "DENY";

        // عدم تسريب عنوان أو بيانات في الـ Referrer عند مغادرة التطبيق
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // تقييد ميزات المتصفح الحساسة
        headers["Permissions-Policy"] =
            "geolocation=(), microphone=(), camera=()";

        // API يعتمد على JWT — منع تخزين الاستجابات في الكاش (ذاكرة أو قرص)
        headers["Cache-Control"] = "no-store";

        // HSTS في الإنتاج فقط (في التطوير عبر http قد يُسبب خللاً)
        if (_env.IsProduction())
        {
            headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains";
        }

        await _next(context);
    }
}