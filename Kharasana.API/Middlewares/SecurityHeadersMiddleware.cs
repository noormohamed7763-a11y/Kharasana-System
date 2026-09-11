namespace Kharasana.API.Middlewares;

/// <summary>
/// إضافة ترويسات أمان على كل استجابة API — API يخدم JSON فقط، لذا السياسة صارمة.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // منع المتصفح من تأويل المحتوى كنوع آخر (يردّ على مرفوعات SVG/HTML المخادعة)
        headers["X-Content-Type-Options"] = "nosniff";

        // منع تضمين الاستجابة في iframe (ضد clickjacking)
        headers["X-Frame-Options"] = "DENY";
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // عدم تسريب عنوان أو بيانات في الـ Referrer عند مغادرة التطبيق
        headers["Referrer-Policy"] = "no-referrer";

        // API يعتمد على JWT — منع تخزين الاستجابات في الكاش (ذاكرة أو قرص)
        headers["Cache-Control"] = "no-store";

        await _next(context);
    }
}