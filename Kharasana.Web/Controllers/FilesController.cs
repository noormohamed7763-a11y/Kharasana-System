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
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        ApiClient apiClient,
        IOptions<ApiSettings> apiSettings,
        ILogger<FilesController> logger)
    {
        _apiClient = apiClient;
        _apiSettings = apiSettings.Value;
        _logger = logger;
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

        try
        {
            // الملفات الثابتة على API تُخدم من جذر المضيف (ليس تحت /api/)
            // نبني URL مطلق باستخدام FilesOrigin (مشتق من BaseUrl بإزالة المسار)
            var origin = _apiSettings.FilesOrigin;
            if (string.IsNullOrWhiteSpace(origin))
            {
                _logger.LogError("FilesOrigin غير مهيأ — تحقق من ApiSettings.BaseUrl");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var apiUrl = $"{origin}/{relativePath.TrimStart('/')}";

            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

            // تمرير توكن المصادقة الحالي للسماح بالوصول للملفات المحمية
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _apiClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound();

                _logger.LogWarning("API أعاد {StatusCode} لملف {Path}", (int)response.StatusCode, relativePath);
                return StatusCode((int)response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            // ترويسات التخزين المؤقت المعقولة للملفات الثابتة
            Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            Response.Headers["ETag"] = $"\"{relativePath}\"";

            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تقديم ملف {Path}", relativePath);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}