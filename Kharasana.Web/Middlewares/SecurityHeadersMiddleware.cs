namespace Kharasana.Web.Middlewares;

/// <summary>
/// إضافة ترويسات أمان على كل استجابة ويب.
/// سياسة CSP متوازنة: تسمح بالمصادر المحلية + jsdelivr + الأنماط/السكربتات الداخلية الحالية،
/// مع منع المصادر غير الموثوقة والـ iframe والتسريب عبر Referrer.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "img-src 'self' data: blob: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // منع المتصفح من تأويل المحتوى كنوع آخر (مرفوعات SVG/HTML مخادعة)
        headers["X-Content-Type-Options"] = "nosniff";

        // ضد clickjacking — التطبيق لا يُضمَّن في iframes
        headers["X-Frame-Options"] = "DENY";
        headers["Content-Security-Policy"] = ContentSecurityPolicy;

        // تسريب العنوان فقط عند مغادرة النطاق، بدون المسار أو المعاملات
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        await _next(context);
    }
}