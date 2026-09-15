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

        // الملفات الثابتة على API تُخدم من جذر المضيف (ليس تحت /api/)
        var origin = _apiSettings.FilesOrigin;
        if (string.IsNullOrWhiteSpace(origin))
            return StatusCode(StatusCodes.Status500InternalServerError);

        var apiUrl = $"{origin}/{relativePath.TrimStart('/')}";

        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // ResponseHeadersRead: نuelaq streaming بدون حجز الذاكرة للملفات الكبيرة
        // نellyا نUse using للاستجابة لأن FileResult يُنفّذ بعد انتهاء الدالة — استخدام using
        // يؤدي لإغلاق الدفق قبل نسخه، لذا ننسخ مباشرة إلى Response.Body داخل الدالة.
        using var response = await _apiClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            return StatusCode((int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["ETag"] = $"\"{relativePath}\"";
        Response.ContentType = contentType;

        // نسخ مباشر من دفق API إلى جسم استجابة الويب (streaming فعلي بدون ملف ناتج)
        var stream = await response.Content.ReadAsStreamAsync();
        await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);

        return new EmptyResult();
    }
}