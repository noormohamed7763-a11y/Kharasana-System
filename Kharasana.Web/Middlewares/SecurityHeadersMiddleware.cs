using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace Kharasana.Web.Middlewares;

/// <summary>
/// إضافة ترويسات أمان على كل استجابة ويب.
/// سياسة CSP متوازنة: تسمح بالمصادر المحلية + jsdelivr + الأنماط الداخلية،
/// مع استخدام nonce للـ scripts الداخلية بدلاً من unsafe-inline.
/// يُمنع المصادر غير الموثوقة والـ iframe والتسريق عبر Referrer.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private const string CspNonceKey = "CspNonce";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>إنشاء nonce عشوائي آمن لكل طلب — يُخزَّن في HttpContext.Items
    /// ليقرأه الـ ViewBag في الـ Controller ثم يُستعمل في الـ Views.</summary>
    private static string GenerateNonce()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = GenerateNonce();
        context.Items[CspNonceKey] = nonce;

        var headers = context.Response.Headers;

        // منع المتصفح من تأويل المحتوى كنوع آخر (مرفوعات SVG/HTML مخادعة)
        headers["X-Content-Type-Options"] = "nosniff";

        // ضد clickjacking — التطبيق لا يُضمَّن في iframes
        headers["X-Frame-Options"] = "DENY";

        // CSP: unsafe-inline مسموح للأنماط (Bootstrap) لكن استُبدلت للـ scripts
        // بـ nonce، وهو ما يسدّ تجاوز CSP المعتمد على الأنماط (CSS-based CSP bypass).
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "img-src 'self' data: blob: https:; " +
            "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com data:; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'";

        // تسريق العنوان فقط عند مغادرة النطاق، بدون المسار أو المعاملات
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // منع المتصفح من استخدام صلاحيات خطيرة (كاميرا، ميكروفون، موقع، إلخ)
        headers["Permissions-Policy"] =
            "geolocation=(), microphone=(), camera=(), payment=(), usb=(), fullscreen=()";

        // منع التخزين للصفحات الديناميكية — يحفظ البيانات الحساسة من الذاكرة المخروطة
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";

        await _next(context);
    }
}
