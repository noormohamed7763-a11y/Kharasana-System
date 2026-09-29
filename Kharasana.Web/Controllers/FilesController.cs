using Kharasana.Web.Common;
using Kharasana.Web.Configuration;
using Kharasana.Web.Filters;
using Kharasana.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kharasana.Web.Controllers;

/// <summary>
/// وسيط لتقديم ملفات الصور من API عبر نفس أصل الويب.
/// يزيل حاجة المتصفح لتحميل موارد من أصل آخر (Cross-Origin)
/// ويحافظ على سياسة CSP نظيفة (img-src 'self' فقط).
/// </summary>
[SessionAuthorize]
public class FilesController : BaseController
{
    private readonly ApiClient _apiClient;
    private readonly ApiSettings _apiSettings;

    /// <summary>البادئة الوحيدة المسموح بتمريرها عبر هذا الوسيط.</summary>
    private const string FactoryLogoPrefix = "Images/Factories/";

    public FilesController(
        ApiClient apiClient,
        IOptions<ApiSettings> apiSettings)
    {
        _apiClient = apiClient;
        _apiSettings = apiSettings.Value;
    }

    /// <summary>
    /// يعيد صورة شعار مصنع بالتمرير المباشر (stream) من API.
    /// المسار: /Files/factories/{relativePath}
    /// مثال: /Files/factories/Images/Factories/abc123.png
    /// </summary>
    [HttpGet("Files/factories/{*relativePath:minlength(1)}")]
    public async Task<IActionResult> FactoryLogo(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return NotFound();

        // ✅ لا يخدم هذا الوسيط إلا شعارات المصانع: فحص البادئة وخلوّ المسار من أي مقطع
        //    اجتياز، والامتداد ضمن قائمة الصور المسموحة. بدونها يعمل الوسيط كوكيل مفتوح
        //    لأي مسار على مضيف API (بما فيه ملفات الإعدادات) بصلاحية جلسة أي مستخدم مُصادَق.
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');

        if (!normalized.StartsWith(FactoryLogoPrefix, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".."))
            return NotFound();

        if (!LogoFiles.AllowedExtensions.Contains(Path.GetExtension(normalized).ToLowerInvariant()))
            return NotFound();

        // الملفات الثابتة على API تُخدم من جذر المضيف (ليس تحت /api/)
        var origin = _apiSettings.FilesOrigin;
        if (string.IsNullOrWhiteSpace(origin))
            return StatusCode(StatusCodes.Status500InternalServerError);

        var apiUrl = $"{origin}/{normalized}";

        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // ResponseHeadersRead: تمرير مباشر (streaming) دون حجز الذاكرة للملفات الكبيرة.
        // ولا نستخدم using على الاستجابة لأن FileResult يُنفَّذ بعد انتهاء الدالة — استخدام
        // using يغلق الدفق قبل نسخه، لذا ننسخ مباشرة إلى Response.Body داخل الدالة.
        using var response = await _apiClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            return StatusCode((int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        // ✅ ETag مُرمَّز بدل المسار الخام: اسم الملف يأتي من المسار، وترويسة HTTP لا تقبل
        //    محارف التحكّم (CR/LF) ولا علامات التنصيص غير المُرمَّزة.
        //
        // ✅ private لا public: الطلب يحمل Authorization من الجلسة، و«public» تسمح صراحةً
        //    لذاكرة وسيطة مشتركة بتخزين الردّ وخدمته لمستخدم آخر (RFC 9111 §3.5 يمنع ذلك
        //    افتراضًا إلا إذا سمحت الترويسة به). «private» تُبقي الفائدة كاملة — متصفح
        //    المستخدم يخزّن الشعار سنة — وتمنع الوسائط المشتركة من رؤيته.
        Response.Headers["Cache-Control"] = "private, max-age=31536000, immutable";
        Response.Headers["ETag"] = $"\"{Uri.EscapeDataString(normalized)}\"";
        Response.ContentType = contentType;

        // نسخ مباشر من دفق API إلى جسم استجابة الويب (streaming فعلي بدون ملف ناتج)
        var stream = await response.Content.ReadAsStreamAsync();
        await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);

        return new EmptyResult();
    }
}